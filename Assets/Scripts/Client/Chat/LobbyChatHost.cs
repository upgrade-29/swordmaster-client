using System;
using UnityEngine;

// 로비에서 채팅창과 상단 공지를 ChatClient에 연결하고, 채팅이 연결되어 있지 않으면 연결을 요청한다.
// 연결은 ChatClient와 StompClient가 씬과 관계없이 유지하므로, 로비를 떠날 때는 view 연결만 해제한다.
public sealed class LobbyChatHost : MonoBehaviour
{
    private const string WebSocketPath = "/ws";

#if UNITY_EDITOR
    private const string EditorTestWebSocketUrlEnvironmentVariableName = "SWORDMASTER_CHAT_WS_URL";
    private const string EditorTestStompHostEnvironmentVariableName = "SWORDMASTER_CHAT_STOMP_HOST";
    private const string EditorTestAccessTokenEnvironmentVariableName = "SWORDMASTER_CHAT_ACCESS_TOKEN";
#endif

    [Header("연결 대상")]
    [SerializeField] private TopNoticeView _topNoticeView;

    private ChatOverlayView _chatOverlayView; // PopupUI에 있으므로 Start에서 LobbyPopupUI로 가져온다

    // 로그인하고 로비에 들어왔으면 로그인한 서버의 채팅에 연결한다. 이미 연결되어 있으면 ChatClient가 아무것도 하지 않는다.
    // 로그인 없이 연 경우 Editor PlayMode에서만 환경 변수로 전달된 일회성 연결 정보로 실제 서버 테스트를 시작한다.
    private void Start()
    {
        ChatClient chatClient = ChatClient.Instance;

        _chatOverlayView = LobbyPopupUI.Instance.Get<ChatOverlayView>();
        _chatOverlayView.Bind(chatClient);

        if (_topNoticeView != null)
        {
            _topNoticeView.Bind(chatClient);
        }

        if (GameSession.Instance.IsLoggedIn)
        {
            var serverUri = new Uri(ServerSettings.BaseUrl);
            chatClient.Connect(ToWebSocketUrl(serverUri), serverUri.Host, GameSession.Instance.AccessToken);
            return;
        }

#if UNITY_EDITOR
        TryConnectForEditorTest(chatClient);
#endif
    }

    // ChatClient는 로비보다 오래 남으므로 view 구독을 반드시 해제한다.
    // 한 번도 열지 않은 채팅창은 OnDestroy가 불리지 않고, 먼저 파괴되면 Unity의 null 비교가 true가 되므로 C# 참조로 확인한다.
    // Unbind는 C# 필드와 이벤트만 다루므로 파괴된 view에서 호출해도 안전하다.
    private void OnDestroy()
    {
        if (ReferenceEquals(_chatOverlayView, null) == false)
        {
            _chatOverlayView.Unbind();
        }

        if (ReferenceEquals(_topNoticeView, null) == false)
        {
            _topNoticeView.Unbind();
        }
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

#if UNITY_EDITOR
    // 세 환경 변수가 모두 있을 때만 연결해 Token을 Prefab·Scene·소스에 남기지 않는다.
    private void TryConnectForEditorTest(ChatClient chatClient)
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

        chatClient.Connect(webSocketUrl, stompHost, accessToken);
    }
#endif
}
