using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 강화 결과를 연출과 함께 보여 준다. 검이 빛을 모은 뒤 성공이면 섬광이 터지고 실패면 빛이 꺼지며, 마지막에 능력치 변화와 확인 버튼이 나타난다
// 연출을 바꾸려면 아래 값과 Append로 시작하는 메서드만 고치면 된다. 효과 이미지는 프리팹의 Image에서 스프라이트만 바꿔 끼운다
public class EnhancePopup : BasePopup
{
    private const float SwordAppearDuration = 0.3f;  // 검이 나타나는 시간(초)
    private const float SwordStartScale = 0.6f;      // 검이 나타날 때의 시작 크기(원래 크기에 대한 비율)
    private const float ChargeDuration = 1.2f;       // 빛을 모으는 시간(초)
    private const float GlowStartScale = 0.3f;       // 빛을 모으기 시작할 때 빛의 크기
    private const float GlowMaxAlpha = 0.8f;
    private const float RayMaxAlpha = 0.6f;
    private const float RayRotation = 90f;           // 빛을 모으는 동안 빛줄기가 도는 각도
    private const float SwordShakeStrength = 6f;     // 빛을 모으는 동안 검이 떨리는 폭(픽셀)
    private const int SwordShakeVibrato = 30;

    private const float FlashMaxAlpha = 0.8f;        // 성공 섬광의 밝기
    private const float FlashInDuration = 0.08f;
    private const float FlashOutDuration = 0.3f;
    private const float BurstDuration = 0.4f;        // 성공 후 빛이 퍼지며 사라지는 시간(초)
    private const float BurstScale = 1.6f;
    private const float TitleStartScale = 1.4f;      // 성공 문구가 커졌다가 줄어드는 시작 크기
    private const float TitleDuration = 0.3f;
    private const float StreakDuration = 0.2f;       // 성공 문구 뒤의 가로 빛줄기가 펼쳐지는 시간(초)
    private const float StreakFadeDuration = 0.5f;

    private const float FailDuration = 0.5f;         // 실패 후 빛이 꺼지고 검이 어두워지는 시간(초)
    private const float FailGlowScale = 0.5f;

    private const float ResultFadeDuration = 0.3f;   // 능력치 변화와 확인 버튼이 나타나는 시간(초)

    private static readonly Color SuccessTitleColor = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
    private static readonly Color FailTitleColor = new Color32(0xFF, 0x66, 0x66, 0xFF);
    private static readonly Color FailGlowColor = new Color32(0x60, 0x60, 0x60, 0x00);
    private static readonly Color FailSwordColor = new Color32(0x60, 0x60, 0x60, 0xFF);

    public readonly struct StatInfo
    {
        public readonly string name;
        public readonly string before;
        public readonly string after;

        public StatInfo(string name, string before, string after)
        {
            this.name = name;
            this.before = before;
            this.after = after;
        }
    }

    // 값은 화면에 표시할 문자열로 받는다
    public readonly struct ResultInfo
    {
        public readonly bool isSuccess;
        public readonly string levelBefore;
        public readonly string levelAfter;
        public readonly string name;
        public readonly string message; // 레벨 아래에 덧붙이는 문구. 없으면 빈 문자열
        public readonly IReadOnlyList<StatInfo> stats;

        public ResultInfo(bool isSuccess, string levelBefore, string levelAfter, string name, string message,
            IReadOnlyList<StatInfo> stats)
        {
            this.isSuccess = isSuccess;
            this.levelBefore = levelBefore;
            this.levelAfter = levelAfter;
            this.name = name;
            this.message = message;
            this.stats = stats;
        }
    }

    [ReadOnly] [SerializeField] private TextMeshProUGUI txtTitle;
    [ReadOnly] [SerializeField] private Image imgTitleStreak;
    [ReadOnly] [SerializeField] private Image imgGlow;
    [ReadOnly] [SerializeField] private RectTransform rectRays;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroupRays;
    [ReadOnly] [SerializeField] private Image imgSword;
    [ReadOnly] [SerializeField] private Image imgFlash;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroupResult;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLevel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMessage;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private RectTransform rectStatList;
    [ReadOnly] [SerializeField] private List<RectTransform> listStatRow = new List<RectTransform>();
    [ReadOnly] [SerializeField] private Button btnConfirm;

    private ResultInfo result;
    private Action onConfirm;

    private Color colorGlow;
    private Color colorSword;
    private Vector2 vecSwordPosition;
    private Sequence seqEnhance;

    protected override void Awake()
    {
        base.Awake();

        txtTitle = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/TitleArea/Title");
        imgTitleStreak = GameUtil.Bind<Image>(transform, "Panel/TitleArea/Streak");
        imgGlow = GameUtil.Bind<Image>(transform, "Panel/SwordArea/Glow");
        rectRays = GameUtil.Bind<RectTransform>(transform, "Panel/SwordArea/Rays");
        canvasGroupRays = GameUtil.Bind<CanvasGroup>(transform, "Panel/SwordArea/Rays");
        imgSword = GameUtil.Bind<Image>(transform, "Panel/SwordArea/Sword");
        imgFlash = GameUtil.Bind<Image>(transform, "Flash");
        canvasGroupResult = GameUtil.Bind<CanvasGroup>(transform, "Panel/Result");
        txtLevel = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Result/Level");
        txtMessage = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Result/Message");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Result/Name");
        rectStatList = GameUtil.Bind<RectTransform>(transform, "Panel/Result/StatList");
        listStatRow.Add(GameUtil.Bind<RectTransform>(rectStatList, "StatRow"));
        btnConfirm = GameUtil.Bind<Button>(transform, "Panel/Result/Footer/ConfirmButton");

        colorGlow = imgGlow.color;
        colorSword = imgSword.color;
        vecSwordPosition = imgSword.rectTransform.anchoredPosition;

        btnConfirm.onClick.AddListener(OnClickConfirm);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        KillEnhanceSequence();
    }

    private void OnDestroy()
    {
        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveListener(OnClickConfirm);
        }
    }

    // 확인을 누르면 onConfirm만 부른다. 팝업을 닫는 일은 여는 쪽이 맡는다
    public void Setup(ResultInfo result, Action onConfirm)
    {
        this.result = result;
        this.onConfirm = onConfirm;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        txtTitle.text = result.isSuccess ? "강화 성공" : "강화 실패";
        txtTitle.color = result.isSuccess ? SuccessTitleColor : FailTitleColor;
        txtLevel.text = $"{result.levelBefore} → {result.levelAfter}";
        txtMessage.text = result.message;
        txtMessage.gameObject.SetActive(string.IsNullOrEmpty(result.message) == false);
        txtName.text = result.name;
        ShowStats(result.stats);

        PlayEnhanceSequence();
    }

    // 연출 중에 화면을 누르면 연출을 건너뛰고 결과를 바로 보여 준다. 결과가 나온 뒤에는 확인 버튼으로만 닫힌다
    protected override void OnClickDimmed()
    {
        if (seqEnhance != null)
        {
            seqEnhance.Complete();
            return;
        }

        base.OnClickDimmed();
    }

    private void ShowStats(IReadOnlyList<StatInfo> stats)
    {
        while (listStatRow.Count < stats.Count)
        {
            listStatRow.Add(Instantiate(listStatRow[0], rectStatList));
        }

        for (int i = 0; i < listStatRow.Count; i++)
        {
            bool visible = i < stats.Count;
            listStatRow[i].gameObject.SetActive(visible);
            if (visible)
            {
                SetStatRow(listStatRow[i], stats[i]);
            }
        }
    }

    private void SetStatRow(RectTransform row, StatInfo stat)
    {
        GameUtil.Bind<TextMeshProUGUI>(row, "Label").text = stat.name;
        GameUtil.Bind<TextMeshProUGUI>(row, "Before").text = stat.before;
        GameUtil.Bind<TextMeshProUGUI>(row, "After").text = stat.after;
    }

    private void PlayEnhanceSequence()
    {
        KillEnhanceSequence();
        ResetEffect();

        seqEnhance = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);
        seqEnhance.Append(imgSword.rectTransform.DOScale(1f, SwordAppearDuration).SetEase(Ease.OutBack));
        AppendCharge(seqEnhance);

        if (result.isSuccess)
        {
            AppendSuccess(seqEnhance);
        }
        else
        {
            AppendFail(seqEnhance);
        }

        seqEnhance.Append(canvasGroupResult.DOFade(1f, ResultFadeDuration));
        seqEnhance.OnComplete(OnEnhanceSequenceComplete);
    }

    // 연출이 바꿔 놓은 값을 처음 상태로 되돌린다. 다시 열 때도 같은 모습에서 시작한다
    private void ResetEffect()
    {
        imgSword.color = colorSword;
        imgSword.rectTransform.anchoredPosition = vecSwordPosition;
        imgSword.rectTransform.localScale = Vector3.one * SwordStartScale;

        imgGlow.color = new Color(colorGlow.r, colorGlow.g, colorGlow.b, 0f);
        imgGlow.rectTransform.localScale = Vector3.one * GlowStartScale;

        rectRays.localRotation = Quaternion.identity;
        rectRays.localScale = Vector3.one;
        canvasGroupRays.alpha = 0f;

        imgFlash.color = new Color(imgFlash.color.r, imgFlash.color.g, imgFlash.color.b, 0f);

        txtTitle.alpha = 0f;
        txtTitle.rectTransform.localScale = Vector3.one * (result.isSuccess ? TitleStartScale : 1f);
        imgTitleStreak.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        imgTitleStreak.color = new Color(imgTitleStreak.color.r, imgTitleStreak.color.g, imgTitleStreak.color.b, 1f);

        canvasGroupResult.alpha = 0f;
        canvasGroupResult.interactable = false;
        canvasGroupResult.blocksRaycasts = false;
    }

    private void AppendCharge(Sequence sequence)
    {
        float start = sequence.Duration();
        sequence.Insert(start, imgGlow.rectTransform.DOScale(1f, ChargeDuration).SetEase(Ease.InQuad));
        sequence.Insert(start, imgGlow.DOFade(GlowMaxAlpha, ChargeDuration));
        sequence.Insert(start, canvasGroupRays.DOFade(RayMaxAlpha, ChargeDuration * 0.5f));
        sequence.Insert(start, rectRays.DOLocalRotate(new Vector3(0f, 0f, -RayRotation), ChargeDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));
        sequence.Insert(start, imgSword.rectTransform.DOShakeAnchorPos(ChargeDuration, SwordShakeStrength, SwordShakeVibrato, 90f, false, false));
    }

    private void AppendSuccess(Sequence sequence)
    {
        float start = sequence.Duration();
        sequence.Insert(start, imgSword.rectTransform.DOAnchorPos(vecSwordPosition, FlashInDuration));
        sequence.Insert(start, imgFlash.DOFade(FlashMaxAlpha, FlashInDuration));
        sequence.Insert(start + FlashInDuration, imgFlash.DOFade(0f, FlashOutDuration));

        float burstStart = start + FlashInDuration;
        sequence.Insert(burstStart, imgGlow.rectTransform.DOScale(BurstScale, BurstDuration));
        sequence.Insert(burstStart, imgGlow.DOFade(0f, BurstDuration));
        sequence.Insert(burstStart, rectRays.DOScale(BurstScale, BurstDuration));
        sequence.Insert(burstStart, canvasGroupRays.DOFade(0f, BurstDuration));
        sequence.Insert(burstStart, txtTitle.DOFade(1f, TitleDuration));
        sequence.Insert(burstStart, txtTitle.rectTransform.DOScale(1f, TitleDuration).SetEase(Ease.OutBack));
        sequence.Insert(burstStart, imgTitleStreak.rectTransform.DOScaleX(1f, StreakDuration).SetEase(Ease.OutQuad));
        sequence.Insert(burstStart, imgTitleStreak.DOFade(0f, StreakFadeDuration).SetEase(Ease.InQuad));
    }

    private void AppendFail(Sequence sequence)
    {
        float start = sequence.Duration();
        sequence.Insert(start, imgSword.rectTransform.DOAnchorPos(vecSwordPosition, FlashInDuration));
        sequence.Insert(start, imgGlow.DOColor(FailGlowColor, FailDuration));
        sequence.Insert(start, imgGlow.rectTransform.DOScale(FailGlowScale, FailDuration));
        sequence.Insert(start, canvasGroupRays.DOFade(0f, FailDuration * 0.6f));
        sequence.Insert(start, imgSword.DOColor(FailSwordColor, FailDuration));
        sequence.Insert(start, txtTitle.DOFade(1f, FailDuration));
    }

    private void KillEnhanceSequence()
    {
        if (seqEnhance != null)
        {
            seqEnhance.Kill();
            seqEnhance = null;
        }
    }

    private void OnEnhanceSequenceComplete()
    {
        seqEnhance = null;
        canvasGroupResult.interactable = true;
        canvasGroupResult.blocksRaycasts = true;
    }

    private void OnClickConfirm()
    {
        onConfirm?.Invoke();
    }
}
