using System;
using TMPro;
using UnityEngine;

// 최신 시스템 공지 또는 강화 공지의 content 한 줄만 TopUI 아래에 표현한다.
public sealed class TopNoticeView : MonoBehaviour
{
    [SerializeField] private TMP_Text _noticeText;

    private ChatCoordinator _chatCoordinator;

    // coordinator의 상단 공지 요청만 구독해 일반 채팅이 banner에 표시되지 않게 한다.
    public void Bind(ChatCoordinator chatCoordinator)
    {
        if (_chatCoordinator != null)
        {
            return;
        }

        _chatCoordinator = chatCoordinator ?? throw new ArgumentNullException(nameof(chatCoordinator));
        _chatCoordinator.TopNoticeRequested += ShowNotice;
    }

    // Lobby 종료 전에 수신 callback을 해제해 파괴된 text를 갱신하지 않게 한다.
    public void Unbind()
    {
        if (_chatCoordinator == null)
        {
            return;
        }

        _chatCoordinator.TopNoticeRequested -= ShowNotice;
        _chatCoordinator = null;
    }

    // component 파괴 시에도 명시적으로 callback을 해제한다.
    private void OnDestroy()
    {
        Unbind();
    }

    // 이전 문구를 누적하지 않고 가장 최근 서버 공지 content로 교체한다.
    private void ShowNotice(ChatMessage message)
    {
        _noticeText.text = message.Content;
    }
}
