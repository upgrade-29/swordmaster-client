using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 모든 팝업의 기반 클래스. Dimmed가 뒤쪽 입력을 막고, 설정에 따라 Dimmed를 누르면 닫힌다
// 열고 닫을 때 공통 연출을 재생한다. 닫힘 연출이 재생되는 동안 IsOpened는 이미 false이고, 입력을 받지 않는다
// 연출을 바꾸려면 GameConstants.PopupAnimation 값과 CreateOpenSequence, CreateCloseSequence의 Tween 구성만 고치면 모든 팝업에 적용된다
public class BasePopup : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Button btnDimmed;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroup;
    [ReadOnly] [SerializeField] private RectTransform rectPanel;

    [ReadOnly(true)] [SerializeField] private bool closeOnOutsideTouch = true; // 팝업 밖을 누르면 닫힘

    private bool isOpened;
    public bool IsOpened => isOpened;

    private Sequence seqTransition;

    public event Action<BasePopup> OnOpenEvent = delegate { };
    public event Action<BasePopup> OnCloseEvent = delegate { };

    protected virtual void Awake()
    {
        btnDimmed = GameUtil.Bind<Button>(transform, "Dimmed");
        GameUtil.BindOrAdd(gameObject, ref canvasGroup);
        rectPanel = GameUtil.TryBind<RectTransform>(transform, "Panel");

        btnDimmed.onClick.AddListener(OnClickDimmed);

        // Awake는 켜질 때 실행되므로, 씬에서 켜진 채로 시작한 팝업도 열린 것으로 본다
        isOpened = true;
    }

    // 씬이 닫히거나 부모가 꺼져 연출 도중에 꺼지면 연출만 멈춘다
    protected virtual void OnDisable()
    {
        KillTransition();
    }

    // 내용이 있는 팝업은 Setup으로 내용을 받은 뒤에 연다. 이미 열려 있으면 켜지 않고 새 내용만 다시 반영한다
    // 닫히는 중에 다시 열면 닫힘 연출을 멈추고 열림 연출을 처음부터 재생한다
    public void Open()
    {
        if (isOpened == false)
        {
            isOpened = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            OnOpenEvent.Invoke(this);
            PlayOpenAnimation();
        }

        OnOpen();
    }

    // 닫힘 연출이 끝난 뒤에 끈다. IsOpened와 OnCloseEvent는 연출을 기다리지 않고 바로 바뀐다
    public void Close()
    {
        if (isOpened == false)
        {
            return;
        }

        isOpened = false;
        canvasGroup.blocksRaycasts = false;
        OnCloseEvent.Invoke(this);

        KillTransition();
        seqTransition = CreateCloseSequence()
            .OnComplete(OnCloseAnimationComplete);
    }

    // 켜진 뒤에 호출되므로 처음 열 때도 Awake의 바인딩이 끝나 있다. Setup으로 받아 둔 내용을 여기서 화면에 반영한다
    protected virtual void OnOpen()
    {
    }

    // 열려 있는 팝업이 새 내용을 받아 열림 연출을 다시 보여 줘야 할 때도 부른다
    protected void PlayOpenAnimation()
    {
        KillTransition();
        seqTransition = CreateOpenSequence();
    }

    // 바깥 터치로 닫힐 때 따로 할 일이 있는 팝업은 재정의한다
    protected virtual void OnClickDimmed()
    {
        if (closeOnOutsideTouch == false)
        {
            return;
        }

        Close();
    }

    // 투명하고 작은 상태에서 시작해 원래 모습으로 나타난다. Panel이 없는 팝업은 페이드만 한다
    private Sequence CreateOpenSequence()
    {
        canvasGroup.alpha = 0f;

        Sequence sequence = CreateTransitionSequence();
        sequence.Append(canvasGroup.DOFade(1f, GameConstants.PopupAnimation.OpenDuration));

        if (rectPanel != null)
        {
            rectPanel.localScale = Vector3.one * GameConstants.PopupAnimation.OpenStartScale;
            sequence.Join(rectPanel.DOScale(1f, GameConstants.PopupAnimation.OpenDuration).SetEase(GameConstants.PopupAnimation.OpenScaleEase));
        }

        return sequence;
    }

    // 지금 보이는 상태에서 시작하므로, 열리는 도중에 닫아도 끊기지 않고 이어서 사라진다
    private Sequence CreateCloseSequence()
    {
        Sequence sequence = CreateTransitionSequence();
        sequence.Append(canvasGroup.DOFade(0f, GameConstants.PopupAnimation.CloseDuration).SetEase(GameConstants.PopupAnimation.CloseEase));

        if (rectPanel != null)
        {
            sequence.Join(rectPanel.DOScale(GameConstants.PopupAnimation.CloseEndScale, GameConstants.PopupAnimation.CloseDuration).SetEase(GameConstants.PopupAnimation.CloseEase));
        }

        return sequence;
    }

    // 전투는 일시정지(timeScale 0) 중에도 팝업을 띄우므로 시간 배율과 관계없이 재생하고, 팝업이 파괴되면 함께 정리되게 묶는다
    private Sequence CreateTransitionSequence()
    {
        return DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void KillTransition()
    {
        if (seqTransition != null)
        {
            seqTransition.Kill();
            seqTransition = null;
        }
    }

    private void OnCloseAnimationComplete()
    {
        seqTransition = null;
        gameObject.SetActive(false);
    }
}
