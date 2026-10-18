using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 같은 오브젝트의 Button 클릭을 OnClickEvent로 전달하고, 누르고 있는 동안 크기를 줄여 눌리는 느낌을 준다
[RequireComponent(typeof(Button))]
public class ButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [ReadOnly] [SerializeField] private Button btnTarget;

    [ReadOnly(true)] [SerializeField] private float pressedScale = 0.95f;
    [ReadOnly(true)] [SerializeField] private float pressDuration = 0.06f;
    [ReadOnly(true)] [SerializeField] private float releaseDuration = 0.1f;

    private Vector3 vecOriginalScale;
    private Tween tweenScale;

    public bool Interactable
    {
        get => btnTarget.interactable;
        set => btnTarget.interactable = value;
    }

    public event Action OnClickEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnTarget);
        vecOriginalScale = transform.localScale;

        btnTarget.onClick.AddListener(OnClick);
    }

    // 누른 채로 꺼져도 다음에 켜질 때 작은 크기로 남지 않게 한다
    private void OnDisable()
    {
        tweenScale?.Kill();
        transform.localScale = vecOriginalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left
            || btnTarget.IsInteractable() == false)
        {
            return;
        }

        AnimateScale(vecOriginalScale * pressedScale, pressDuration);
    }

    // 목록을 스크롤하기 시작할 때도 EventSystem이 OnPointerUp을 보내므로 원래 크기로 돌아온다
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        AnimateScale(vecOriginalScale, releaseDuration);
    }

    private void AnimateScale(Vector3 scale, float duration)
    {
        tweenScale?.Kill();
        tweenScale = transform.DOScale(scale, duration)
            .SetEase(Ease.OutQuad);
    }

    private void OnClick()
    {
        OnClickEvent.Invoke();
    }
}
