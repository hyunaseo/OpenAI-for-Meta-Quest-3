import os 
import json 
import asyncio
from datetime import datetime

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import base64
from pathlib import Path
from fastapi.middleware.cors import CORSMiddleware

from openai import OpenAI

OPENAI_MODEL = os.getenv("OPENAI_MODEL", "gpt-4o-mini")
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")    

client = OpenAI(api_key = OPENAI_API_KEY)

app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ----------------------------
# CALL OPENAI
# ----------------------------

def _call_openai_sync(user_text: str, image_path: str = None, image_b64: str = None) -> str:
    if not user_text:
        return "No input provided."

    # Build messages; include image info if provided.
    # If we have inline base64, prefer using the Responses API with a multimodal input
    # Build a simple text-only path otherwise.
    if image_b64:
        try:
            # Some OpenAI SDKs accept a data URL as an image_url for multimodal inputs.
            data_url = f"data:image/jpeg;base64,{image_b64}"
            # Try Responses API (multimodal). Structure depends on SDK version; try a common shape.
            resp = client.responses.create(
                model=OPENAI_MODEL,
                input=[
                    {
                        "role": "user",
                        "content": [
                            {"type": "input_text", "text": user_text},
                            {"type": "input_image", "image_url": data_url}
                        ]
                    }
                ],
            )

            # Try to extract a text output in a few possible fields
            text_out = None
            if hasattr(resp, 'output_text') and resp.output_text:
                text_out = resp.output_text
            else:
                # Some SDK responses place text in resp.output[0].content
                try:
                    for item in getattr(resp, 'output', []) or []:
                        for c in item.get('content', []) if isinstance(item, dict) else []:
                            if c.get('type') == 'output_text' or c.get('type') == 'text':
                                text_out = c.get('text') or c.get('content') or text_out
                                break
                        if text_out:
                            break
                except Exception:
                    text_out = None

            if text_out:
                return text_out.strip()
            # Fallback: if multimodal call didn't return text, continue to chat fallback below
        except Exception as e:
            print(f"[OpenAI] multimodal request failed: {e}; falling back to text-only chat.")

    # Fallback: send a text-only conversation, optionally mentioning the saved image path.
    messages = [
        {"role": "system", "content": "You are a concise & helpful assistant. Please always answer in one single sentence."},
        {"role": "user", "content": user_text}
    ]

    if image_path:
        messages.append({"role": "user", "content": f"An image was saved on the server at: {image_path}. Please note the model may not be able to access server files."})

    response = client.chat.completions.create(
        model=OPENAI_MODEL,
        messages=messages,
    )

    return (response.choices[0].message.content or "").strip()


async def ask_openai(user_text: str, image_path: str = None, image_b64: str = None) -> str:
    return await asyncio.to_thread(_call_openai_sync, user_text, image_path, image_b64)

# ----------------------------
# WEBLSOCKET ENDPOINT
# ----------------------------

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    print ("[WebSocket] Client connected")

    uploads_dir = Path("./uploads")
    uploads_dir.mkdir(parents=True, exist_ok=True)

    try:
        while True:
            msg = await websocket.receive()

            # Text message
            if msg.get("text") is not None:
                data = msg["text"]
                try:
                    payload = json.loads(data)
                except json.JSONDecodeError:
                    await websocket.send_text(json.dumps({"type": "error", "message": "invalid json"}))
                    continue

                message_type = payload.get("type")
                message_text = payload.get("text", "")

                # If the client included an inline base64 image, decode and save it.
                image_b64 = payload.get("image_b64")
                saved_image_path = None
                inline_b64_for_openai = None
                if image_b64:
                    try:
                        img_bytes = base64.b64decode(image_b64)
                        ts = datetime.utcnow().strftime("%Y%m%d_%H%M%S_%f")
                        fname = uploads_dir / f"img_{ts}.jpg"
                        with open(fname, "wb") as f:
                            f.write(img_bytes)
                        saved_image_path = str(fname)
                        print(f"[WebSocket] Saved inline image to {fname} ({len(img_bytes)} bytes)")
                        # Only include base64 inline to OpenAI if reasonably small (to avoid huge requests)
                        if len(image_b64) <= 100_000:  # ~100KB base64 -> ~75KB binary
                            inline_b64_for_openai = image_b64
                    except Exception as e:
                        print(f"[WebSocket] Failed to decode/save inline image: {e}")

                if message_type == "stt_final":
                    print(f"[WebSocket] STT FINAL: {message_text}")

                    try:
                        reply = await ask_openai(message_text, image_path=saved_image_path, image_b64=inline_b64_for_openai)
                    except Exception as e:
                        print(f"[WebSocket] OpenAI error: {e}")
                        await websocket.send_text(json.dumps({"type": "error", "message": "OpenAI request failed"}))
                        continue

                    await websocket.send_text(json.dumps({"type": "reply", "text": reply}))
                else:
                    print (f"[WebSocket] Unknown message type: {message_type}")
                    await websocket.send_text(json.dumps({"type": "error", "message": "unknown message type"}))

            # Binary message
            elif msg.get("bytes") is not None:
                data_bytes = msg["bytes"]
                try:
                    ts = datetime.utcnow().strftime("%Y%m%d_%H%M%S_%f")
                    fname = uploads_dir / f"upload_{ts}.bin"
                    with open(fname, "wb") as f:
                        f.write(data_bytes)
                    print(f"[WebSocket] Received binary upload saved to {fname} ({len(data_bytes)} bytes)")
                    # Reply with acknowledgement and saved pathㅈ
                    await websocket.send_text(json.dumps({"type": "binary_received", "path": str(fname), "size": len(data_bytes)}))
                except Exception as e:
                    print(f"[WebSocket] Failed saving binary upload: {e}")
                    await websocket.send_text(json.dumps({"type": "error", "message": "failed saving binary"}))
            else:
                # Unknown receive type; ignore or report
                print(f"[WebSocket] Received unexpected message frame: {msg}")
                await websocket.send_text(json.dumps({"type": "error", "message": "unsupported frame"}))

    except WebSocketDisconnect:
        print("[WebSocket] Client disconnected")
    except Exception as e:
        print(f"[WebSocket] Error: {e}")
    finally:
        try:
            await websocket.close()
        except Exception:
            pass    