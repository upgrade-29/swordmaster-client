using System;
using System.Threading.Tasks;
using UnityEngine;

// Lobby 수명 동안 채팅 service와 coordinator를 조립하고 runtime 연결 정보를 받아 연결을 제어한다.
public sealed class LobbyChatHost : MonoBehaviour
{
#if UNITY_EDITOR
    private const string EditorTestWebSocketUrlEnvironmentVariableName = "SWORDMASTER_CHAT_WS_URL";
    private const string EditorTestStompHostEnvironmentVariableName = "SWORDMASTER_CHAT_STOMP_HOST";
    private const string EditorTestAccessTokenEnvironmentVariableName = "SWORDMASTER_CHAT_ACCESS_TOKEN";
#endif

    [Header("연결 대상")]
    [SerializeField] private ChatOverlayView _chatOverlayView;
    [SerializeField] private TopNoticeView _topNoticeView;

    private StompChatService _chatService;
    private ChatCoordinator _chatCoordinator;
    private bool _isDestroying;

    // Lobby 수명 Prefab이 채팅·공지 view를 조립하되 인증 정보가 주입되기 전에는 연결을 시작하지 않는다.
    private void Awake()
    {
        _chatService = new StompChatService(new NativeWebSocketStompSocket(), new StompFrameCodec());
        _chatCoordinator = new ChatCoordinator(_chatService);
        BindViews();
    }

#if UNITY_EDITOR
    // Editor PlayMode에서만 환경 변수로 전달된 일회성 연결 정보로 실제 서버 테스트를 시작한다.
    private void Start()
    {
        TryConnectForEditorTest();
    }
#endif

    // 로그인 성공 경계가 제공한 WebSocket URL, STOMP host, access token으로 연결을 시작한다.
    public void ConfigureAndConnect(string webSocketUrl, string stompHost, string accessToken)
    {
        if (_isDestroying == true)
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

        ConnectAsync(webSocketUrl, stompHost, accessToken);
    }

    // DISCONNECT와 transport close 순서를 마친 뒤 coordinator와 view callback을 해제한다.
    private async void OnDestroy()
    {
        _isDestroying = true;
        UnbindViews();

        if (_chatService != null)
        {
            try
            {
                await _chatService.DisconnectAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"채팅 종료 중 오류가 발생했습니다. {exception.Message}");
            }
        }

        _chatCoordinator?.Dispose();
        _chatService?.Dispose();
    }

    // 연결 중 발생한 transport·protocol 오류는 view가 이미 구독한 coordinator 이벤트로 전달한다.
    private async void ConnectAsync(string webSocketUrl, string stompHost, string accessToken)
    {
        try
        {
            await _chatService.ConnectAsync(webSocketUrl, stompHost, accessToken);
        }
        catch (Exception exception)
        {
            if (_isDestroying == false)
            {
                ShowChatOverlayError(exception.Message);
            }
        }
    }

    // Scene에 배치된 선택적 채팅 오버레이와 Lobby 상단 공지를 같은 coordinator에 연결한다.
    private void BindViews()
    {
        if (_chatOverlayView != null)
        {
            _chatOverlayView.Bind(_chatCoordinator);
        }

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

    // BattleTab 채팅 UI가 아직 연결되지 않은 상태에서도 연결 실패가 예외로 번지지 않게 한다.
    private void ShowChatOverlayError(string errorMessage)
    {
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
