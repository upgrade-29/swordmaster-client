using System;

// 서버가 확정한 수신 정보를 UI가 표현할 수 있는 불변 메시지로 보관한다.
public sealed class ChatMessage
{
    public ChatMessage(
        string type,
        long? senderId,
        string senderNickname,
        string content,
        int? swordLevel,
        DateTimeOffset sentAt)
    {
        Type = type;
        SenderId = senderId;
        SenderNickname = senderNickname;
        Content = content;
        SwordLevel = swordLevel;
        SentAt = sentAt;
    }

    public string Type { get; }

    public long? SenderId { get; }

    public string SenderNickname { get; }

    public string Content { get; }

    public int? SwordLevel { get; }

    public DateTimeOffset SentAt { get; }
}
