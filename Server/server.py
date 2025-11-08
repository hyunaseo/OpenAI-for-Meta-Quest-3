import os 
import json 
import asyncio
from datetime import datetime

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
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

def _call_openai_sync(user_text: str) -> str:
    if not user_text:
        return "No input provided."
    
    response = client.chat.completions.create(
        model=OPENAI_MODEL,
        messages=[
            {"role": "system", "content": "You are a concise & helpful assistant. Please always answer in one single sentence."},
            {"role": "user", "content": user_text}
        ],
    )

    return (response.choices[0].message.content or "").strip() 

async def ask_openai(user_text: str) -> str:
    return await asyncio.to_thread(_call_openai_sync, user_text)

# ----------------------------
# WEBLSOCKET ENDPOINT
# ----------------------------

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    print ("[WebSocket] Client connected")

    try:
        while True:
            data = await websocket.receive_text()
            try: 
                msg = json.loads(data)
            except json.JSONDecodeError:
                await websocket.send_text(json.dumps({"type": "error", "message": "invalid json"}))
                continue

            message_type = msg.get("type")
            message_text = msg.get("text", "")

            if message_type == "stt_final":
                print(f"[WebSocket] STT FINAL: {message_text}")

                try:
                    reply = await ask_openai(message_text)
                except Exception as e:
                    print(f"[WebSocket] OpenAI error: {e}")
                    await websocket.send_text(json.dumps({"type": "error", "message": "OpenAI request failed"}))
                    continue

                await websocket.send_text(json.dumps({"type": "reply", "text": reply}))

            else:
                print (f"[WebSocket] Unknown message type: {message_type}")
                await websocket.send_text(json.dumps({"type": "error", "message": "unknown message type"}))

    except WebSocketDisconnect:
        print("[WebSocket] Client disconnected")
    except Exception as e:
        print(f"[WebSocket] Error: {e}")
    finally:
        try:
            await websocket.close()
        except Exception:
            pass    