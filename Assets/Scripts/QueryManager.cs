using System;
using System.Collections;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using Unity.VisualScripting.AssemblyQualifiedNameParser;

using UnityEngine;

public class QueryManager : MonoBehaviour
{
    [Header("Modality Configuration")]
    [SerializeField] private StreamingDataSelector dataSelector;

    [Header("References")]
    [SerializeField] private SpeechToTextProvider stt;
    [SerializeField] private WebSocketClient ws;
    [SerializeField] private TextToSpeech tts;
    [SerializeField] private PassThroughProvider passthroughProvider;

    private bool _subscribed;
    private Coroutine _speakLoop;

    private enum QueryState { Idle, Listening, WaitingServer, Speaking }
    private QueryState _state = QueryState.Idle;

    // QueryState Events 
    public event Action<string> OnListeningPartial;
    public event Action<string> OnListeningFinal;
    public event Action OnProcessingStart;
    public event Action<string> OnReply;
    public event Action<string> OnTtsStart;
    public event Action OnTtsComplete;

    private void Reset()
    {
        if (!stt) stt = FindAnyObjectByType<SpeechToTextProvider>();
        if (!ws) ws = FindAnyObjectByType<WebSocketClient>();
        if (!tts) tts = FindAnyObjectByType<TextToSpeech>();
        if (!dataSelector) dataSelector = FindAnyObjectByType<StreamingDataSelector>();
        if (!passthroughProvider) passthroughProvider = FindAnyObjectByType<PassThroughProvider>();
    }

    private void Awake()
    {
        if (!stt) Debug.LogError("[QueryManager] SpeechToTextProvider not assigned.");
        if (!ws) Debug.LogError("[QueryManager] WebSocketClient not assigned.");
        if (!tts) Debug.LogError("[QueryManager] TextToSpeech not assigned.");
        // `dataSelector` and `passthroughProvider` are optional; only required when sending images.
        if (dataSelector == null)
            Debug.Log("[QueryManager] StreamingDataSelector not assigned (image sending disabled).");
        if (passthroughProvider == null)
            Debug.Log("[QueryManager] PassThroughProvider not assigned (image capture unavailable).");
    }

    private void OnEnable()
    {
        EnsureSubscriptions(true);
        ArmListening();
    }

    private void OnDisable()
    {
        EnsureSubscriptions(false);
        if (_speakLoop != null) { StopCoroutine(_speakLoop); _speakLoop = null; }
        _state = QueryState.Idle;
    }

    private void EnsureSubscriptions(bool enable)
    {
        if (enable && !_subscribed)
        {
            if (stt != null)
            {
                stt.OnPartialTranscription += HandlePartialTranscript;
                stt.OnFinalTranscription += HandleFullTranscript;
            }

            if (ws != null)
            {
                ws.OnTextMessage += HandleServerMessage;
            }
            _subscribed = true;
        }

        else if (!enable && _subscribed)
        {
            if (stt != null)
            {
                stt.OnPartialTranscription -= HandlePartialTranscript;
                stt.OnFinalTranscription -= HandleFullTranscript;
            }

            if (ws != null)
            {
                ws.OnTextMessage -= HandleServerMessage;
            }
            _subscribed = false;
        }
    }

    private void ArmListening()
    {
        if (!isActiveAndEnabled || stt == null) return;
        stt.StartListening();
        _state = QueryState.Listening;
        Debug.Log("[QueryManager] Listening…");
    }

    private void DisarmListening()
    {
        if (stt!= null) stt.StopListening();
        if (_state == QueryState.Listening) _state = QueryState.Idle;
        Debug.Log("[QueryManager] Stopped listening.");
    }

    private void HandlePartialTranscript(string text)
    {
        if (_state != QueryState.Listening) return;
        OnListeningPartial?.Invoke(text);
        Debug.Log($"[QueryManager][Partial] {text}");
    }

    private void HandleFullTranscript(string text)
    {
        if (_state != QueryState.Listening) return;

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.Log("[QueryManager] Empty transcription received; ignoring.");
            ArmListening();
            return;
        }

        DisarmListening();

        OnListeningFinal?.Invoke(text);
        OnProcessingStart?.Invoke();
        // If configured, try to capture an image and include it with the STT payload.
        string json;
        bool wantImage = dataSelector != null && dataSelector.SendImage;
        if (wantImage && passthroughProvider != null)
        {
            try
            {
                if (passthroughProvider.TryCapturePassThrough(out byte[] jpgBytes, out int iw, out int ih) && jpgBytes != null && jpgBytes.Length > 0)
                {
                    string b64 = Convert.ToBase64String(jpgBytes);
                    json = $"{{\"type\":\"stt_final\",\"text\":{ToJsonString(text)},\"image_b64\":{ToJsonString(b64)},\"image_w\":{iw},\"image_h\":{ih}}}";
                }
                else
                {
                    Debug.LogWarning("[QueryManager] Image capture failed or returned empty bytes; sending text only.");
                    json = $"{{\"type\":\"stt_final\",\"text\":{ToJsonString(text)}}}";
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QueryManager] Exception capturing image: {ex.Message}; sending text only.");
                json = $"{{\"type\":\"stt_final\",\"text\":{ToJsonString(text)}}}";
            }
        }
        else
        {
            if (wantImage && passthroughProvider == null)
                Debug.LogWarning("[QueryManager] SendImage requested but PassThroughProvider is not assigned.");
            json = $"{{\"type\":\"stt_final\",\"text\":{ToJsonString(text)}}}";
        }

        ws?.SendText(json);
        _state = QueryState.WaitingServer;
        Debug.Log($"[QueryManager] → Server: {text}");
    }

    [Serializable]
    private class ServerMsg { public string type; public string text; }

    private void HandleServerMessage(string msg)
    {
        if (_state != QueryState.WaitingServer && _state != QueryState.Speaking)
        {
            return;
        }


        ServerMsg serverMsg = null;
        try { serverMsg = JsonUtility.FromJson<ServerMsg>(msg); } catch { }

        if (serverMsg == null || string.IsNullOrEmpty(serverMsg.type))
        {
            Debug.LogWarning($"[QueryManager] Server message (raw): {msg}");
            return;
        }

        switch (serverMsg.type)
        {
            case "reply":
                OnServerReply(serverMsg.text ?? string.Empty);
                break;

            default:
                Debug.LogWarning($"[QueryManager] Unknown server message type: {serverMsg.type}");
                break;
        }
    }

    private void OnServerReply(string text)
    {
        if (_state != QueryState.WaitingServer && _state != QueryState.Speaking)
            return;
        
        OnReply?.Invoke(text ?? string.Empty);
        Debug.Log($"[QueryManager] ← Server: {text}");

        OnTtsStart?.Invoke(text ?? string.Empty);
        if (_speakLoop != null) StopCoroutine(_speakLoop);
        _speakLoop = StartCoroutine(SpeakThenRearm(text));
    }

    private IEnumerator SpeakThenRearm(string text)
    {
        _state = QueryState.Speaking;
        stt.StopListening();

        bool done = false;
        void MarkDone() => done = true;

        tts.OnSpeechComplete += MarkDone;
        
        tts?.SpeakNow(text);
        OnTtsComplete?.Invoke();

        while (!done) yield return null;

        tts.OnSpeechComplete -= MarkDone;

        OnTtsComplete?.Invoke();
        ArmListening();
        _speakLoop = null;
    }

    private static string ToJsonString(string str)
    {
        if (str == null) return "null";
        return "\"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
