using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

// socket transport와 frame codec을 조합해 채팅 STOMP 연결 순서와 수신 DTO 변환을 조정한다.
public sealed class StompChatService : IDisposable
{
    private const string PublicChatDestination = "/topic/chat/public";
    private const string NoticesDestination = "/topic/notices";
    private const string UserSystemDestination = "/user/queue/system";
    private const string UserErrorDestination = "/user/queue/errors";

    private readonly IStompSocket _socket;
    private readonly StompFrameCodec _codec;

    private string _host;
    private string _accessToken;
    private int _nextConnectionId;
    private int _activeConnectionId;
    private int _closeRequestedConnectionId;
    private Task _closeTask;
    private bool _disposed;

    public StompChatService(IStompSocket socket, StompFrameCodec codec)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));

        _socket.Opened += OnSocketOpened;
        _socket.MessageReceived += OnSocketMessageReceived;
        _socket.ErrorReceived += OnSocketErrorReceived;
        _socket.Closed += OnSocketClosed;
    }

    public StompChatConnectionState State { get; private set; } = StompChatConnectionState.Disconnected;
    public bool IsNormalDisconnectRequested { get; private set; }

    public event Action<StompChatConnectionState> StateChanged;
    public event Action<string, ChatMessageDto> MessageReceived;
    public event Action<string> ApplicationErrorReceived;
    public event Action<string> TransportErrorReceived;
    public event Action<string> ProtocolErrorReceived;

    // Lobby가 제공한 URL·host·token을 보관하고 WebSocket 연결을 시작한다.
    public async Task ConnectAsync(string url, string host, string accessToken)
    {
        ThrowIfDisposed();

        if (State != StompChatConnectionState.Disconnected)
        {
            throw new InvalidOperationException("이미 채팅 연결을 진행 중이거나 연결되어 있습니다.");
        }

        int connectionId = ++_nextConnectionId;
        _activeConnectionId = connectionId;
        _closeRequestedConnectionId = 0;
        _closeTask = null;
        IsNormalDisconnectRequested = false;
        _host = host;
        _accessToken = accessToken;
        _codec.Reset();
        SetState(StompChatConnectionState.WebSocketConnecting);

        try
        {
            await _socket.ConnectAsync(url, connectionId);
        }
        catch (Exception exception)
        {
            if (IsActiveConnection(connectionId) == true
                && State != StompChatConnectionState.Disconnecting
                && State != StompChatConnectionState.Disconnected)
            {
                TransportErrorReceived?.Invoke(exception.Message);
                CompleteDisconnected(connectionId);
            }

            throw;
        }
    }

    // 서버가 되돌려 줄 PUBLIC_CHAT만 목록에 반영한다는 계약으로 공개 채팅을 전송한다.
    public Task SendPublicChatAsync(string content)
    {
        ThrowIfDisposed();

        if (State != StompChatConnectionState.Connected)
        {
            throw new InvalidOperationException("필수 STOMP 구독 완료 후에만 채팅을 전송할 수 있습니다.");
        }

        return _socket.SendTextAsync(_codec.CreateSendPublicChat(content));
    }

    // Lobby 종료 시 DISCONNECT를 먼저 보내고 transport를 닫는다.
    public Task DisconnectAsync()
    {
        if (_disposed == true || State == StompChatConnectionState.Disconnected)
        {
            return Task.CompletedTask;
        }

        if (_closeTask != null)
        {
            return _closeTask;
        }

        IsNormalDisconnectRequested = true;
        _closeTask = CloseConnectionAsync(_activeConnectionId, true);
        return _closeTask;
    }

    // socket callback 구독을 해제해 Lobby 수명 종료 뒤의 수신을 막는다.
    public void Dispose()
    {
        if (_disposed == true)
        {
            return;
        }

        _disposed = true;
        IsNormalDisconnectRequested = true;
        _closeRequestedConnectionId = _activeConnectionId;
        _codec.Reset();
        _socket.Opened -= OnSocketOpened;
        _socket.MessageReceived -= OnSocketMessageReceived;
        _socket.ErrorReceived -= OnSocketErrorReceived;
        _socket.Closed -= OnSocketClosed;

        if (State != StompChatConnectionState.Disconnected)
        {
            SetState(StompChatConnectionState.Disconnected);
        }

        _ = CloseAfterDisposeAsync();
    }

    // WebSocket open event는 Action 시그니처라 Task 작업을 시작하되 내부 예외를 모두 처리한다.
    private void OnSocketOpened(int connectionId)
    {
        _ = SendConnectAsync(connectionId);
    }

    // WebSocket open 직후에는 구독 대신 STOMP CONNECT부터 보낸다.
    private async Task SendConnectAsync(int connectionId)
    {
        if (IsActiveConnection(connectionId) == false || State != StompChatConnectionState.WebSocketConnecting)
        {
            return;
        }

        try
        {
            SetState(StompChatConnectionState.WebSocketConnected);
            SetState(StompChatConnectionState.StompConnecting);
            await _socket.SendTextAsync(_codec.CreateConnect(_host, _accessToken));
        }
        catch (Exception exception)
        {
            if (IsActiveConnection(connectionId) == false
                || State == StompChatConnectionState.Disconnecting
                || State == StompChatConnectionState.Disconnected)
            {
                return;
            }

            ProtocolErrorReceived?.Invoke(exception.Message);
            await CloseAfterFailureAsync(connectionId);
        }
    }

    // 누적 codec 결과를 command별로 분기하되 transport는 frame 내용을 알지 못하게 유지한다.
    private void OnSocketMessageReceived(int connectionId, string text)
    {
        if (IsActiveConnection(connectionId) == false
            || State == StompChatConnectionState.Disconnecting
            || State == StompChatConnectionState.Disconnected)
        {
            return;
        }

        try
        {
            foreach (StompFrame frame in _codec.AppendReceivedText(text))
            {
                HandleFrame(connectionId, frame);
            }
        }
        catch (Exception exception)
        {
            ProtocolErrorReceived?.Invoke(exception.Message);
            _ = CloseAfterFailureAsync(connectionId);
        }
    }

    // CONNECTED 이후에는 필수 SUBSCRIBE 전송이 모두 끝난 다음에만 Connected 상태가 된다.
    private void HandleFrame(int connectionId, StompFrame frame)
    {
        if (frame.Command == StompCommand.Connected)
        {
            if (State != StompChatConnectionState.StompConnecting)
            {
                ProtocolErrorReceived?.Invoke("예상하지 못한 CONNECTED frame입니다.");
                return;
            }

            SetState(StompChatConnectionState.Subscribing);
            _ = SubscribeRequiredDestinationsAsync(connectionId);
            return;
        }

        if (frame.Command == StompCommand.Message)
        {
            HandleMessageFrame(frame);
            return;
        }

        if (frame.Command == StompCommand.Error)
        {
            HandleStompError(connectionId, frame);
        }
    }

    // 서버 계약의 네 destination을 중복되지 않는 고정 ID로 순서대로 구독한다.
    private async Task SubscribeRequiredDestinationsAsync(int connectionId)
    {
        try
        {
            await SendSubscribeAsync(connectionId, PublicChatDestination, "sub-public-chat");
            await SendSubscribeAsync(connectionId, NoticesDestination, "sub-notices");
            await SendSubscribeAsync(connectionId, UserSystemDestination, "sub-system");
            await SendSubscribeAsync(connectionId, UserErrorDestination, "sub-errors");

            if (IsActiveConnection(connectionId) == true && State == StompChatConnectionState.Subscribing)
            {
                SetState(StompChatConnectionState.Connected);
            }
        }
        catch (OperationCanceledException) when (IsActiveConnection(connectionId) == false
                                                 || State == StompChatConnectionState.Disconnecting
                                                 || State == StompChatConnectionState.Disconnected)
        {
            // 종료 요청이 들어온 뒤에는 아직 보내지 않은 SUBSCRIBE frame을 전송하지 않는다.
        }
        catch (Exception exception)
        {
            if (IsActiveConnection(connectionId) == false
                || State == StompChatConnectionState.Disconnecting
                || State == StompChatConnectionState.Disconnected)
            {
                return;
            }

            TransportErrorReceived?.Invoke(exception.Message);
            await CloseAfterFailureAsync(connectionId);
        }
    }

    // 종료 또는 다른 연결 시도가 시작되면 남은 구독 전송을 중단한다.
    private Task SendSubscribeAsync(int connectionId, string destination, string subscriptionId)
    {
        if (IsActiveConnection(connectionId) == false || State != StompChatConnectionState.Subscribing)
        {
            throw new OperationCanceledException("채팅 구독 전송이 중단되었습니다.");
        }

        return _socket.SendTextAsync(_codec.CreateSubscribe(destination, subscriptionId));
    }

    // destination에 따라 application error와 공통 채팅 DTO를 서로 다른 이벤트로 전달한다.
    private void HandleMessageFrame(StompFrame frame)
    {
        if (frame.TryGetHeader("destination", out string destination) == false)
        {
            ProtocolErrorReceived?.Invoke("MESSAGE frame에 destination header가 없습니다.");
            return;
        }

        if (destination == UserErrorDestination)
        {
            ChatApplicationErrorDto errorDto = JsonConvert.DeserializeObject<ChatApplicationErrorDto>(frame.Body);

            if (string.IsNullOrEmpty(errorDto?.Message))
            {
                ProtocolErrorReceived?.Invoke("오류 MESSAGE body 형식이 올바르지 않습니다.");
                return;
            }

            ApplicationErrorReceived?.Invoke(errorDto.Message);
            return;
        }

        ChatMessageDto messageDto = JsonConvert.DeserializeObject<ChatMessageDto>(frame.Body);

        if (messageDto == null)
        {
            ProtocolErrorReceived?.Invoke("채팅 MESSAGE body 형식이 올바르지 않습니다.");
            return;
        }

        MessageReceived?.Invoke(destination, messageDto);
    }

    // STOMP ERROR는 application 오류와 달리 종료 상태로 전환하고 transport 종료를 요청한다.
    private void HandleStompError(int connectionId, StompFrame frame)
    {
        frame.TryGetHeader("message", out string headerMessage);
        string errorMessage = string.IsNullOrEmpty(headerMessage) ? frame.Body : $"{headerMessage}: {frame.Body}";
        ProtocolErrorReceived?.Invoke(errorMessage);
        _ = CloseAfterFailureAsync(connectionId);
    }

    // transport 오류는 STOMP protocol 오류와 섞지 않고 상위 표시 계층에 전달한다.
    private void OnSocketErrorReceived(int connectionId, string errorMessage)
    {
        if (IsActiveConnection(connectionId) == true
            && State != StompChatConnectionState.Disconnecting
            && State != StompChatConnectionState.Disconnected)
        {
            TransportErrorReceived?.Invoke(errorMessage);
        }
    }

    // 원격 종료는 상위에 오류를 알린 뒤 재연결 가능한 최종 끊김 상태로 바꾼다.
    private void OnSocketClosed(int connectionId)
    {
        if (IsActiveConnection(connectionId) == false)
        {
            return;
        }

        bool wasUnexpected = _closeRequestedConnectionId != connectionId;
        if (wasUnexpected == true && _disposed == false)
        {
            TransportErrorReceived?.Invoke("채팅 연결이 끊겼습니다.");
        }

        CompleteDisconnected(connectionId);
    }

    // CONNECT 또는 SUBSCRIBE 실패는 같은 close 작업을 공유해 종료 요청을 중복하지 않는다.
    private async Task CloseAfterFailureAsync(int connectionId)
    {
        if (IsActiveConnection(connectionId) == false
            || State == StompChatConnectionState.Disconnecting
            || State == StompChatConnectionState.Disconnected)
        {
            return;
        }

        Task closeTask = _closeTask;
        if (closeTask == null)
        {
            closeTask = CloseConnectionAsync(connectionId, false);
            _closeTask = closeTask;
        }

        try
        {
            await closeTask;
        }
        catch (Exception)
        {
            // CloseConnectionAsync가 이미 transport 오류 이벤트를 전달했다.
        }
    }

    // DISCONNECT 실패와 Close 실패를 모두 전달하되, 어떤 경우에도 transport 종료를 시도한다.
    private async Task CloseConnectionAsync(int connectionId, bool sendDisconnect)
    {
        Exception firstException = null;

        _closeRequestedConnectionId = connectionId;

        if (IsActiveConnection(connectionId) == true)
        {
            SetState(StompChatConnectionState.Disconnecting);
        }

        if (sendDisconnect == true && _socket.IsOpen == true)
        {
            try
            {
                await _socket.SendTextAsync(_codec.CreateDisconnect());
            }
            catch (Exception exception)
            {
                firstException = exception;
                TransportErrorReceived?.Invoke(exception.Message);
            }
        }

        try
        {
            await _socket.CloseAsync();
        }
        catch (Exception exception)
        {
            if (firstException == null)
            {
                firstException = exception;
            }

            TransportErrorReceived?.Invoke(exception.Message);
        }
        finally
        {
            CompleteDisconnected(connectionId);
        }

        if (firstException != null)
        {
            throw firstException;
        }
    }

    // Dispose 중에는 예외를 관찰하고, 이미 해제된 상위 계층을 다시 호출하지 않는다.
    private async Task CloseAfterDisposeAsync()
    {
        try
        {
            await _socket.CloseAsync();
        }
        catch (Exception exception)
        {
            TransportErrorReceived?.Invoke(exception.Message);
        }
    }

    // 현재 연결의 종료만 상태와 codec 버퍼에 반영한다.
    private void CompleteDisconnected(int connectionId)
    {
        if (IsActiveConnection(connectionId) == false)
        {
            return;
        }

        _codec.Reset();
        SetState(StompChatConnectionState.Disconnected);
    }

    // 상태 변경을 한 지점에 모아 UI가 WebSocket과 STOMP 성공을 구분할 수 있게 한다.
    private void SetState(StompChatConnectionState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(State);
    }

    private bool IsActiveConnection(int connectionId)
    {
        return _disposed == false && _activeConnectionId == connectionId;
    }

    // 해제된 service가 transport callback을 다시 조정하지 못하게 막는다.
    private void ThrowIfDisposed()
    {
        if (_disposed == true)
        {
            throw new ObjectDisposedException(nameof(StompChatService));
        }
    }

    // `/user/queue/errors` body에 정의된 message 단일 필드만 역직렬화한다.
    private sealed class ChatApplicationErrorDto
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
