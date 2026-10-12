using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 채팅 오버레이의 열기·닫기, 단일 목록, 입력 전송과 오류 표현만 담당한다.
public sealed class ChatOverlayView : BasePopup
{
    [Header("오버레이")]
    [SerializeField] private UnityEngine.UI.Button _openButton;
    [SerializeField] private UnityEngine.UI.Button _closeButton;

    [Header("채팅 목록")]
    [SerializeField] private TMP_Text _messagesText;
    [SerializeField] private UnityEngine.UI.ScrollRect _messagesScrollRect;

    [Header("메시지 색상")]
    [SerializeField] private Color _publicChatColor = Color.white;
    [SerializeField] private Color _systemNoticeColor = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
    [SerializeField] private Color _swordEnhancementColor = new Color32(0xFF, 0x8A, 0x3D, 0xFF);

    [Header("입력")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private UnityEngine.UI.Button _sendButton;
    [SerializeField] private TMP_Text _errorText;

    private const float AutoScrollThreshold = 0.1f;

    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    private ChatCoordinator _chatCoordinator;

    // BasePopup이 Dimmed를 bind한 뒤 Prefab 내부 button callback을 연결하고,
    // 패널은 활성 상태로 로드되므로 와이어링 직후 스스로 Close()를 호출해 기본적으로 닫아 둔다.
    protected override void Awake()
    {
        base.Awake();

        _openButton.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _sendButton.onClick.AddListener(RequestSend);
        _errorText.text = string.Empty;

        Close();
    }

    // coordinator 수신 이벤트를 view 갱신으로만 연결한다.
    public void Bind(ChatCoordinator chatCoordinator)
    {
        if (_chatCoordinator != null)
        {
            return;
        }

        _chatCoordinator = chatCoordinator ?? throw new ArgumentNullException(nameof(chatCoordinator));
        _chatCoordinator.MessageAdded += AddMessage;
        _chatCoordinator.ApplicationErrorReceived += ShowApplicationError;
        _chatCoordinator.ConnectionErrorReceived += ShowConnectionError;

        foreach (ChatMessage message in _chatCoordinator.Messages)
        {
            _messages.Add(message);
        }

        RefreshMessages();
    }

    // Lobby 종료 전에 coordinator callback을 해제해 파괴 뒤 UI 갱신을 막는다.
    public void Unbind()
    {
        if (_chatCoordinator == null)
        {
            return;
        }

        _chatCoordinator.MessageAdded -= AddMessage;
        _chatCoordinator.ApplicationErrorReceived -= ShowApplicationError;
        _chatCoordinator.ConnectionErrorReceived -= ShowConnectionError;
        _chatCoordinator = null;
    }

    // UI 외부에서 발생한 연결 오류도 오버레이의 단일 오류 영역에 표시한다.
    public void ShowConnectionError(string errorMessage)
    {
        _errorText.text = errorMessage;
    }

    // button callback을 해제해 Prefab 재생성이나 파괴 시 중복 구독을 남기지 않는다.
    private void OnDestroy()
    {
        _openButton.onClick.RemoveListener(Open);
        _closeButton.onClick.RemoveListener(Close);
        _sendButton.onClick.RemoveListener(RequestSend);
        Unbind();
    }

    // client UX 검증 후 coordinator에 전송을 요청하고 서버 echo 전에는 목록을 낙관 반영하지 않는다.
    private void RequestSend()
    {
        if (ChatInputValidator.TryValidate(_inputField.text, out string errorMessage) == false)
        {
            ShowApplicationError(errorMessage);
            return;
        }

        SendAsync(_inputField.text);
    }

    // 연결 상태 오류를 입력 오류 영역에 표시하고 성공 요청 뒤에만 입력을 비운다.
    private async void SendAsync(string content)
    {
        try
        {
            await _chatCoordinator.SendPublicChatAsync(content);
            _inputField.text = string.Empty;
            _errorText.text = string.Empty;
        }
        catch (Exception exception)
        {
            ShowConnectionError(exception.Message);
        }
    }

    // 모든 정상 서버 메시지를 도착 순서 목록에 보관하고 text list를 다시 그린다.
    private void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        RefreshMessages();
    }

    // `/user/queue/errors`의 application 오류는 연결 상태와 분리해 표시한다.
    private void ShowApplicationError(string errorMessage)
    {
        _errorText.text = errorMessage;
    }

    // UI Text를 단일 목록으로 유지하고, 새 메시지 도착 직전 사용자가 맨 아래 근처에 있었을 때만
    // 자동으로 최신 메시지가 보이도록 scroll 위치를 맞춘다. 과거 기록을 보던 중이면 위치를 유지한다.
    private void RefreshMessages()
    {
        bool wasNearBottom = _messagesScrollRect.verticalNormalizedPosition <= AutoScrollThreshold;

        _messagesText.text = string.Join("\n", _messages.ConvertAll(FormatMessage));
        Canvas.ForceUpdateCanvases();

        if (wasNearBottom)
        {
            _messagesScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 발신자 표시값이 없는 개인 시스템 메시지도 content는 그대로 확인할 수 있게 만들고,
    // 서버 type별 색상으로 공지·강화 성공·일반 채팅을 구분한다.
    private string FormatMessage(ChatMessage message)
    {
        string body = string.IsNullOrWhiteSpace(message.SenderNickname)
            ? message.Content
            : $"{message.SenderNickname}: {message.Content}";

        Color color = GetMessageColor(message.Type);
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{body}</color>";
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
