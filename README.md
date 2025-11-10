# OpenAI for Meta Quest 3

This project connects **Meta Quest 3** (Unity client) with an **OpenAI-powered server** via **WebSocket**, enabling real-time voice and image-based interaction through passthrough camera access.

---

## Project Overview

- **Client**: Unity app for Meta Quest 3  
  - Captures voice and camera frames  
  - Sends them to server for GPT-based processing  
  - Receives and speaks responses through TTS  

- **Server**: Python FastAPI + WebSocket backend  
  - Handles multimodal queries (text + image)  
  - Connects to OpenAI API (e.g., GPT-4o)  
  - Streams responses back to client  


---

## Installation

### 🕹️ Client (Unity)

1. Install **Unity 6** (or higher)  
2. Import **[Meta All-in-One SDK](https://developers.meta.com/horizon/downloads/package/meta-xr-sdk-all-in-one-upm/)** (for Quest 3 integration)  
3. Open the Unity project  
4. In `WebSocketClient` component, set the `serverUrl` to your local network IP  
   - Example: `ws://192.168.x.x:8080/ws`


### 🖥️ Server (Python)

#### Requirements

- Python 3.10+  
- [FastAPI](https://fastapi.tiangolo.com/)  
- [Uvicorn](https://www.uvicorn.org/)  
- [OpenAI Python SDK](https://pypi.org/project/openai/)  
- (Optional) `websockets` or `aiohttp` if used in your implementation  

#### Install dependencies

```
pip install fastapi uvicorn openai
```

Make sure your Quest 3 and server PC are on the same Wi-Fi network.


## Meta Quest Camera Permission

To access passthrough camera in Meta Quest, the following permissions are required.

### Android Permission 
```xml
android.permission.CAMERA
horizonos.permission.HEADSET_CAMERA
```

#### `android.permission.CAMERA`
- Required by Unity’s **`WebCamTexture`** API for standard camera access.  

#### `horizonos.permission.HEADSET_CAMERA`
- A **Meta-specific system permission** used to access the **headset’s passthrough cameras**.  
- Only available on **Meta Horizon OS (Quest 3 and later)**.  


### ⚠️ When Manifest Edits Are Not Applied 

If your modified `AndroidManifest.xml` does **not** apply,  
you can manually grant the required permissions using **ADB commands**.

Use the following commands after installing your app on Meta Quest:

```
adb shell pm grant {PACKAGE_NAME} horizonos.permission.HEADSET_CAMERA
adb shell pm grant {PACKAGE_NAME} android.permission.CAMERA
```

To check if the permissions were successfully applied:
```
adb shell dumpsys package {PACKAGE_NAME} | grep -i HEADSET_CAMERA
adb shell dumpsys package {PACKAGE_NAME} | grep -i CAMERA
```

---

## 🚀 Running the App

1. Connect both Quest 3 and server PC to the same Wi-Fi

2. Set the WebSocket IP inside Unity (WebSocketClient component)

3. Start the Python server
```
export OPENAI_API_KEY={YOUR_API_KEY}
cd Server
uvicorn server:app --host 0.0.0.0 --port 8080 --reload
```

4. Build & run the Unity app on Meta Quest 3 (Once launched, the Quest app will connect to the server and begin processing audio/image input through the OpenAI API.)