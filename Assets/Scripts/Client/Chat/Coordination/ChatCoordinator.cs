using System;
using System.Collections.Generic;

// 수신 destination과 서버 type을 함께 확인해 채팅 목록과 상단 공지 요청을 분기한다.
public sealed class ChatCoordinator : IDisposable
{
    private const string PublicChatDestination = "/topic/chat/public";
    private const string NoticesDestination = "/topic/notices";
    private const string UserSystemDestination = "/user/queue/system";

    private readonly StompChatService _chatService;
    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    private bool _disposed;

    public ChatCoordinator(StompChatService chatService)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _chatService.MessageReceived += OnMessageReceived;
        _chatService.ApplicationErrorReceived += OnApplicationErrorReceived;
        _chatService.TransportErrorReceived += OnConnectionErrorReceived;
        _chatService.ProtocolErrorReceived += OnConnectionErrorReceived;
    }

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public event Action<ChatMessage> MessageAdded;
    public event Action<ChatMessage> TopNoticeRequested;
    public event Action<string> ApplicationErrorReceived;
    public event Action<string> ConnectionErrorReceived;

    // View가 protocol과 transport를 알지 않고 공개 채팅 전송만 요청할 수 있게 한다.
    public System.Threading.Tasks.Task SendPublicChatAsync(string content)
    {
        ThrowIfDisposed();
        return _chatService.SendPublicChatAsync(content);
    }

    // service callback 구독을 해제해 Coordinator가 더는 UI 갱신을 요청하지 않게 한다.
    public void Dispose()
    {
        if (_disposed == true)
        {
            return;
        }

        _disposed = true;
        _chatService.MessageReceived -= OnMessageReceived;
        _chatService.ApplicationErrorReceived -= OnApplicationErrorReceived;
        _chatService.TransportErrorReceived -= OnConnectionErrorReceived;
        _chatService.ProtocolErrorReceived -= OnConnectionErrorReceived;
    }

    // destination과 type 조합이 계약에 맞는 경우에만 단일 목록과 공지 표시를 갱신한다.
    private void OnMessageReceived(string destination, ChatMessageDto messageDto)
    {
        if (messageDto.TryToChatMessage(out ChatMessage message) == false)
        {
            ConnectionErrorReceived?.Invoke("채팅 MESSAGE DTO 형식이 올바르지 않습니다.");
            return;
        }

        if (IsAllowedMessage(destination, message.Type) == false)
        {
            ConnectionErrorReceived?.Invoke("destination과 메시지 type 조합이 서버 계약과 다릅니다.");
            return;
        }

        ChatMessageOrdering.InsertBySentAt(_messages, message);
        MessageAdded?.Invoke(message);

        if (message.Type == "SYSTEM_NOTICE" || message.Type == "SWORD_ENHANCEMENT")
        {
            TopNoticeRequested?.Invoke(message);
        }
    }

    // 서버의 입력 검증 오류는 연결을 끊지 않고 별도 UI 오류로 전달한다.
    private void OnApplicationErrorReceived(string errorMessage)
    {
        ApplicationErrorReceived?.Invoke(errorMessage);
    }

    // transport와 STOMP protocol 오류는 입력 오류와 분리해 연결 오류로 전달한다.
    private void OnConnectionErrorReceived(string errorMessage)
    {
        ConnectionErrorReceived?.Invoke(errorMessage);
    }

    // 문서에 정의된 destination과 type 조합만 정상 수신 메시지로 인정한다.
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

    // 해제된 Coordinator를 View가 다시 사용하지 못하게 막는다.
    private void ThrowIfDisposed()
    {
        if (_disposed == true)
        {
            throw new ObjectDisposedException(nameof(ChatCoordinator));
        }
    }
}
