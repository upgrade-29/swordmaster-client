using System;
using System.Linq;
using TMPro;
using UnityEngine;

// 로비 채팅 버튼에 가장 최근 일반 채팅 한 줄을 "닉네임 : 내용"으로 표현한다.
public sealed class ChatPreviewView : MonoBehaviour
{
    private const string PublicChatType = "PUBLIC_CHAT";

    [SerializeField] private TMP_Text _messageText;

    private ChatClient _chatClient;

    // 이미 받아 둔 메시지가 있으면 바로 보여 주고, 이후 들어오는 일반 채팅으로 교체한다.
    public void Bind(ChatClient chatClient)
    {
        if (_chatClient != null)
        {
            return;
        }

        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatClient.OnMessageAddedEvent += OnMessageAdded;
        ShowLatestPublicChat();
    }

    // ChatClient는 로비보다 오래 남으므로, 로비 종료 전에 수신 callback을 해제해 파괴된 text를 갱신하지 않게 한다.
    public void Unbind()
    {
        if (_chatClient == null)
        {
            return;
        }

        _chatClient.OnMessageAddedEvent -= OnMessageAdded;
        _chatClient = null;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    // 메시지는 서버 시각 순서로 끼워 넣어지므로, 늦게 도착한 과거 메시지가 최신 줄을 덮지 않게 목록에서 다시 고른다.
    private void ShowLatestPublicChat()
    {
        ChatMessage latest = _chatClient.Messages.LastOrDefault(x => x.Type == PublicChatType);
        _messageText.text = latest == null
            ? string.Empty
            : $"{latest.SenderNickname} : {latest.Content}";
    }

    private void OnMessageAdded(ChatMessage message)
    {
        if (message.Type != PublicChatType)
        {
            return;
        }

        ShowLatestPublicChat();
    }
}
