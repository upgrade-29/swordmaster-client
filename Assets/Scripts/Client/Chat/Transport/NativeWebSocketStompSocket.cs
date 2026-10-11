using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;

// NativeWebSocket callback을 STOMP를 모르는 text transport event로 변환한다.
public sealed class NativeWebSocketStompSocket : IStompSocket
{
    private WebSocket _webSocket;

    public bool IsOpen => _webSocket != null && _webSocket.State == WebSocketState.Open;

    public event Action Opened;
    public event Action<string> MessageReceived;
    public event Action<string> ErrorReceived;
    public event Action Closed;

    // NativeWebSocket 생성과 callback 연결을 마친 뒤 WebSocket 연결을 시작한다.
    public Task ConnectAsync(string url)
    {
        _webSocket = new WebSocket(url);
        _webSocket.OnOpen += OnWebSocketOpened;
        _webSocket.OnMessage += OnWebSocketMessageReceived;
        _webSocket.OnError += OnWebSocketErrorReceived;
        _webSocket.OnClose += OnWebSocketClosed;
        return _webSocket.Connect();
    }

    // STOMP frame을 변경하지 않고 NativeWebSocket text 전송으로 위임한다.
    public Task SendTextAsync(string message)
    {
        if (_webSocket == null)
        {
            return Task.CompletedTask;
        }

        return _webSocket.SendText(message);
    }

    // NativeWebSocket의 정상 종료 handshake를 요청한다.
    public Task CloseAsync()
    {
        if (_webSocket == null)
        {
            return Task.CompletedTask;
        }

        return _webSocket.Close();
    }

    // WebSocket open은 STOMP CONNECT를 보낼 수 있는 transport 준비 상태만 뜻한다.
    private void OnWebSocketOpened()
    {
        Opened?.Invoke();
    }

    // binary callback 데이터를 UTF-8 text로 바꾸되 STOMP command와 body는 해석하지 않는다.
    private void OnWebSocketMessageReceived(byte[] bytes)
    {
        MessageReceived?.Invoke(Encoding.UTF8.GetString(bytes));
    }

    // NativeWebSocket transport 오류를 상위 protocol 계층에 그대로 전달한다.
    private void OnWebSocketErrorReceived(string errorMessage)
    {
        ErrorReceived?.Invoke(errorMessage);
    }

    // close code는 현재 UI 계약에 없으므로 종료 사실만 전달한다.
    private void OnWebSocketClosed(WebSocketCloseCode closeCode)
    {
        Closed?.Invoke();
    }
}
