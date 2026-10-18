using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

// 채팅 오버레이의 열기·닫기, 메시지 목록, 입력 전송과 오류 표현만 담당한다.
public sealed class ChatOverlayView : BasePopup
{
    [Header("오버레이")]
    [SerializeField] private ButtonUI _closeButton;

    [Header("채팅 목록")]
    [SerializeField] private TMP_Text _messageItemTemplate;
    [SerializeField] private UnityEngine.UI.ScrollRect _messagesScrollRect;

    [Header("메시지 색상")]
    [SerializeField] private Color _publicChatColor = Color.white;
    [SerializeField] private Color _systemNoticeColor = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
    [SerializeField] private Color _swordEnhancementColor = new Color32(0xFF, 0x8A, 0x3D, 0xFF);

    [Header("입력")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private ButtonUI _sendButton;
    [SerializeField] private TMP_Text _errorText;

    private const float AutoScrollThreshold = 0.1f;

    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    // _messages와 같은 순서로 메시지마다 하나씩 두는 text 항목과, 목록에서 빠져 다시 쓸 항목
    private readonly List<TMP_Text> _messageItems = new List<TMP_Text>();
    private readonly Stack<TMP_Text> _idleMessageItems = new Stack<TMP_Text>();

    private ChatClient _chatClient;

    // BasePopup이 Dimmed를 bind한 뒤 Prefab 내부 button callback을 연결한다.
    // PopupUI에 꺼 둔 채로 배치되어 처음 Open할 때 실행되므로, 그 전에 표시한 연결 오류를 지우지 않는다.
    protected override void Awake()
    {
        base.Awake();

        _closeButton.OnClickEvent += Close;
        _sendButton.OnClickEvent += RequestSend;
    }

    // ChatClient 수신 이벤트를 view 갱신으로만 연결하고, 로비를 열기 전에 받아 둔 메시지도 목록에 넣는다.
    public void Bind(ChatClient chatClient)
    {
        if (_chatClient != null)
        {
            return;
        }

        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatClient.OnMessageAddedEvent += AddMessage;
        _chatClient.OnApplicationErrorEvent += ShowApplicationError;
        _chatClient.OnConnectionErrorEvent += ShowConnectionError;
        _chatClient.OnConnectedEvent += ClearConnectionError;

        foreach (ChatMessage message in _chatClient.Messages)
        {
            InsertMessage(message);
        }
    }

    // ChatClient는 로비보다 오래 남으므로, 로비 종료 전에 callback을 해제해 파괴 뒤 UI 갱신을 막는다.
    public void Unbind()
    {
        if (_chatClient == null)
        {
            return;
        }

        _chatClient.OnMessageAddedEvent -= AddMessage;
        _chatClient.OnApplicationErrorEvent -= ShowApplicationError;
        _chatClient.OnConnectionErrorEvent -= ShowConnectionError;
        _chatClient.OnConnectedEvent -= ClearConnectionError;
        _chatClient = null;
    }

    // UI 외부에서 발생한 연결 오류도 오버레이의 단일 오류 영역에 표시한다.
    private void ShowConnectionError(string errorMessage)
    {
        _errorText.text = errorMessage;
    }

    // 연결에 성공하면 이전 연결 오류와 재연결 안내 문구를 지운다.
    private void ClearConnectionError()
    {
        _errorText.text = string.Empty;
    }

    // 닫혀 있는 동안에는 layout이 계산되지 않으므로, 열 때 다시 계산하고 최신 메시지가 보이게 맨 아래로 내린다.
    protected override void OnOpen()
    {
        base.OnOpen();

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_messagesScrollRect.content);
        _messagesScrollRect.verticalNormalizedPosition = 0f;
    }

    // button callback을 해제해 Prefab 재생성이나 파괴 시 중복 구독을 남기지 않는다.
    private void OnDestroy()
    {
        _closeButton.OnClickEvent -= Close;
        _sendButton.OnClickEvent -= RequestSend;
        Unbind();
    }

    // client UX 검증 후 ChatClient에 전송을 요청하고 서버 echo 전에는 목록을 낙관 반영하지 않는다.
    private void RequestSend()
    {
        if (ChatInputValidator.TryValidate(_inputField.text, out string errorMessage) == false)
        {
            ShowApplicationError(errorMessage);
            return;
        }

        _ = SendAsync(_inputField.text);
    }

    // 연결 상태 오류를 입력 오류 영역에 표시하고 성공 요청 뒤에만 입력을 비운다.
    private async Task SendAsync(string content)
    {
        try
        {
            await _chatClient.SendPublicChatAsync(content);
            _inputField.text = string.Empty;
            _errorText.text = string.Empty;
        }
        catch (Exception exception)
        {
            ShowConnectionError(exception.Message);
        }
    }

    // 새 메시지 도착 직전 사용자가 맨 아래 근처에 있었을 때만 최신 메시지가 보이도록 따라 내려가고,
    // 과거 기록을 보던 중이면 콘텐츠 맨 위에서부터의 거리를 유지해 보던 위치가 밀리지 않게 한다.
    private void AddMessage(ChatMessage message)
    {
        if (IsOpened == false)
        {
            InsertMessage(message);
            return;
        }

        bool wasNearBottom = _messagesScrollRect.verticalNormalizedPosition <= AutoScrollThreshold;
        float distanceFromTop = (1f - _messagesScrollRect.verticalNormalizedPosition) * GetScrollableHeight();

        InsertMessage(message);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_messagesScrollRect.content);

        if (wasNearBottom)
        {
            _messagesScrollRect.verticalNormalizedPosition = 0f;
            return;
        }

        float scrollableHeight = GetScrollableHeight();
        if (scrollableHeight > 0f)
        {
            _messagesScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - distanceFromTop / scrollableHeight);
        }
    }

    // coordinator와 같은 sentAt 순서 위치에 메시지와 text 항목을 넣고, 새 메시지의 text만 그린다.
    private void InsertMessage(ChatMessage message)
    {
        ChatMessageOrdering.InsertBySentAt(_messages, message);
        int index = _messages.IndexOf(message);

        TMP_Text item = GetIdleMessageItem();
        item.text = FormatMessage(message);

        // 꺼 둔 항목이 사이에 있어도 순서가 맞도록, 같은 위치에 있던 항목 바로 앞에 둔다
        // 다시 쓰는 항목이 앞쪽에 있으면 SetSiblingIndex가 한 칸 뒤에 넣으므로, 먼저 맨 뒤로 보낸 다음 옮긴다
        item.transform.SetAsLastSibling();
        if (index < _messageItems.Count)
        {
            item.transform.SetSiblingIndex(_messageItems[index].transform.GetSiblingIndex());
        }

        item.gameObject.SetActive(true);
        _messageItems.Insert(index, item);
        TrimOldestMessages();
    }

    // 꺼 둔 항목이 있으면 다시 쓰고, 없으면 원본 항목을 복제한다.
    private TMP_Text GetIdleMessageItem()
    {
        if (_idleMessageItems.Count > 0)
        {
            return _idleMessageItems.Pop();
        }

        return Instantiate(_messageItemTemplate, _messageItemTemplate.transform.parent);
    }

    // 장시간 로비 접속 중에도 UI 항목 수가 무한히 늘지 않게 가장 오래된 메시지부터 버리고, 그 항목은 꺼서 다시 쓴다.
    private void TrimOldestMessages()
    {
        while (_messages.Count > ChatClient.MaximumMessageCount)
        {
            _messages.RemoveAt(0);

            TMP_Text item = _messageItems[0];
            _messageItems.RemoveAt(0);
            item.gameObject.SetActive(false);
            _idleMessageItems.Push(item);
        }
    }

    private float GetScrollableHeight()
    {
        return Mathf.Max(0f, _messagesScrollRect.content.rect.height - _messagesScrollRect.viewport.rect.height);
    }

    // `/user/queue/errors`의 application 오류는 연결 상태와 분리해 표시한다.
    private void ShowApplicationError(string errorMessage)
    {
        _errorText.text = errorMessage;
    }

    // 발신자 표시값이 없는 개인 시스템 메시지도 content는 그대로 확인할 수 있게 만들고,
    // 서버 type별 색상으로 공지·강화 성공·일반 채팅을 구분한다.
    private string FormatMessage(ChatMessage message)
    {
        string content = IsServerNotice(message.Type)
            ? message.Content
            : EscapeRichText(message.Content);

        // 강화 공지 문구에는 서버가 닉네임을 이미 넣으므로 앞에 다시 붙이지 않는다
        string body = IsServerNotice(message.Type) || string.IsNullOrWhiteSpace(message.SenderNickname)
            ? content
            : $"{EscapeRichText(message.SenderNickname)}: {content}";

        Color color = GetMessageColor(message.Type);
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{body}</color>";
    }

    // 서버가 작성한 공지와 강화 성공 문구는 일부 글자 강조 같은 rich text tag를 그대로 적용한다.
    private static bool IsServerNotice(string messageType)
    {
        return messageType == "SYSTEM_NOTICE" || messageType == "SWORD_ENHANCEMENT";
    }

    // 사용자가 입력한 닉네임과 채팅은 '\'와 '<' 뒤에 zero width space를 넣어 TMP가 \u003C 같은 escape나 tag로 해석하지 못하게 한다.
    // TMP는 &lt; 같은 HTML entity를 해석하지 않으므로 entity로 바꾸면 그 글자가 그대로 보인다.
    private static string EscapeRichText(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\u200B")
            .Replace("<", "<\u200B");
    }

    private Color GetMessageColor(string messageType)
    {
        switch (messageType)
        {
            case "SYSTEM_NOTICE":
                return _systemNoticeColor;
            case "SWORD_ENHANCEMENT":
                return _swordEnhancementColor;
            default:
                return _publicChatColor;
        }
    }
}
