using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// 채팅 destination을 StompClient에 구독하고, 받은 메시지를 서버 시각 순서로 최근 MaximumMessageCount개까지 보관합니다.
/// 처음 사용할 때 싱글턴 오브젝트를 만들고 씬이 바뀌어도 유지하므로, 전투에 다녀와도 받은 메시지가 남아 있습니다.
/// </summary>
public class ChatClient : SingletonMonoBehaviour<ChatClient>
{
    public const int MaximumMessageCount = 200;

    private const string PublicChatDestination = "/topic/chat/public";
    private const string NoticesDestination = "/topic/notices";
    private const string UserSystemDestination = "/user/queue/system";
    private const string UserErrorDestination = "/user/queue/errors";
    private const string PublicChatSendDestination = "/app/chat/public";

    private readonly List<ChatMessage> listMessage = new List<ChatMessage>();
    public IReadOnlyList<ChatMessage> Messages => listMessage;

    private readonly List<ChatMessage> listHeldTopNotice = new List<ChatMessage>();
    private bool isTopNoticeHeld;

    public event Action<ChatMessage> OnMessageAddedEvent = delegate { };
    public event Action<ChatMessage> OnTopNoticeEvent = delegate { };
    public event Action<string> OnApplicationErrorEvent = delegate { };
    public event Action<string> OnConnectionErrorEvent = delegate { };
    public event Action OnConnectedEvent = delegate { };

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);

        StompClient stompClient = StompClient.Instance;
        stompClient.Subscribe(PublicChatDestination, OnPublicChatReceived);
        stompClient.Subscribe(NoticesDestination, OnNoticeReceived);
        stompClient.Subscribe(UserSystemDestination, OnUserSystemReceived);
        stompClient.Subscribe(UserErrorDestination, OnUserErrorReceived);

        stompClient.OnStateChangedEvent += OnStompStateChanged;
        stompClient.OnDisconnectRequestedEvent += OnStompDisconnectRequested;
        stompClient.OnErrorEvent += OnStompError;
        stompClient.OnReconnectScheduledEvent += OnStompReconnectScheduled;
        stompClient.OnReconnectFailedEvent += OnStompReconnectFailed;
    }

    protected override void OnDestroySingleton()
    {
        if (StompClient.IsInitialize() == false)
        {
            return;
        }

        StompClient stompClient = StompClient.Instance;
        stompClient.OnStateChangedEvent -= OnStompStateChanged;
        stompClient.OnDisconnectRequestedEvent -= OnStompDisconnectRequested;
        stompClient.OnErrorEvent -= OnStompError;
        stompClient.OnReconnectScheduledEvent -= OnStompReconnectScheduled;
        stompClient.OnReconnectFailedEvent -= OnStompReconnectFailed;
    }

    // 로비에 들어올 때마다 호출해도 되며, 이미 연결 중이거나 연결되어 있으면 아무것도 하지 않습니다.
    public void Connect(string webSocketUrl, string stompHost, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(webSocketUrl)
            || string.IsNullOrWhiteSpace(stompHost)
            || string.IsNullOrWhiteSpace(accessToken))
        {
            OnConnectionErrorEvent.Invoke("채팅 연결 정보를 확인할 수 없습니다.");
            return;
        }

        StompClient.Instance.Connect(webSocketUrl, stompHost, accessToken);
    }

    // 서버가 되돌려 줄 PUBLIC_CHAT만 목록에 반영하므로, 보낸 메시지를 목록에 미리 넣지 않습니다.
    public Task SendPublicChatAsync(string content)
    {
        if (ChatInputValidator.TryValidate(content, out string errorMessage) == false)
        {
            throw new ArgumentException(errorMessage, nameof(content));
        }

        string body = JsonConvert.SerializeObject(new { content });
        return StompClient.Instance.SendAsync(PublicChatSendDestination, body);
    }

    // 서버는 검 강화 결과를 응답보다 먼저 공지로 보내므로, 강화 연출이 끝날 때까지 상단 공지를 모아 두었다가 순서대로 내보낸다
    public void HoldTopNotices()
    {
        isTopNoticeHeld = true;
    }

    public void ReleaseTopNotices()
    {
        isTopNoticeHeld = false;
        foreach (ChatMessage notice in listHeldTopNotice)
        {
            OnTopNoticeEvent.Invoke(notice);
        }

        listHeldTopNotice.Clear();
    }

    // destination과 type 조합이 서버 계약에 맞는 메시지만 서버 시각 순서로 넣고, 상한을 넘으면 가장 오래된 메시지를 버린다
    private void AddReceivedMessage(string destination, string body)
    {
        var messageDto = JsonConvert.DeserializeObject<ChatMessageDto>(body);
        if (messageDto == null)
        {
            OnConnectionErrorEvent.Invoke("채팅 MESSAGE body 형식이 올바르지 않습니다.");
            return;
        }

        if (messageDto.TryToChatMessage(out ChatMessage message) == false)
        {
            OnConnectionErrorEvent.Invoke("채팅 MESSAGE DTO 형식이 올바르지 않습니다.");
            return;
        }

        if (IsAllowedMessage(destination, message.Type) == false)
        {
            OnConnectionErrorEvent.Invoke("destination과 메시지 type 조합이 서버 계약과 다릅니다.");
            return;
        }

        ChatMessageOrdering.InsertBySentAt(listMessage, message);
        if (listMessage.Count > MaximumMessageCount)
        {
            listMessage.RemoveAt(0);
        }

        OnMessageAddedEvent.Invoke(message);

        if (message.Type != "SYSTEM_NOTICE" && message.Type != "SWORD_ENHANCEMENT")
        {
            return;
        }

        if (isTopNoticeHeld)
        {
            listHeldTopNotice.Add(message);
            return;
        }

        OnTopNoticeEvent.Invoke(message);
    }

    private static bool IsAllowedMessage(string destination, string messageType)
    {
        if (destination == PublicChatDestination)
        {
            return messageType == "PUBLIC_CHAT";
        }

        if (destination == NoticesDestination)
        {
            return messageType == "SYSTEM_NOTICE" || messageType == "SWORD_ENHANCEMENT";
        }

        return destination == UserSystemDestination;
    }

    private void OnPublicChatReceived(string body)
    {
        AddReceivedMessage(PublicChatDestination, body);
    }

    private void OnNoticeReceived(string body)
    {
        AddReceivedMessage(NoticesDestination, body);
    }

    private void OnUserSystemReceived(string body)
    {
        AddReceivedMessage(UserSystemDestination, body);
    }

    // 서버의 입력 검증 오류는 연결을 끊지 않고 { message } 형식으로 온다
    private void OnUserErrorReceived(string body)
    {
        var errorDto = JsonConvert.DeserializeAnonymousType(body, new { message = string.Empty });
        if (string.IsNullOrEmpty(errorDto?.message))
        {
            OnConnectionErrorEvent.Invoke("오류 MESSAGE body 형식이 올바르지 않습니다.");
            return;
        }

        OnApplicationErrorEvent.Invoke(errorDto.message);
    }

    private void OnStompStateChanged(StompConnectionState state)
    {
        if (state == StompConnectionState.Connected)
        {
            OnConnectedEvent.Invoke();
        }
    }

    // 토큰 만료로 연결을 끊으면, 다음에 로그인한 사용자에게 이전 대화가 보이지 않게 지운다
    private void OnStompDisconnectRequested()
    {
        listMessage.Clear();
        listHeldTopNotice.Clear();
        isTopNoticeHeld = false;
    }

    private void OnStompError(string errorMessage)
    {
        OnConnectionErrorEvent.Invoke(errorMessage);
    }

    private void OnStompReconnectScheduled(int attempt, int maxAttempts)
    {
        OnConnectionErrorEvent.Invoke($"채팅 연결이 끊겼습니다. 재연결을 시도합니다. ({attempt.ToString()}/{maxAttempts.ToString()})");
    }

    private void OnStompReconnectFailed()
    {
        OnConnectionErrorEvent.Invoke("채팅 재연결에 실패했습니다.");
    }
}
