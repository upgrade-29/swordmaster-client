using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 체력바 하나. 맞으면 채움이 바로 줄고 노란 잔상이 늦게 따라 줄어들며, 채움이 깜빡이고 바가 흔들린다
// 아이콘은 Bar 밖에 두어 흔들리지 않는다
public class HpBarView : MonoBehaviour
{
    // 채움
    private const float FillDuration = 0.08f;
    private const float HealDuration = 0.3f;

    // 잔상
    private const float TrailDelay = 0.3f;
    private const float TrailDuration = 0.4f;

    // 깜빡임: 채움 색을 흰색 쪽으로 잠깐 섞는다
    private const float FlashDuration = 0.15f;
    private const float FlashPeak = 0.8f;

    // 흔들림
    private const float ShakeDuration = 0.2f;
    private const float ShakeStrength = 8f;
    private const int ShakeVibrato = 20;
    private const float ShakeRandomness = 90f;
    private const float StrongHitShakeScale = 1.5f; // 최대 체력의 StrongHitDamageRatio 이상을 잃었을 때의 흔들림 배율

    [ReadOnly] [SerializeField] private RectTransform rectBar;
    [ReadOnly] [SerializeField] private Image imgFill; // Image Type = Filled
    [ReadOnly] [SerializeField] private Image imgTrail; // Image Type = Filled
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtHp;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroup; // 아이콘까지 블록 전체를 숨긴다

    private Color fillColor;
    private Vector2 vecBarPosition;
    private double currentHp;

    private void Awake()
    {
        rectBar = GameUtil.Bind<RectTransform>(transform, "Bar");
        imgFill = GameUtil.Bind<Image>(transform, "Bar/Fill");
        imgTrail = GameUtil.Bind<Image>(transform, "Bar/Trail");
        txtHp = GameUtil.Bind<TextMeshProUGUI>(transform, "Bar/HpText");
        GameUtil.Bind(gameObject, ref canvasGroup);

        fillColor = imgFill.color;
        vecBarPosition = rectBar.anchoredPosition;
    }

    private void OnDestroy()
    {
        canvasGroup.DOKill();
    }

    /// <summary>
    /// 체력바를 duration(배속 반영) 동안 흐리게 숨기거나 보여 준다. 0이면 바로 바뀐다.
    /// </summary>
    public void SetVisible(bool visible, float duration)
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(visible ? 1f : 0f, duration);
    }

    /// <summary>
    /// 연출 없이 체력을 바로 맞춘다. 전투 시작, 재도전, 새 적 등장처럼 처음 값을 정할 때 쓴다.
    /// </summary>
    public void SetHp(double current, double max)
    {
        StopEffects();
        currentHp = current;

        float ratio = GetRatio(current, max);
        imgFill.fillAmount = ratio;
        imgTrail.fillAmount = ratio;
        SetHpText(current, max);
    }

    /// <summary>
    /// 맞거나 회복한 체력을 연출과 함께 보여 준다. 잃은 체력이 최대 체력에 비해 클수록 바가 세게 흔들린다.
    /// </summary>
    public void ChangeHp(double current, double max)
    {
        double lostHp = currentHp - current;
        currentHp = current;
        SetHpText(current, max);

        float ratio = GetRatio(current, max);
        if (lostHp > 0)
        {
            PlayDamage(ratio, GetRatio(lostHp, max));
        }
        else if (lostHp < 0)
        {
            PlayHeal(ratio);
        }
    }

    // 연타로 맞으면 잔상의 기다림을 처음부터 다시 시작해서, 연타가 끝난 뒤 한 번에 줄어들게 한다
    private void PlayDamage(float ratio, float lostRatio)
    {
        imgFill.DOKill();
        imgTrail.DOKill();
        imgFill.color = fillColor;

        imgFill.DOFillAmount(ratio, FillDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        imgTrail.DOFillAmount(ratio, TrailDuration).SetDelay(TrailDelay).SetEase(Ease.InOutQuad).SetLink(gameObject);
        DOVirtual.Float(0f, 1f, FlashDuration, progress => imgFill.color = Color.Lerp(fillColor, Color.white, GetFlashAmount(progress)))
            .SetEase(Ease.Linear)
            .SetTarget(imgFill)
            .SetLink(gameObject);

        float hitStrength = Mathf.Clamp01(lostRatio / GameConstants.BattleEffect.StrongHitDamageRatio);
        float shakeStrength = ShakeStrength * Mathf.Lerp(1f, StrongHitShakeScale, hitStrength);
        rectBar.DOKill();
        rectBar.anchoredPosition = vecBarPosition;
        rectBar.DOShakeAnchorPos(ShakeDuration, shakeStrength, ShakeVibrato, ShakeRandomness).SetLink(gameObject);
    }

    // 회복은 잔상 없이 채움과 잔상이 함께 차오른다
    private void PlayHeal(float ratio)
    {
        imgFill.DOKill();
        imgTrail.DOKill();
        imgFill.color = fillColor;

        imgFill.DOFillAmount(ratio, HealDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        imgTrail.DOFillAmount(ratio, HealDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
    }

    private void StopEffects()
    {
        imgFill.DOKill();
        imgTrail.DOKill();
        rectBar.DOKill();
        imgFill.color = fillColor;
        rectBar.anchoredPosition = vecBarPosition;
    }

    // 맞은 순간 가장 밝았다가 원래 색으로 돌아온다
    private static float GetFlashAmount(float progress)
    {
        return (0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * progress)) * (1f - progress) * FlashPeak;
    }

    private static float GetRatio(double value, double max)
    {
        return max > 0 ? (float)(value / max) : 0f;
    }

    // 소수 첫째 자리까지 보여주고 그 아래는 올린다. 살아 있는데 0으로 보이지 않게 하기 위해서다
    private void SetHpText(double current, double max)
    {
        txtHp.text = $"{CeilToTenth(current):#,0.#} / {CeilToTenth(max):#,0.#}";
    }

    // 87.54 -> 87.6. 87.5처럼 이미 딱 떨어지는 값이 부동소수 오차로 87.6이 되지 않게 조금 깎고 올린다
    private static double CeilToTenth(double value)
    {
        return System.Math.Ceiling(value * 10 - 1e-6) / 10;
    }
}
