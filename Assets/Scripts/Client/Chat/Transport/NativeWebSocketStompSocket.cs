using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;

// NativeWebSocket callback을 STOMP를 모르는 text transport event로 변환한다.
public sealed class NativeWebSocketStompSocket : IStompSocket
{
    private WebSocket _webSocket;
    private int _connectionId;
    private bool _closeWhenOpened;

    private WebSocketOpenEventHandler _openedHandler;
    private WebSocketMessageEventHandler _messageHandler;
    private WebSocketErrorEventHandler _errorHandler;
    private WebSocketCloseEventHandler _closedHandler;

    public bool IsOpen => _webSocket != null && _webSocket.State == WebSocketState.Open;

    public event Action<int> Opened;
    public event Action<int, string> MessageReceived;
    public event Action<int, string> ErrorReceived;
    public event Action<int> Closed;

    // NativeWebSocket 생성과 callback 연결을 마친 뒤 WebSocket 연결을 시작한다.
    public Task ConnectAsync(string url, int connectionId)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("WebSocket URL이 필요합니다.", nameof(url));
        }

        if (_webSocket != null && _webSocket.State != WebSocketState.Closed)
        {
            throw new InvalidOperationException("이전 WebSocket 연결이 아직 종료되지 않았습니다.");
        }

        DetachWebSocketHandlers();

        WebSocket socket = new WebSocket(url);
        _webSocket = socket;
        _connectionId = connectionId;
        _closeWhenOpened = false;

        _openedHandler = () => OnWebSocketOpened(socket, connectionId);
        _messageHandler = bytes => OnWebSocketMessageReceived(socket, connectionId, bytes);
        _errorHandler = errorMessage => OnWebSocketErrorReceived(socket, connectionId, errorMessage);
        _closedHandler = closeCode => OnWebSocketClosed(socket, connectionId, closeCode);

        socket.OnOpen += _openedHandler;
        socket.OnMessage += _messageHandler;
        socket.OnError += _errorHandler;
        socket.OnClose += _closedHandler;
        return socket.Connect();
    }

    // STOMP frame을 변경하지 않고 NativeWebSocket text 전송으로 위임한다.
    public Task SendTextAsync(string message)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("열려 있는 WebSocket 연결이 없어 메시지를 전송할 수 없습니다.");
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

        if (_webSocket.State == WebSocketState.Connecting)
        {
            _closeWhenOpened = true;
            _webSocket.CancelConnection();
            return Task.CompletedTask;
        }

        if (_webSocket.State == WebSocketState.Closing || _webSocket.State == WebSocketState.Closed)
        {
            return Task.CompletedTask;
        }

        return _webSocket.Close();
    }

    // WebSocket open은 STOMP CONNECT를 보낼 수 있는 transport 준비 상태만 뜻한다.
    private void OnWebSocketOpened(WebSocket socket, int connectionId)
    {
        if (IsCurrentSocket(socket, connectionId) == false)
        {
            return;
        }

        if (_closeWhenOpened == true)
        {
            _closeWhenOpened = false;
            _ = CloseAfterOpenAsync(connectionId);
            return;
        }

        Opened?.Invoke(connectionId);
    }

    // Connecting 상태의 플랫폼별 cancel이 open 직후에만 반영돼도 STOMP CONNECT 없이 종료한다.
    private async Task CloseAfterOpenAsync(int connectionId)
    {
        try
        {
            await CloseAsync();
        }
        catch (Exception exception)
        {
            ErrorReceived?.Invoke(connectionId, exception.Message);
        }
    }

    // binary callback 데이터를 UTF-8 text로 바꾸되 STOMP command와 body는 해석하지 않는다.
    private void OnWebSocketMessageReceived(WebSocket socket, int connectionId, byte[] bytes)
    {
        if (IsCurrentSocket(socket, connectionId) == true)
        {
            MessageReceived?.Invoke(connectionId, Encoding.UTF8.GetString(bytes));
        }
    }

    // NativeWebSocket transport 오류를 상위 protocol 계층에 그대로 전달한다.
    private void OnWebSocketErrorReceived(WebSocket socket, int connectionId, string errorMessage)
    {
        if (IsCurrentSocket(socket, connectionId) == true)
        {
            ErrorReceived?.Invoke(connectionId, errorMessage);
        }
    }

    // close code는 현재 UI 계약에 없으므로 종료 사실만 전달한다.
    private void OnWebSocketClosed(WebSocket socket, int connectionId, WebSocketCloseCode closeCode)
    {
        if (IsCurrentSocket(socket, connectionId) == false)
        {
            return;
        }

        Closed?.Invoke(connectionId);
        DetachWebSocketHandlers();
        _webSocket = null;
        _connectionId = 0;
        _closeWhenOpened = false;
    }

    // 이전 socket의 지연 callback이 새 연결 이벤트로 전달되지 않도록 같은 delegate 인스턴스를 해제한다.
    private void DetachWebSocketHandlers()
    {
        if (_webSocket == null)
        {
            return;
        }

        if (_openedHandler != null)
        {
            _webSocket.OnOpen -= _openedHandler;
            _webSocket.OnMessage -= _messageHandler;
            _webSocket.OnError -= _errorHandler;
            _webSocket.OnClose -= _closedHandler;
        }

        _openedHandler = null;
        _messageHandler = null;
        _errorHandler = null;
        _closedHandler = null;
    }

    private bool IsCurrentSocket(WebSocket socket, int connectionId)
    {
        return ReferenceEquals(_webSocket, socket) && _connectionId == connectionId;
    }
}
