using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEditor.PackageManager;


[DisallowMultipleComponent]
public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    [Header("WebSocket")]
    [Tooltip("e.g., ws://127.0.0.1:8080/ws")]
    public string serverUrl = "ws://127.0.0.1:8080/ws";

    [Tooltip("Milliseconds between reconnect attempts,.")]
    public int reconnectIntervalMs = 1000;

    public event Action<string> OnTextMessage;

    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _recvTask;
    private Task _sendTask;
    private readonly ConcurrentQueue<ArraySegment<byte>> _sendQueue = new();
    private readonly byte[] _recvBuffer = new byte[64 * 1024];
    private volatile bool _connecting;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void OnEnable()
    {
        await EnsureConnected();
    }

    private async void OnDisable()
    {
        await CloseAsync();
    }

    public async Task EnsureConnected()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open) return;
        if (_connecting) return;
        _connecting = true;

        while (enabled)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();

            try
            {
                Debug.Log($"[WebSocketClient] Connecting to {serverUrl}...");
                await _webSocket.ConnectAsync(new Uri(serverUrl), _cancellationTokenSource.Token);
                Debug.Log("[WebSocketClient] Connected.");

                _recvTask = Task.Run(() => RecvLoop(_cancellationTokenSource.Token));
                _sendTask = Task.Run(() => SendLoop(_cancellationTokenSource.Token));

                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebSocketClient] Connection failed: {ex.Message}.");
                await Task.Delay(reconnectIntervalMs);
            }
        }

        _connecting = false;
    }

    public void SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("[WS] Not connected; message skipped.");
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        _sendQueue.Enqueue(new ArraySegment<byte>(bytes));
    }

    public async Task CloseAsync()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch { /* ignore */ }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
        }
    }

    private async Task RecvLoop(CancellationToken token)
    {
        var stringBuilder = new StringBuilder();

        try
        {
            while (!token.IsCancellationRequested && _webSocket != null)
            {
                var seg = new ArraySegment<byte>(_recvBuffer);
                WebSocketReceiveResult res = await _webSocket.ReceiveAsync(seg, token);

                if (res.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning("[WebSocketClient] Server closed connection.");
                    break;
                }

                stringBuilder.Append(Encoding.UTF8.GetString(_recvBuffer, 0, res.Count));
                if (res.EndOfMessage)
                {
                    var message = stringBuilder.ToString();
                    stringBuilder.Length = 0;
                    OnMainThread(() => OnTextMessage?.Invoke(message));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebSocketClient] Receive error: {ex.Message}");
        }

        await EnsureConnected();
    }

    private async Task SendLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _webSocket != null)
            {
                while (_sendQueue.TryDequeue(out var msg))
                {
                    await _webSocket.SendAsync(msg, WebSocketMessageType.Text, true, token);
                }

                await Task.Delay(5, token);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebSocketClient] Send error: {ex.Message}");
        }
    }

    private readonly ConcurrentQueue<Action> _mainQueue = new();

    private void Update()
    {
        while (_mainQueue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    private void OnMainThread(Action action)
    {
        _mainQueue.Enqueue(action);
    }
    
    [Serializable] private struct Wrapper { public object data; }
}
