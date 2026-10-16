using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Lobby 수명 동안 채팅 service와 coordinator를 조립하고 runtime 연결 정보를 받아 연결을 제어한다.
public sealed class LobbyChatHost : MonoBehaviour
{
    private const string WebSocketPath = "/ws";
    private const int MaximumReconnectAttempts = 3;

#if UNITY_EDITOR
    private const string EditorTestWebSocketUrlEnvironmentVariableName = "SWORDMASTER_CHAT_WS_URL";
    private const string EditorTestStompHostEnvironmentVariableName = "SWORDMASTER_CHAT_STOMP_HOST";
    private const string EditorTestAccessTokenEnvironmentVariableName = "SWORDMASTER_CHAT_ACCESS_TOKEN";
#endif

    [Header("연결 대상")]
    [SerializeField] private TopNoticeView _topNoticeView;

    private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();

    private ChatOverlayView _chatOverlayView; // PopupUI에 있으므로 Start에서 LobbyPopupUI로 가져온다
    private StompChatService _chatService;
    private ChatCoordinator _chatCoordinator;
    private Task _reconnectTask;
    private Task _shutdownTask;
    private bool _isShuttingDown;
    private bool _resourcesDisposed;
    private bool _hasReportedReconnectFailure;
    private int _reconnectAttemptCount;
    private string _webSocketUrl;
    private string _stompHost;
    private string _accessToken;

    // Lobby 수명 Prefab이 채팅·공지 view를 조립하되 인증 정보가 주입되기 전에는 연결을 시작하지 않는다.
    private void Awake()
    {
        _chatService = new StompChatService(new NativeWebSocketStompSocket(), new StompFrameCodec());
        _chatService.StateChanged += OnChatServiceStateChanged;
        _chatCoordinator = new ChatCoordinator(_chatService);
        BindViews();
    }

    // 로그인하고 로비에 들어왔으면 로그인한 서버의 채팅에 연결한다.
    // 로그인 없이 연 경우 Editor PlayMode에서만 환경 변수로 전달된 일회성 연결 정보로 실제 서버 테스트를 시작한다.
    private void Start()
    {
        _chatOverlayView = LobbyPopupUI.Instance.Get<ChatOverlayView>();
        _chatOverlayView.Bind(_chatCoordinator);

        if (GameSession.Instance.IsLoggedIn)
        {
            var serverUri = new Uri(ServerSettings.BaseUrl);
            ConfigureAndConnect(ToWebSocketUrl(serverUri), serverUri.Host, GameSession.Instance.AccessToken);
            return;
        }

#if UNITY_EDITOR
        TryConnectForEditorTest();
#endif
    }

    // 로그인한 서버 주소(http://host:port)에서 STOMP endpoint 주소(ws://host:port/ws)를 만든다.
    private static string ToWebSocketUrl(Uri serverUri)
    {
        var builder = new UriBuilder(serverUri)
        {
            Scheme = serverUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
            Path = WebSocketPath,
        };
        return builder.Uri.ToString();
    }

    // Start에서 받은 WebSocket URL, STOMP host, access token으로 연결을 시작한다.
    private void ConfigureAndConnect(string webSocketUrl, string stompHost, string accessToken)
    {
        if (_isShuttingDown == true)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(webSocketUrl)
            || string.IsNullOrWhiteSpace(stompHost)
            || string.IsNullOrWhiteSpace(accessToken))
        {
            ShowChatOverlayError("채팅 연결 정보를 확인할 수 없습니다.");
            return;
        }

        if (_chatService.State != StompChatConnectionState.Disconnected)
        {
            ShowChatOverlayError("이미 채팅 연결을 진행 중이거나 연결되어 있습니다.");
            return;
        }

        _webSocketUrl = webSocketUrl;
        _stompHost = stompHost;
        _accessToken = accessToken;
        _reconnectAttemptCount = 0;
        _hasReportedReconnectFailure = false;
        StartConnection();
    }

    // 로비가 아직 살아 있는 정상 종료 경로는 이 Task를 await해 DISCONNECT와 close 완료를 기다릴 수 있다.
    public Task ShutdownAsync()
    {
        if (_shutdownTask == null)
        {
            _shutdownTask = ShutdownCoreAsync();
        }

        return _shutdownTask;
    }

    // Unity는 OnDestroy의 Task 완료를 기다리지 않으므로 동기적으로 callback·재연결·socket 정리만 시작한다.
    private void OnDestroy()
    {
        BeginShutdown();
        DisposeResources();
    }

    // 정상 종료에서는 먼저 DISCONNECT/close를 기다리고, 완료 뒤 event와 coordinator를 해제한다.
    private async Task ShutdownCoreAsync()
    {
        BeginShutdown();

        try
        {
            await _chatService.DisconnectAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"채팅 종료 중 오류가 발생했습니다. {exception.Message}");
        }
        finally
        {
            DisposeResources();
        }
    }

    // 예외를 내부에서 UI 오류로 변환해 호출자가 관찰하지 않는 연결 Task를 남기지 않는다.
    private async Task ConnectAsync()
    {
        try
        {
            await _chatService.ConnectAsync(_webSocketUrl, _stompHost, _accessToken);
        }
        catch (Exception exception)
        {
            if (_isShuttingDown == false)
            {
                ShowChatOverlayError(exception.Message);
            }
        }
    }

    // service 연결은 socket close까지 오래 유지될 수 있으므로 재연결 대기 Task와 별도로 소유한다.
    private void StartConnection()
    {
        if (_isShuttingDown == false && _chatService.State == StompChatConnectionState.Disconnected)
        {
            _ = ConnectAsync();
        }
    }

    // 비정상 종료 뒤에는 보관한 로그인 연결 정보로 제한된 횟수만 재연결한다. 서버가 거절한 연결은 다시 시도하지 않는다.
    private void OnChatServiceStateChanged(StompChatConnectionState state)
    {
        if (state == StompChatConnectionState.Connected)
        {
            _reconnectAttemptCount = 0;
            _hasReportedReconnectFailure = false;

            if (_chatOverlayView != null)
            {
                _chatOverlayView.ClearConnectionError();
            }

            return;
        }

        if (state == StompChatConnectionState.Disconnected
            && _chatService.IsNormalDisconnectRequested == false
            && _chatService.IsRejectedByServer == false)
        {
            StartReconnect();
        }
    }

    // 이벤트 callback에서는 Task를 시작만 하고, 실제 예외·취소 처리는 Task 반환 메서드에서 끝낸다.
    private void StartReconnect()
    {
        if (_isShuttingDown == true
            || _reconnectTask != null
            || string.IsNullOrWhiteSpace(_webSocketUrl))
        {
            return;
        }

        // 대기 없이 끝나는 Task를 _reconnectTask에 남기지 않도록 횟수 초과는 Task를 만들기 전에 거른다
        if (_reconnectAttemptCount >= MaximumReconnectAttempts)
        {
            ReportReconnectFailure();
            return;
        }

        _reconnectTask = ScheduleReconnectAsync();
    }

    // 이전 시도와 겹치지 않게 1초, 2초, 4초 backoff로 최대 세 번만 다시 연결한다.
    private async Task ScheduleReconnectAsync()
    {
        try
        {
            _reconnectAttemptCount++;
            int delaySeconds = 1 << (_reconnectAttemptCount - 1);
            ShowChatOverlayError($"채팅 연결이 끊겼습니다. 재연결을 시도합니다. ({_reconnectAttemptCount}/{MaximumReconnectAttempts})");
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _lifetimeCancellation.Token);

            if (_isShuttingDown == false && _chatService.State == StompChatConnectionState.Disconnected)
            {
                StartConnection();
            }
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            // Lobby 종료가 대기 중인 재연결을 취소한 정상 경로다.
        }
        finally
        {
            _reconnectTask = null;

            if (_isShuttingDown == false && _chatService.State == StompChatConnectionState.Disconnected)
            {
                if (_reconnectAttemptCount >= MaximumReconnectAttempts)
                {
                    ReportReconnectFailure();
                }
                else
                {
                    StartReconnect();
                }
            }
        }
    }

    // 재연결 횟수를 모두 쓰면 실패 문구를 한 번만 표시한다.
    private void ReportReconnectFailure()
    {
        if (_hasReportedReconnectFailure == false)
        {
            _hasReportedReconnectFailure = true;
            ShowChatOverlayError("채팅 재연결에 실패했습니다.");
        }
    }

    // 종료 시작 시 이후 continuation이 Unity view에 접근하지 못하게 먼저 callback과 대기를 끊는다.
    private void BeginShutdown()
    {
        if (_isShuttingDown == true)
        {
            return;
        }

        _isShuttingDown = true;
        _lifetimeCancellation.Cancel();
        UnbindViews();

        if (_chatService != null)
        {
            _chatService.StateChanged -= OnChatServiceStateChanged;
        }
    }

    // 정상 종료 완료 또는 Unity 파괴 중복 호출에도 coordinator와 service를 한 번만 해제한다.
    private void DisposeResources()
    {
        if (_resourcesDisposed == true)
        {
            return;
        }

        _resourcesDisposed = true;
        _chatCoordinator?.Dispose();
        _chatService?.Dispose();
        _lifetimeCancellation.Dispose();
    }

    // Lobby 상단 공지를 coordinator에 연결한다. 채팅창은 PopupUI에 있으므로 Start에서 연결한다.
    private void BindViews()
    {
        if (_topNoticeView != null)
        {
            _topNoticeView.Bind(_chatCoordinator);
        }
    }

    // 수명 종료 시 실제로 연결된 view만 해제해 별도 Prefab 배치를 허용한다.
    private void UnbindViews()
    {
        if (_chatOverlayView != null)
        {
            _chatOverlayView.Unbind();
        }

        if (_topNoticeView != null)
        {
            _topNoticeView.Unbind();
        }
    }

    // 채팅창을 가져오기 전에 오류가 나면 예외 대신 경고 로그만 남긴다.
    private void ShowChatOverlayError(string errorMessage)
    {
        if (_isShuttingDown == true)
        {
            return;
        }

        if (_chatOverlayView != null)
        {
            _chatOverlayView.ShowConnectionError(errorMessage);
            return;
        }

        Debug.LogWarning($"채팅 연결 오류: {errorMessage}");
    }

#if UNITY_EDITOR
    // 세 환경 변수가 모두 있을 때만 연결해 Token을 Prefab·Scene·소스에 남기지 않는다.
    private void TryConnectForEditorTest()
    {
        string webSocketUrl = Environment.GetEnvironmentVariable(EditorTestWebSocketUrlEnvironmentVariableName);
        string stompHost = Environment.GetEnvironmentVariable(EditorTestStompHostEnvironmentVariableName);
        string accessToken = Environment.GetEnvironmentVariable(EditorTestAccessTokenEnvironmentVariableName);

        bool hasAnyEditorTestConnectionValue = string.IsNullOrWhiteSpace(webSocketUrl) == false
                                                || string.IsNullOrWhiteSpace(stompHost) == false
                                                || string.IsNullOrWhiteSpace(accessToken) == false;
        if (hasAnyEditorTestConnectionValue == false)
        {
            return;
        }

        ConfigureAndConnect(webSocketUrl, stompHost, accessToken);
    }
#endif
}
