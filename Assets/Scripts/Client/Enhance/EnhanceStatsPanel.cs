using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceStatsPanel : MonoBehaviour
{
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroup;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtAttack;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMaxHp;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtAttackSpeed;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCritRate;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLifesteal;
    [ReadOnly] [SerializeField] private Button btnOutside;

    private Sequence seqTransition;

    public event Action OnClickOutsideEvent = delegate { };

    private void Awake()
    {
        GameUtil.BindOrAdd(gameObject, ref canvasGroup);
        txtAttack = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/Attack/Value");
        txtMaxHp = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/MaxHp/Value");
        txtAttackSpeed = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/AttackSpeed/Value");
        txtCritRate = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/CritRate/Value");
        txtLifesteal = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/Lifesteal/Value");
        btnOutside = GameUtil.Bind<Button>(transform, "OutsideArea");

        btnOutside.onClick.AddListener(OnClickOutside);
    }

    private void OnDestroy()
    {
        if (btnOutside != null)
        {
            btnOutside.onClick.RemoveListener(OnClickOutside);
        }
    }

    public void SetStats(string attack, string maxHp, string attackSpeed, string critRate, string lifesteal)
    {
        txtAttack.text = attack;
        txtMaxHp.text = maxHp;
        txtAttackSpeed.text = attackSpeed;
        txtCritRate.text = critRate;
        txtLifesteal.text = lifesteal;
    }

    // 열 때는 모서리에서 펼쳐지고, 닫을 때는 접힌 뒤에 꺼진다. immediately면 연출 없이 바로 켜고 끈다
    public void SetOpened(bool opened, bool immediately = false)
    {
        KillTransition();

        if (immediately)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;
            gameObject.SetActive(opened);
            return;
        }

        if (opened)
        {
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * GameConstants.PopupAnimation.OpenStartScale;
            seqTransition = CreateTransitionSequence()
                .Append(canvasGroup.DOFade(1f, GameConstants.PopupAnimation.OpenDuration))
                .Join(transform.DOScale(1f, GameConstants.PopupAnimation.OpenDuration).SetEase(GameConstants.PopupAnimation.OpenScaleEase));
            return;
        }

        if (gameObject.activeSelf == false)
        {
            return;
        }

        canvasGroup.blocksRaycasts = false;
        seqTransition = CreateTransitionSequence()
            .Append(canvasGroup.DOFade(0f, GameConstants.PopupAnimation.CloseDuration).SetEase(GameConstants.PopupAnimation.CloseEase))
            .Join(transform.DOScale(GameConstants.PopupAnimation.CloseEndScale, GameConstants.PopupAnimation.CloseDuration).SetEase(GameConstants.PopupAnimation.CloseEase))
            .OnComplete(OnCloseComplete);
    }

    private Sequence CreateTransitionSequence()
    {
        return DOTween.Sequence()
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

    private void OnCloseComplete()
    {
        seqTransition = null;
        gameObject.SetActive(false);
    }

    private void OnClickOutside()
    {
        OnClickOutsideEvent.Invoke();
    }
}
