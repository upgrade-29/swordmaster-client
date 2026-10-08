using System;
using System.Globalization;
using Newtonsoft.Json;

// 서버 JSON을 수신한 직후 계약 필드를 보존하는 wire DTO다.
public sealed class ChatMessageDto
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("senderId")]
    public long? SenderId { get; set; }

    [JsonProperty("senderNickname")]
    public string SenderNickname { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }

    [JsonProperty("swordLevel")]
    public int? SwordLevel { get; set; }

    [JsonProperty("sentAt")]
    public string SentAt { get; set; }

    // 필수 표시 값과 UTC 시각이 유효할 때만 UI용 불변 모델로 변환한다.
    public bool TryToChatMessage(out ChatMessage chatMessage)
    {
        chatMessage = null;

        if (string.IsNullOrEmpty(Type) || string.IsNullOrEmpty(Content))
        {
            return false;
        }

        if (DateTimeOffset.TryParse(
                SentAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset sentAt) == false)
        {
            return false;
        }

        chatMessage = new ChatMessage(Type, SenderId, SenderNickname, Content, SwordLevel, sentAt);
        return true;
    }
}
