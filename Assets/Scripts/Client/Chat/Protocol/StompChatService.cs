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

    public event Action<StompChatConnectionState> StateChanged;
    public event Action<string, ChatMessageDto> MessageReceived;
    public event Action<string> ApplicationErrorReceived;
    public event Action<string> TransportErrorReceived;
    public event Action<string> ProtocolErrorReceived;

    // Lobby가 제공한 URL·host·token을 보관하고 WebSocket 연결을 시작한다.
    public Task ConnectAsync(string url, string host, string accessToken)
    {
        ThrowIfDisposed();

        if (State != StompChatConnectionState.Disconnected)
        {
            throw new InvalidOperationException("이미 채팅 연결을 진행 중이거나 연결되어 있습니다.");
        }

        _host = host;
        _accessToken = accessToken;
        SetState(StompChatConnectionState.WebSocketConnecting);
        return _socket.ConnectAsync(url);
    }

    // 서버가 되돌려 줄 PUBLIC_CHAT만 목록에 반영한다는 계약으로 공개 채팅을 전송한다.
    public Task SendPublicChatAsync(string content)
    {
        ThrowIfDisposed();

        if (State != StompChatConnectionState.Connected)
        {
            throw new InvalidOperationException("STOMP 연결 완료 후에만 채팅을 전송할 수 있습니다.");
        }

        return _socket.SendTextAsync(_codec.CreateSendPublicChat(content));
    }

    // Lobby 종료 시 DISCONNECT를 먼저 보내고 transport를 닫는다.
    public async Task DisconnectAsync()
    {
        if (_disposed == true || State == StompChatConnectionState.Disconnected)
        {
            return;
        }

        SetState(StompChatConnectionState.Disconnecting);

        if (_socket.IsOpen == true)
        {
            await _socket.SendTextAsync(_codec.CreateDisconnect());
        }

        await _socket.CloseAsync();
        SetState(StompChatConnectionState.Disconnected);
    }

    // socket callback 구독을 해제해 Lobby 수명 종료 뒤의 수신을 막는다.
    public void Dispose()
    {
        if (_disposed == true)
        {
            return;
        }

        _disposed = true;
        _socket.Opened -= OnSocketOpened;
        _socket.MessageReceived -= OnSocketMessageReceived;
        _socket.ErrorReceived -= OnSocketErrorReceived;
        _socket.Closed -= OnSocketClosed;
    }

    // WebSocket open 직후에는 구독 대신 STOMP CONNECT부터 보낸다.
    private async void OnSocketOpened()
    {
        try
        {
            SetState(StompChatConnectionState.WebSocketConnected);
            SetState(StompChatConnectionState.StompConnecting);
            await _socket.SendTextAsync(_codec.CreateConnect(_host, _accessToken));
        }
        catch (Exception exception)
        {
            ProtocolErrorReceived?.Invoke(exception.Message);
        }
    }

    // 누적 codec 결과를 command별로 분기하되 transport는 frame 내용을 알지 못하게 유지한다.
    private void OnSocketMessageReceived(string text)
    {
        try
        {
            foreach (StompFrame frame in _codec.AppendReceivedText(text))
            {
                HandleFrame(frame);
            }
        }
        catch (Exception exception)
        {
            ProtocolErrorReceived?.Invoke(exception.Message);
        }
    }

    // CONNECTED 이후에만 고정된 네 destination을 구독하고 수신 command를 처리한다.
    private void HandleFrame(StompFrame frame)
    {
        if (frame.Command == StompCommand.Connected)
        {
            if (State != StompChatConnectionState.StompConnecting)
            {
                ProtocolErrorReceived?.Invoke("예상하지 못한 CONNECTED frame입니다.");
                return;
            }

            SetState(StompChatConnectionState.Connected);
            SubscribeRequiredDestinations();
            return;
        }

        if (frame.Command == StompCommand.Message)
        {
            HandleMessageFrame(frame);
            return;
        }

        if (frame.Command == StompCommand.Error)
        {
            HandleStompError(frame);
        }
    }

    // 서버 계약의 네 destination을 중복되지 않는 고정 ID로 순서대로 구독한다.
    private async void SubscribeRequiredDestinations()
    {
        try
        {
            await _socket.SendTextAsync(_codec.CreateSubscribe(PublicChatDestination, "sub-public-chat"));
            await _socket.SendTextAsync(_codec.CreateSubscribe(NoticesDestination, "sub-notices"));
            await _socket.SendTextAsync(_codec.CreateSubscribe(UserSystemDestination, "sub-system"));
            await _socket.SendTextAsync(_codec.CreateSubscribe(UserErrorDestination, "sub-errors"));
        }
        catch (Exception exception)
        {
            TransportErrorReceived?.Invoke(exception.Message);
        }
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
    private async void HandleStompError(StompFrame frame)
    {
        frame.TryGetHeader("message", out string headerMessage);
        string errorMessage = string.IsNullOrEmpty(headerMessage) ? frame.Body : $"{headerMessage}: {frame.Body}";
        ProtocolErrorReceived?.Invoke(errorMessage);
        SetState(StompChatConnectionState.Disconnected);

        try
        {
            await _socket.CloseAsync();
        }
        catch (Exception exception)
        {
            TransportErrorReceived?.Invoke(exception.Message);
        }
    }

    // transport 오류는 STOMP protocol 오류와 섞지 않고 상위 표시 계층에 전달한다.
    private void OnSocketErrorReceived(string errorMessage)
    {
        TransportErrorReceived?.Invoke(errorMessage);
    }

    // 원격 종료와 로컬 종료 모두 최종 연결 상태를 끊김으로 맞춘다.
    private void OnSocketClosed()
    {
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
