using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 화면의 스테이지, 남은 시간, HP바, 이번 전투에서 모은 골드, 시작 카운트다운을 표시한다
public class BattleHudView : MonoBehaviour
{
    // 카운트다운 숫자 한 칸(1초) 안의 연출. 남은 시간에서 바로 계산하므로 일시 정지하면 함께 멈춘다
    private const float CountdownInScale = 2f; // 이 크기에서 1배로 내려앉으며 나타난다
    private const float CountdownInDuration = 0.25f;
    private const Ease CountdownInEase = Ease.OutBack;
    private const float CountdownInOvershoot = 1.7f;
    private const float CountdownOutStart = 0.75f; // 한 칸 안에서 사라지기 시작하는 시점
    private const float CountdownOutScale = 1.4f; // 이 크기로 커지며 사라진다
    private const Ease CountdownOutEase = Ease.InQuad;

    // 카운트다운 동안 화면 전체를 어둡게 덮는다
    private const float CountdownDimAlpha = 0.45f;
    private const float CountdownDimFadeDuration = 0.2f;

    // 남은 시간 경고: 경고 시간부터 숫자를 경고색으로 바꾸고, 정수 초가 바뀔 때마다 숫자가 맥박치고 모래시계가 흔들린다
    // 남은 시간(게임 시간)에서 바로 계산하므로 배속에 맞춰 빨라지고, 일시 정지하면 함께 멈춘다
    public const double TimeWarningSeconds = 5d;
    private static readonly Color TimeWarningColor = new Color32(0xFF, 0x5A, 0x5A, 0xFF);
    private const float TimePulseScale = 1.25f;
    private const double TimePulseFinalSeconds = 3d; // 이 시간부터는 커지는 양을 TimePulseFinalBoost배로 늘린다
    private const float TimePulseFinalBoost = 1.2f;
    private const float TimePulseUpDuration = 0.08f;
    private const float TimePulseDownDuration = 0.25f;
    private const Ease TimePulseDownEase = Ease.OutCubic;
    private const float TimerIconShakeAngle = 15f;
    private const float TimerIconShakeDuration = 0.3f;

    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStageName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtSubStage;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtTime;
    [ReadOnly] [SerializeField] private RectTransform rectTimerIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCountdown;
    [ReadOnly] [SerializeField] private Image imgCountdownDim;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGold;
    [ReadOnly] [SerializeField] private RectTransform rectGoldIcon;
    [ReadOnly] [SerializeField] private HpBarView objPlayerHpBar;
    [ReadOnly] [SerializeField] private HpBarView objEnemyHpBar;

    public HpBarView PlayerHpBar => objPlayerHpBar;

    public HpBarView EnemyHpBar => objEnemyHpBar;

    // 처치 골드 동전이 빨려 들어갈 자리. 골드 숫자 앞의 아이콘 가운데라 자릿수가 바뀌어도 움직이지 않는다
    public Vector3 GoldIconPosition => rectGoldIcon.position;

    private bool isCountdownShown;
    private Color colorTime; // 경고 전 시간 숫자 색

    private void Awake()
    {
        txtStageName = GameUtil.Bind<TextMeshProUGUI>(transform, "TopPanel/StageNameText");
        txtSubStage = GameUtil.Bind<TextMeshProUGUI>(transform, "TopPanel/SubStageText");
        txtTime = GameUtil.Bind<TextMeshProUGUI>(transform, "Timer/TimeText");
        rectTimerIcon = GameUtil.Bind<RectTransform>(transform, "Timer/TimerIcon");
        colorTime = txtTime.color;
        txtCountdown = GameUtil.Bind<TextMeshProUGUI>(transform, "CountdownText");
        imgCountdownDim = GameUtil.Bind<Image>(transform, "CountdownDim");
        txtGold = GameUtil.Bind<TextMeshProUGUI>(transform, "GoldText");
        rectGoldIcon = GameUtil.Bind<RectTransform>(transform, "GoldIcon");
        objPlayerHpBar = GameUtil.Bind<HpBarView>(transform, "PlayerHpBar");
        objEnemyHpBar = GameUtil.Bind<HpBarView>(transform, "EnemyHpBar");
    }

    /// <summary>
    /// 스테이지 이름과 세부 스테이지 번호를 표시한다. subStageNumber는 1부터 센다.
    /// </summary>
    public void SetStage(string stageName, int stage, int subStageNumber, bool isBoss)
    {
        txtStageName.text = stageName;

        string subStage = $"{stage.ToString()}-{subStageNumber.ToString()}";
        txtSubStage.text = isBoss
            ? $"{subStage} BOSS"
            : subStage;
    }

    private void OnDestroy()
    {
        imgCountdownDim.DOKill();
    }

    /// <summary>
    /// 전투 시작 전에 화면을 어둡게 덮고, 가운데에 남은 초를 크게 띄운다. 소수는 올려서 3, 2, 1로 센다.
    /// 매 프레임 불러서 숫자마다 커졌다 내려앉고, 커지며 사라지게 한다.
    /// </summary>
    public void SetStartCountdown(double seconds)
    {
        if (isCountdownShown == false)
        {
            isCountdownShown = true;
            ShowCountdownDim();
        }

        double number = Math.Ceiling(seconds);
        float time = (float)(number - seconds); // 이 숫자가 뜬 뒤 흐른 시간(0~1초)

        float scale = 1f;
        float alpha = 1f;
        if (time < CountdownInDuration)
        {
            float progress = time / CountdownInDuration;
            scale = DOVirtual.EasedValue(CountdownInScale, 1f, progress, CountdownInEase, CountdownInOvershoot);
            alpha = DOVirtual.EasedValue(0f, 1f, progress, Ease.OutQuad);
        }

        if (time >= CountdownOutStart)
        {
            float progress = DOVirtual.EasedValue(0f, 1f, (time - CountdownOutStart) / (1f - CountdownOutStart), CountdownOutEase);
            scale = Mathf.Lerp(1f, CountdownOutScale, progress);
            alpha *= 1f - progress;
        }

        txtCountdown.gameObject.SetActive(true);
        txtCountdown.text = number.ToString("0");
        txtCountdown.rectTransform.localScale = Vector3.one * scale;
        txtCountdown.alpha = alpha;
    }

    public void HideStartCountdown()
    {
        isCountdownShown = false;
        txtCountdown.gameObject.SetActive(false);

        imgCountdownDim.DOKill();
        imgCountdownDim.DOFade(0f, CountdownDimFadeDuration)
            .OnComplete(() => imgCountdownDim.gameObject.SetActive(false));
    }

    private void ShowCountdownDim()
    {
        imgCountdownDim.DOKill();
        imgCountdownDim.gameObject.SetActive(true);
        imgCountdownDim.color = new Color(imgCountdownDim.color.r, imgCountdownDim.color.g, imgCountdownDim.color.b, 0f);
        imgCountdownDim.DOFade(CountdownDimAlpha, CountdownDimFadeDuration);
    }

    /// <summary>
    /// 남은 시간을 표시한다. 매 프레임 불러서 TimeWarningSeconds 이하면 경고 연출을 함께 계산한다.
    /// </summary>
    public void SetRemainingTime(double seconds)
    {
        txtTime.text = $"{Math.Max(0d, seconds).ToString("0.0")}s";

        bool isWarning = seconds <= TimeWarningSeconds;
        txtTime.color = isWarning ? TimeWarningColor : colorTime;

        float scale = 1f;
        float angle = 0f;
        if (isWarning && seconds > 0d)
        {
            double second = Math.Ceiling(seconds);
            float time = (float)(second - seconds); // 이 정수 초에 들어온 뒤 흐른 시간(0~1초)

            float boost = second <= TimePulseFinalSeconds ? TimePulseFinalBoost : 1f;
            float peak = 1f + (TimePulseScale - 1f) * boost;
            scale = time < TimePulseUpDuration
                ? DOVirtual.EasedValue(1f, peak, time / TimePulseUpDuration, Ease.OutQuad)
                : DOVirtual.EasedValue(peak, 1f, Mathf.Clamp01((time - TimePulseUpDuration) / TimePulseDownDuration), TimePulseDownEase);

            float shake = Mathf.Clamp01(time / TimerIconShakeDuration);
            angle = Mathf.Sin(shake * Mathf.PI * 3f) * TimerIconShakeAngle * (1f - shake);
        }

        txtTime.rectTransform.localScale = Vector3.one * scale;
        rectTimerIcon.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    public void SetGold(long gold)
    {
        txtGold.text = gold.ToString(GameConstants.NumberFormats.Integer);
    }

    /// <summary>
    /// 처치 동전이 골드 표시에 닿을 때 아이콘과 숫자를 한 번 튀게 한다. 연달아 닿으면 처음부터 다시 튄다. speed는 전투 배속이다.
    /// </summary>
    public void PlayGoldArrived(float speed)
    {
        rectGoldIcon.DOKill();
        Tween tween = DOVirtual.Float(0f, 1f, GameConstants.CurrencyArrive.PunchDuration, progress =>
            {
                float wave = Mathf.Sin(progress * Mathf.PI);
                rectGoldIcon.localScale = Vector3.one * (1f + (GameConstants.CurrencyArrive.IconPunchScale - 1f) * wave);
                txtGold.rectTransform.localScale = Vector3.one * (1f + (GameConstants.CurrencyArrive.TextPunchScale - 1f) * wave);
            })
            .SetEase(Ease.Linear)
            .SetTarget(rectGoldIcon)
            .SetLink(gameObject);
        tween.timeScale = speed;
    }
}
