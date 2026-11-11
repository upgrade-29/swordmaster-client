using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// socket transport와 frame codec을 조합해 STOMP 연결 순서를 조정하고, 등록받은 destination을 구독해 수신 body를 destination별로 전달한다.
public sealed class StompConnection : IDisposable
{
    private readonly IStompSocket _socket;
    private readonly StompFrameCodec _codec;

    private IReadOnlyList<string> _destinations = Array.Empty<string>();
    private string _host;
    private string _accessToken;
    private int _nextConnectionId;
    private int _activeConnectionId;
    private int _closeRequestedConnectionId;
    private Task _closeTask;
    private bool _disposed;

    public StompConnection(IStompSocket socket, StompFrameCodec codec)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));

        _socket.Opened += OnSocketOpened;
        _socket.MessageReceived += OnSocketMessageReceived;
        _socket.ErrorReceived += OnSocketErrorReceived;
        _socket.Closed += OnSocketClosed;
    }

    public StompConnectionState State { get; private set; } = StompConnectionState.Disconnected;
    public bool IsNormalDisconnectRequested { get; private set; }
    public bool IsRejectedByServer { get; private set; }

    public event Action<StompConnectionState> StateChanged;
    public event Action<string, string> MessageReceived;
    public event Action<string> TransportErrorReceived;
    public event Action<string> ProtocolErrorReceived;

    // URL·host·token과 이번 연결에서 구독할 destination 목록을 보관하고 WebSocket 연결을 시작한다.
    public async Task ConnectAsync(string url, string host, string accessToken, IReadOnlyList<string> destinations)
    {
        ThrowIfDisposed();

        if (State != StompConnectionState.Disconnected)
        {
            throw new InvalidOperationException("이미 연결을 진행 중이거나 연결되어 있습니다.");
        }

        int connectionId = ++_nextConnectionId;
        _activeConnectionId = connectionId;
        _closeRequestedConnectionId = 0;
        _closeTask = null;
        IsNormalDisconnectRequested = false;
        IsRejectedByServer = false;
        _destinations = new List<string>(destinations);
        _host = host;
        _accessToken = accessToken;
        _codec.Reset();
        SetState(StompConnectionState.WebSocketConnecting);

        try
        {
            await _socket.ConnectAsync(url, connectionId);
        }
        catch (Exception exception)
        {
            if (IsActiveConnection(connectionId) == true
                && State != StompConnectionState.Disconnecting
                && State != StompConnectionState.Disconnected)
            {
                TransportErrorReceived?.Invoke(exception.Message);
                CompleteDisconnected(connectionId);
            }

            throw;
        }
    }

    // 구독까지 끝난 연결에서만 destination으로 JSON body를 보낸다.
    public Task SendAsync(string destination, string body)
    {
        ThrowIfDisposed();

        if (State != StompConnectionState.Connected)
        {
            throw new InvalidOperationException("STOMP 연결과 구독이 끝난 뒤에만 보낼 수 있습니다.");
        }

        return _socket.SendTextAsync(_codec.CreateSend(destination, body));
    }

    // 연결을 끝낼 때 DISCONNECT를 먼저 보내고 transport를 닫는다.
    public Task DisconnectAsync()
    {
        if (_disposed == true || State == StompConnectionState.Disconnected)
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

    // socket callback 구독을 해제해 소유자 수명 종료 뒤의 수신을 막는다.
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

        if (State != StompConnectionState.Disconnected)
        {
            SetState(StompConnectionState.Disconnected);
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
        if (IsActiveConnection(connectionId) == false || State != StompConnectionState.WebSocketConnecting)
        {
            return;
        }

        try
        {
            SetState(StompConnectionState.WebSocketConnected);
            SetState(StompConnectionState.StompConnecting);
            await _socket.SendTextAsync(_codec.CreateConnect(_host, _accessToken));
        }
        catch (Exception exception)
        {
            if (IsActiveConnection(connectionId) == false
                || State == StompConnectionState.Disconnecting
                || State == StompConnectionState.Disconnected)
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
            || State == StompConnectionState.Disconnecting
            || State == StompConnectionState.Disconnected)
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
            if (State != StompConnectionState.StompConnecting)
            {
                ProtocolErrorReceived?.Invoke("예상하지 못한 CONNECTED frame입니다.");
                return;
            }

            SetState(StompConnectionState.Subscribing);
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

    // 등록받은 destination을 목록 순서대로, 연결 안에서 겹치지 않는 ID로 구독한다.
    private async Task SubscribeRequiredDestinationsAsync(int connectionId)
    {
        try
        {
            for (int index = 0; index < _destinations.Count; index++)
            {
                await SendSubscribeAsync(connectionId, _destinations[index], $"sub-{index.ToString()}");
            }

            if (IsActiveConnection(connectionId) == true && State == StompConnectionState.Subscribing)
            {
                SetState(StompConnectionState.Connected);
            }
        }
        catch (OperationCanceledException) when (IsActiveConnection(connectionId) == false
                                                 || State == StompConnectionState.Disconnecting
                                                 || State == StompConnectionState.Disconnected)
        {
            // 종료 요청이 들어온 뒤에는 아직 보내지 않은 SUBSCRIBE frame을 전송하지 않는다.
        }
        catch (Exception exception)
        {
            if (IsActiveConnection(connectionId) == false
                || State == StompConnectionState.Disconnecting
                || State == StompConnectionState.Disconnected)
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
        if (IsActiveConnection(connectionId) == false || State != StompConnectionState.Subscribing)
        {
            throw new OperationCanceledException("구독 전송이 중단되었습니다.");
        }

        return _socket.SendTextAsync(_codec.CreateSubscribe(destination, subscriptionId));
    }

    // body 해석은 구독한 기능이 맡으므로 destination과 body만 그대로 전달한다.
    private void HandleMessageFrame(StompFrame frame)
    {
        if (frame.TryGetHeader("destination", out string destination) == false)
        {
            ProtocolErrorReceived?.Invoke("MESSAGE frame에 destination header가 없습니다.");
            return;
        }

        MessageReceived?.Invoke(destination, frame.Body);
    }

    // STOMP ERROR는 application 오류와 달리 종료 상태로 전환하고 transport 종료를 요청한다.
    // 서버는 인증 실패나 허용되지 않은 주소처럼 다시 연결해도 같은 결과인 경우에만 ERROR를 보내므로 재연결하지 않게 표시한다.
    private void HandleStompError(int connectionId, StompFrame frame)
    {
        IsRejectedByServer = true;
        frame.TryGetHeader("message", out string headerMessage);

        // header와 body 중 비어 있는 쪽은 빼고 이어 붙인다
        string errorMessage = headerMessage;
        if (string.IsNullOrEmpty(headerMessage))
        {
            errorMessage = frame.Body;
        }
        else if (string.IsNullOrWhiteSpace(frame.Body) == false)
        {
            errorMessage = $"{headerMessage}: {frame.Body}";
        }

        ProtocolErrorReceived?.Invoke(errorMessage);
        _ = CloseAfterFailureAsync(connectionId);
    }

    // transport 오류는 STOMP protocol 오류와 섞지 않고 상위 표시 계층에 전달한다.
    private void OnSocketErrorReceived(int connectionId, string errorMessage)
    {
        if (IsActiveConnection(connectionId) == true
            && State != StompConnectionState.Disconnecting
            && State != StompConnectionState.Disconnected)
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
            TransportErrorReceived?.Invoke("서버와의 연결이 끊겼습니다.");
        }

        CompleteDisconnected(connectionId);
    }

    // CONNECT 또는 SUBSCRIBE 실패는 같은 close 작업을 공유해 종료 요청을 중복하지 않는다.
    private async Task CloseAfterFailureAsync(int connectionId)
    {
        if (IsActiveConnection(connectionId) == false
            || State == StompConnectionState.Disconnecting
            || State == StompConnectionState.Disconnected)
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
            SetState(StompConnectionState.Disconnecting);
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
        SetState(StompConnectionState.Disconnected);
    }

    // 상태 변경을 한 지점에 모아 UI가 WebSocket과 STOMP 성공을 구분할 수 있게 한다.
    private void SetState(StompConnectionState state)
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
            throw new ObjectDisposedException(nameof(StompConnection));
        }
    }
}
