using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 시스템 공지 또는 강화 공지의 content를 TopUI 아래 띠에서 오른쪽에서 왼쪽으로 한 번만 흘려보낸다.
// 공지가 연달아 오면 앞의 공지가 다 지나간 뒤 차례로 흘린다.
public sealed class TopNoticeView : MonoBehaviour
{
    private const float ScrollSpeed = 150f; // 초당 이동 px

    private RectTransform _viewportRect; // 띠에서 아이콘·오른쪽 여백을 뺀 영역. RectMask2D로 밖의 글자를 잘라낸다
    private TMP_Text _noticeText;
    private RectTransform _noticeTextRect;

    private readonly Queue<string> _pendingNotices = new Queue<string>();
    private Tween _scrollTween;
    private ChatClient _chatClient;

    private void Awake()
    {
        _viewportRect = GameUtil.Bind<RectTransform>(gameObject, "Viewport");
        _noticeText = GameUtil.Bind<TMP_Text>(gameObject, "Viewport/NoticeText");
        _noticeTextRect = _noticeText.rectTransform;

        _noticeText.text = string.Empty;
    }

    // ChatClient의 상단 공지 이벤트만 구독해 일반 채팅이 banner에 표시되지 않게 한다.
    public void Bind(ChatClient chatClient)
    {
        if (_chatClient != null)
        {
            return;
        }

        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatClient.OnTopNoticeEvent += ShowNotice;
    }

    // ChatClient는 로비보다 오래 남으므로, 로비 종료 전에 수신 callback을 해제해 파괴된 text를 갱신하지 않게 한다.
    public void Unbind()
    {
        _scrollTween?.Kill();
        _scrollTween = null;
        _pendingNotices.Clear();

        if (_chatClient == null)
        {
            return;
        }

        _chatClient.OnTopNoticeEvent -= ShowNotice;
        _chatClient = null;
    }

    // component 파괴 시에도 명시적으로 callback을 해제한다.
    private void OnDestroy()
    {
        Unbind();
    }

    // 흘러가는 공지가 없으면 바로 흘리고, 있으면 대기열에 넣어 앞 공지가 끝난 뒤 흘린다.
    private void ShowNotice(ChatMessage message)
    {
        _pendingNotices.Enqueue(message.Content);

        if (_scrollTween == null)
        {
            ScrollNext();
        }
    }

    // 대기열의 다음 공지를 보이는 영역 오른쪽 끝에서 시작해 글자 끝이 왼쪽 끝을 지날 때까지 한 번 흘린다.
    private void ScrollNext()
    {
        if (_pendingNotices.Count == 0)
        {
            _scrollTween = null;
            _noticeText.text = string.Empty;
            return;
        }

        string content = _pendingNotices.Dequeue();
        _noticeText.text = content;

        // 줄바꿈 없이 한 줄로 그린 폭만큼 text 영역을 늘려 글자가 잘리지 않게 한다
        float textWidth = _noticeText.GetPreferredValues(content).x;
        _noticeTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);

        // text pivot이 왼쪽 끝이므로 viewport 오른쪽 끝에서 출발해 글자 끝이 viewport 왼쪽 끝을 넘으면 끝난다
        float startX = _viewportRect.rect.width;
        float endX = -textWidth;
        float duration = (startX - endX) / ScrollSpeed;

        _noticeTextRect.anchoredPosition = new Vector2(startX, _noticeTextRect.anchoredPosition.y);
        _scrollTween = _noticeTextRect.DOAnchorPosX(endX, duration)
            .SetEase(Ease.Linear)
            .OnComplete(ScrollNext);
    }
}
