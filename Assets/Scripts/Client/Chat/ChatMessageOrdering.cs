using System.Collections.Generic;

// 단일 채팅 목록이 서버가 부여한 시각 순서를 유지하도록 삽입 위치를 계산한다.
public static class ChatMessageOrdering
{
    // 같은 시각에는 별도 식별자를 만들지 않고 현재 수신 순서를 유지해 목록에 삽입한다.
    public static void InsertBySentAt(IList<ChatMessage> messages, ChatMessage message)
    {
        int insertIndex = 0;

        while (insertIndex < messages.Count && messages[insertIndex].SentAt <= message.SentAt)
        {
            insertIndex++;
        }

        messages.Insert(insertIndex, message);
    }
}
