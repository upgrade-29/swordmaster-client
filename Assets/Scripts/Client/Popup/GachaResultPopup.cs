using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 가챠 결과를 상자 열기 연출로 보여 준다. 상자를 누르면 떨리면서 틈으로 빛이 새어 나오고, 화면이 순간 하얘지면서 뚜껑이 들리고, 들리는 중에 글로우가 터지고,
// 다 열리면 광선이 하나씩 돌면서 뻗어 나온 뒤 멈춘다
// 빛 색은 뽑은 유물의 최고 등급으로 정한다. 처음에는 패널 없이 어두운 배경 위에 상자만 보인다
// 상자가 열리고 iconDelay초가 지나면 상자를 치우고, 결과 패널을 띄운 뒤 왼쪽 위부터 보상을 채우고 닫기 버튼을 켠다
// 그리는 순서는 새는 빛 > 상자(애니메이션) > 틈 빛 > 광선 > 글로우 > 상자 앞부분이라서 빛이 상자 안에서 나오는 것처럼 보인다
// 앞부분은 두 장이다. 열리는 중에는 뚜껑과 겹치지 않는 몸통만(FrontOpening), 다 열린 뒤에는 앞 테두리까지(Front) 덮는다
// 연출 중에 누르면 건너뛴다. 상자가 열리는 중이면 다 열리고 빛이 자리 잡은 모습으로, 결과 패널이 뜬 뒤면 보상이 다 채워진 모습으로 바로 간다
// 상자가 열린 채 보이는 동안 누르면 기다리지 않고 바로 상자를 치우지만, 결과 패널 연출은 그대로 보여 준다
// 상자를 건너뛰려고 연타한 클릭이 결과 연출까지 건너뛰지 않게, 상자가 사라지는 중의 클릭은 무시한다
public class GachaResultPopup : BasePopup, IPointerClickHandler
{
    private enum EffectPhase
    {
        Ready,     // 상자를 누르기 전
        Opening,   // 떨림부터 광선이 나오기 시작할 때까지
        Opened,    // 상자가 열린 채 보이는 동안(iconDelay). 누르면 기다리지 않고 바로 치운다
        Hiding,    // 상자가 사라지는 중. 누르는 것을 무시한다
        Revealing, // 결과 패널이 뜨고 보상이 다 박힐 때까지
        Finished,
    }

    private static readonly int AnimOpenTrigger = Animator.StringToHash("Open");
    private static readonly int AnimOpenState = Animator.StringToHash("Open");

    [ReadOnly] [SerializeField] private RectTransform rectChestArea;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroupChestArea;
    [ReadOnly] [SerializeField] private RectTransform rectChest;
    [ReadOnly] [SerializeField] private ButtonUI btnChest;
    [ReadOnly] [SerializeField] private Animator animatorChest;
    [ReadOnly] [SerializeField] private Image imgLeakGlow;
    [ReadOnly] [SerializeField] private Image imgSeamLight;
    [ReadOnly] [SerializeField] private Image imgChestFront;
    [ReadOnly] [SerializeField] private Image imgChestFrontOpening;
    [ReadOnly] [SerializeField] private Image imgGlow;
    [ReadOnly] [SerializeField] private RectTransform rectRays;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroupRays;
    [ReadOnly] [SerializeField] private List<Image> listRay = new List<Image>();
    [ReadOnly] [SerializeField] private List<float> listRayAngle = new List<float>(); // 이번에 쓰는 광선들이 연출 끝에 멈출 각도. 등급에 따라 정한다
    [ReadOnly] [SerializeField] private RectTransform rectResultPanel;
    [ReadOnly] [SerializeField] private RectTransform rectRewardGrid;
    [ReadOnly] [SerializeField] private GameObject objSlotTemplate;
    [ReadOnly] [SerializeField] private ButtonUI btnClose;
    [ReadOnly] [SerializeField] private Image imgFlash;

    [Header("떨림")]
    [SerializeField] private float shakeDuration = 1f;     // 상자를 누른 뒤 떨리는 시간(초)
    [SerializeField] private float shakeStrength = 12f;    // 좌우·위아래로 떨리는 폭(유닛)
    [SerializeField] private float shakeAngle = 6f;        // 기울어지며 떨리는 각도(도)
    [SerializeField] private int shakeVibrato = 30;        // 떨리는 동안 흔들리는 횟수

    [Header("새어 나오는 빛")]
    [SerializeField] [Range(0f, 1f)] private float leakGlowAlpha = 0.7f; // 떨림이 끝날 때 상자 뒤 빛의 밝기
    [SerializeField] private float leakGlowStartScale = 0.6f;            // 상자 뒤 빛이 커지기 시작하는 크기
    [SerializeField] [Range(0f, 1f)] private float seamAlpha = 1f;       // 뚜껑 틈 빛의 밝기
    [SerializeField] private float seamFlickerScale = 2f;                // 틈 빛이 깜빡일 때 두꺼워지는 배율
    [SerializeField] private float seamFlickerDuration = 0.07f;          // 틈 빛이 한 번 깜빡이는 시간(초)

    [Header("섬광 (열리기 직전에 화면이 하얘진다)")]
    [SerializeField] [Range(0f, 1f)] private float flashAlpha = 0.8f; // 섬광이 가장 밝을 때의 밝기
    [SerializeField] private float flashInDuration = 0.2f;           // 하얘지기까지의 시간(초). 가장 밝을 때 뚜껑이 들리기 시작한다
    [SerializeField] private float flashOutDuration = 0.3f;          // 다시 걷히는 시간(초)

    [Header("글로우 (열리는 중에 터진다)")]
    [SerializeField] [Range(0f, 1f)] private float glowStartTime = 0.3f; // 열림 애니메이션의 이 지점(0~1)에서 터진다
    [SerializeField] [Range(0f, 1f)] private float glowAlpha = 0.8f;
    [SerializeField] private float glowDuration = 0.5f;    // 글로우가 터졌다가 자리 잡는 시간(초)
    [SerializeField] private float glowStartScale = 0.3f;  // 글로우가 나오기 시작할 때의 크기(원래 크기에 대한 비율)
    [SerializeField] private float glowPunchScale = 1.4f;  // 글로우가 순간 커졌다 돌아오는 크기
    [SerializeField] private float stagePunch = 0.06f;     // 글로우가 터질 때 상자와 빛이 함께 튀는 크기

    [Header("광선 (다 열린 뒤 하나씩 나온다)")]
    [SerializeField] [Range(0f, 1f)] private float rayAlpha = 0.8f;
    [SerializeField] private float rayDuration = 0.5f;     // 광선 하나가 돌아 들어와 제자리에 멈추기까지의 시간(초)
    [SerializeField] private float rayStartLength = 320f;  // 처음 튀어나올 때의 길이(유닛)
    [SerializeField] private float rayPopDuration = 0.08f; // 입구에서 짧게 튀어나오는 시간(초)
    [SerializeField] private float rayGrowDuration = 0.15f; // 튀어나온 뒤 화면 위 끝까지 길어지는 시간(초). 회전이 끝나기 전에 다 길어지게 짧게 둔다
    [SerializeField] private float rayInterval = 0.12f;    // 다음 광선이 나오기까지의 간격(초)
    [SerializeField] private float rayRotation = 50f;      // 광선이 제자리까지 돌아 들어오는 각도(도)
    [SerializeField] private float raySpacing = 32f;       // 광선 사이 각도(도). 가운데를 기준으로 좌우 같은 간격으로 벌린다

    [Header("등급별 빛 색")]
    [SerializeField] private Color colorCommon = new Color32(0xC8, 0xC8, 0xC8, 0xFF);
    [SerializeField] private Color colorRare = new Color32(0x4D, 0xB8, 0xFF, 0xFF);
    [SerializeField] private Color colorEpic = new Color32(0xB4, 0x5C, 0xFF, 0xFF);
    [SerializeField] private Color colorLegendary = new Color32(0xFD, 0xCF, 0x17, 0xFF);

    [Header("상자")]
    [SerializeField] private ArtifactGrade goldChestMinGrade = ArtifactGrade.Epic; // 이 등급 이상이 나오면 금 상자를 쓴다
    [SerializeField] private RuntimeAnimatorController goldChestController;        // 셋 중 하나라도 비어 있으면 나무 상자를 쓴다
    [SerializeField] private Sprite goldChestFront;
    [SerializeField] private Sprite goldChestFrontOpening;
    [SerializeField] private Vector2 goldChestOffset = new Vector2(0f, -24f); // 금 상자 그림이 나무 상자보다 6px 위에 있어서 상자와 앞부분을 내려 빛 위치에 맞춘다

    [Header("보상")]
    [SerializeField] private float iconDelay = 2f;         // 상자가 열리고 상자를 치우기까지(초)
    [SerializeField] private float chestHideDuration = 0.3f; // 상자와 빛이 작아지며 사라지는 시간(초). 그 뒤 같은 자리에 보상이 나온다
    [SerializeField] private float chestHideScale = 0.6f;    // 상자가 사라질 때의 끝 크기
    [SerializeField] private float iconStartScale = 2.5f;  // 아이콘이 나타날 때의 크기(칸 크기에 대한 비율)
    [SerializeField] private float iconDuration = 0.18f;   // 아이콘 하나가 칸에 박히기까지의 시간(초)
    [SerializeField] private float iconInterval = 0.08f;   // 다음 아이콘이 나오기까지의 간격(초)
    [SerializeField] private float iconLandPunch = 0.15f;  // 칸에 박힐 때 찌그러지는 정도
    [SerializeField] private float outsideCloseDelay = 0.5f; // 보상이 다 나온 뒤 이 시간(초) 동안은 바깥을 눌러도 닫지 않는다. 건너뛰려고 연타하다 바로 닫히지 않게 한다

    [Header("등급 테두리")]
    [SerializeField] private Sprite gradeFrameCommon;
    [SerializeField] private Sprite gradeFrameRare;
    [SerializeField] private Sprite gradeFrameEpic;
    [SerializeField] private Sprite gradeFrameLegendary;

    // 보상 칸에 쓰는 오브젝트 묶음. SlotTemplate을 복제해서 만든다
    private class RewardSlot
    {
        public readonly RectTransform rect;
        public readonly CanvasGroup canvasGroup;
        public readonly Image imgIcon;
        public readonly Image imgGradeFrame;
        public readonly TextMeshProUGUI txtAmount;

        public RewardSlot(GameObject obj)
        {
            rect = (RectTransform)obj.transform;
            canvasGroup = GameUtil.TryGetComponent<CanvasGroup>(obj);
            imgIcon = GameUtil.Bind<Image>(obj, "Icon");
            imgGradeFrame = GameUtil.Bind<Image>(obj, "GradeFrame");
            txtAmount = GameUtil.Bind<TextMeshProUGUI>(obj, "Amount");
        }
    }

    private IReadOnlyList<RewardResponse> rewards;
    private GameDB gameDB;
    private Func<RewardResponse, Sprite> getRewardIcon;
    private readonly List<RewardSlot> listSlot = new List<RewardSlot>();

    // 나무 상자는 프리팹에 넣어 둔 컨트롤러와 앞부분 이미지를 쓴다
    private RuntimeAnimatorController woodChestController;
    private Sprite woodChestFront;
    private Sprite woodChestFrontOpening;
    private Vector2 vecWoodChestPosition;
    private Vector2 vecWoodChestFrontPosition;
    private Vector2 vecWoodChestFrontOpeningPosition;
    private Vector2 vecChestPosition; // 이번에 쓰는 상자의 제자리. 떨림이 끝나면 여기로 돌아온다
    private EffectPhase phase;
    private float finishedTime; // 보상이 다 나온 시각(시간 배율과 관계없는 시간)

    private Coroutine coOpenChest;
    private Tween tweenSeamFlicker;
    // 여러 연출이 동시에 돌아서 한꺼번에 멈출 수 있게 모아 둔다
    private readonly List<Tween> listTween = new List<Tween>();

    protected override void Awake()
    {
        base.Awake();

        rectChestArea = GameUtil.Bind<RectTransform>(transform, "ChestArea");
        canvasGroupChestArea = GameUtil.Bind<CanvasGroup>(transform, "ChestArea");
        rectChest = GameUtil.Bind<RectTransform>(transform, "ChestArea/Chest");
        btnChest = GameUtil.Bind<ButtonUI>(transform, "ChestArea/Chest");
        animatorChest = GameUtil.Bind<Animator>(transform, "ChestArea/Chest");
        imgLeakGlow = GameUtil.Bind<Image>(transform, "ChestArea/LeakGlow");
        imgSeamLight = GameUtil.Bind<Image>(transform, "ChestArea/SeamLight");
        imgChestFront = GameUtil.Bind<Image>(transform, "ChestArea/ChestFront");
        imgChestFrontOpening = GameUtil.Bind<Image>(transform, "ChestArea/ChestFrontOpening");
        imgGlow = GameUtil.Bind<Image>(transform, "ChestArea/Glow");
        rectRays = GameUtil.Bind<RectTransform>(transform, "ChestArea/Rays");
        canvasGroupRays = GameUtil.Bind<CanvasGroup>(transform, "ChestArea/Rays");
        foreach (Transform ray in rectRays)
        {
            listRay.Add(GameUtil.TryGetComponent<Image>(ray.gameObject));
        }
        rectResultPanel = GameUtil.Bind<RectTransform>(transform, "Panel");
        rectRewardGrid = GameUtil.Bind<RectTransform>(transform, "Panel/Stage/RewardGrid");
        objSlotTemplate = GameUtil.Bind<RectTransform>(transform, "Panel/Stage/RewardGrid/SlotTemplate").gameObject;
        btnClose = GameUtil.Bind<ButtonUI>(transform, "Panel/Footer/CloseButton");
        imgFlash = GameUtil.Bind<Image>(transform, "Flash");

        woodChestController = animatorChest.runtimeAnimatorController;
        woodChestFront = imgChestFront.sprite;
        woodChestFrontOpening = imgChestFrontOpening.sprite;
        vecWoodChestPosition = rectChest.anchoredPosition;
        vecWoodChestFrontPosition = imgChestFront.rectTransform.anchoredPosition;
        vecWoodChestFrontOpeningPosition = imgChestFrontOpening.rectTransform.anchoredPosition;

        btnChest.OnClickEvent += OnClickChest;
        btnClose.OnClickEvent += OnClickClose;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        StopEffect();
    }

    private void OnDestroy()
    {
        if (btnChest != null)
        {
            btnChest.OnClickEvent -= OnClickChest;
        }

        if (btnClose != null)
        {
            btnClose.OnClickEvent -= OnClickClose;
        }
    }

    // 서버가 준 보상 목록을 그대로 받는다. 상자 종류와 빛 색은 그중 유물의 최고 등급으로 정한다
    // 아이콘은 보상마다 getRewardIcon으로 받는다(유물 이미지, 재화 아이콘 등은 부르는 쪽이 정한다)
    public void Setup(IReadOnlyList<RewardResponse> rewards, GameDB gameDB, Func<RewardResponse, Sprite> getRewardIcon)
    {
        this.rewards = rewards;
        this.gameDB = gameDB;
        this.getRewardIcon = getRewardIcon;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        ArtifactGrade bestGrade = GetBestGrade(rewards, gameDB);
        ResetEffect(bestGrade);
    }

    // 연출 중에 바깥을 누르면 닫지 않고 연출을 건너뛴다. 다 끝난 직후에도 잠깐은 닫지 않는다
    protected override void OnClickDimmed()
    {
        if (phase != EffectPhase.Finished)
        {
            Skip();
            return;
        }

        if (Time.unscaledTime - finishedTime < outsideCloseDelay)
        {
            return;
        }

        base.OnClickDimmed();
    }

    // 결과 패널이나 빛처럼 버튼이 아닌 곳을 누르면 여기로 올라온다
    public void OnPointerClick(PointerEventData eventData)
    {
        Skip();
    }

    // 상자를 누르기 전, 상자가 사라지는 중, 다 끝난 뒤에는 할 일이 없다
    private void Skip()
    {
        switch (phase)
        {
            case EffectPhase.Opening:
                SkipToOpenedChest();
                break;
            case EffectPhase.Opened:
                SkipChestWait();
                break;
            case EffectPhase.Revealing:
                SkipToResult();
                break;
        }
    }

    // 다시 열 때도 닫힌 상자에서 시작한다
    private void ResetEffect(ArtifactGrade bestGrade)
    {
        StopEffect();
        phase = EffectPhase.Ready;

        bool useGoldChest = bestGrade >= goldChestMinGrade && goldChestController != null && goldChestFront != null &&
                            goldChestFrontOpening != null;
        animatorChest.runtimeAnimatorController = useGoldChest ? goldChestController : woodChestController;
        imgChestFront.sprite = useGoldChest ? goldChestFront : woodChestFront;
        imgChestFrontOpening.sprite = useGoldChest ? goldChestFrontOpening : woodChestFrontOpening;
        Vector2 offset = useGoldChest ? goldChestOffset : Vector2.zero;
        vecChestPosition = vecWoodChestPosition + offset;
        imgChestFront.rectTransform.anchoredPosition = vecWoodChestFrontPosition + offset;
        imgChestFrontOpening.rectTransform.anchoredPosition = vecWoodChestFrontOpeningPosition + offset;
        animatorChest.Rebind();
        animatorChest.Update(0f);

        rectChestArea.localScale = Vector3.one;
        canvasGroupChestArea.alpha = 1f;
        canvasGroupChestArea.blocksRaycasts = true;
        rectChest.anchoredPosition = vecChestPosition;
        rectChest.localRotation = Quaternion.identity;
        btnChest.Interactable = true;

        Color color = GetGradeColor(bestGrade);
        SetColor(imgLeakGlow, color, 0f);
        SetColor(imgSeamLight, color, 0f);
        SetColor(imgGlow, color, 0f);
        // 일반 1개, 희귀 2개, 영웅 3개, 전설 4개를 쓰고 나머지는 끈다. 프리팹에 놓인 광선 수보다 많이 쓰지는 않는다
        int rayCount = Mathf.Min(GetRayCount(bestGrade), listRay.Count);
        listRayAngle.Clear();
        for (int i = 0; i < listRay.Count; i++)
        {
            bool isUsed = i < rayCount;
            listRay[i].gameObject.SetActive(isUsed);
            if (isUsed == false)
            {
                continue;
            }

            // 왼쪽부터 raySpacing 간격으로 놓는다. 4개면 48, 16, -16, -48도
            float angle = ((rayCount - 1) * 0.5f - i) * raySpacing;
            listRayAngle.Add(angle);
            SetColor(listRay[i], color, 1f);
            listRay[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            listRay[i].rectTransform.localScale = new Vector3(1f, 0f, 1f);
        }
        canvasGroupRays.alpha = 0f;
        imgSeamLight.rectTransform.localScale = Vector3.one;
        SetColor(imgFlash, imgFlash.color, 0f);

        imgLeakGlow.gameObject.SetActive(false);
        imgSeamLight.gameObject.SetActive(false);
        imgChestFront.gameObject.SetActive(false);
        imgChestFrontOpening.gameObject.SetActive(false);
        imgGlow.gameObject.SetActive(false);
        rectRays.gameObject.SetActive(false);

        ClearRewardSlots();

        // 상자를 다 열 때까지 결과 패널은 숨겨 둔다
        rectResultPanel.gameObject.SetActive(false);
        btnClose.Interactable = false;
    }

    // 처음 누르면 상자를 열고, 연출 중에 다시 누르면 건너뛴다
    private void OnClickChest()
    {
        if (phase != EffectPhase.Ready)
        {
            Skip();
            return;
        }

        phase = EffectPhase.Opening;
        coOpenChest = StartCoroutine(CoOpenChest());
    }

    // 떨림(빛이 샌다) > 섬광 > 열리는 중 글로우가 터진다 > 다 열리면 광선 > 대기 > 보상 순서로 진행한다
    // 팝업과 애니메이터 모두 시간 배율과 관계없이 재생한다
    private IEnumerator CoOpenChest()
    {
        Sequence leak = PlayLeak();
        yield return leak.WaitForCompletion();

        rectChest.anchoredPosition = vecChestPosition;
        rectChest.localRotation = Quaternion.identity;

        // 틈 빛이 가장 밝은 채로 섬광이 터진다
        PlayFlash();
        yield return new WaitForSecondsRealtime(flashInDuration);

        // 화면이 가장 하얄 때 뚜껑이 들리기 시작한다. 뚜껑이 들리면 틈이 없어지므로 틈 빛을 끈다
        tweenSeamFlicker.Kill();
        imgSeamLight.gameObject.SetActive(false);
        animatorChest.SetTrigger(AnimOpenTrigger);
        yield return new WaitUntil(() => GetOpenAnimationTime() >= glowStartTime);

        PlayGlowBurst();
        bool isRare = GetBestGrade(rewards, gameDB) >= goldChestMinGrade;
        SoundManager.Instance.PlaySfx(isRare ? GameConstants.Sounds.GachaRevealRare : GameConstants.Sounds.GachaReveal);
        yield return new WaitUntil(() => GetOpenAnimationTime() >= 1f);

        // 다 열리면 앞 테두리까지 덮는 이미지로 바꾼다
        imgChestFrontOpening.gameObject.SetActive(false);
        imgChestFront.gameObject.SetActive(true);
        PlayRays();
        phase = EffectPhase.Opened;
        yield return CoRevealResult(true);
    }

    // 열린 상자를 iconDelay초 보여 준 뒤 치우고, 결과 패널을 띄워 보상을 채운다. waitChest가 false면 기다리지 않고 바로 치운다
    private IEnumerator CoRevealResult(bool waitChest)
    {
        if (waitChest)
        {
            yield return new WaitForSecondsRealtime(iconDelay);
        }

        phase = EffectPhase.Hiding;
        Sequence hide = HideChest();
        yield return hide.WaitForCompletion();

        phase = EffectPhase.Revealing;
        Sequence panel = ShowResultPanel();
        yield return panel.WaitForCompletion();

        ShowRewards();
        coOpenChest = null;
    }

    // 떨림, 섬광, 열림, 글로우, 광선을 끝난 모습으로 바로 맞춘다. 상자를 보여 주는 시간(iconDelay)부터 다시 이어 간다
    private void SkipToOpenedChest()
    {
        StopEffect();

        rectChestArea.localScale = Vector3.one;
        rectChest.anchoredPosition = vecChestPosition;
        rectChest.localRotation = Quaternion.identity;
        imgLeakGlow.gameObject.SetActive(false);
        imgSeamLight.gameObject.SetActive(false);
        SetColor(imgFlash, imgFlash.color, 0f);

        animatorChest.Play(AnimOpenState, 0, 1f);
        animatorChest.Update(0f);
        imgChestFrontOpening.gameObject.SetActive(false);
        imgChestFront.gameObject.SetActive(true);

        imgGlow.gameObject.SetActive(true);
        SetColor(imgGlow, imgGlow.color, glowAlpha);
        imgGlow.rectTransform.localScale = Vector3.one;

        rectRays.gameObject.SetActive(true);
        canvasGroupRays.alpha = rayAlpha;
        float screenTop = GetScreenTopY();
        for (int i = 0; i < listRayAngle.Count; i++)
        {
            RectTransform ray = listRay[i].rectTransform;
            ray.localRotation = Quaternion.Euler(0f, 0f, listRayAngle[i]);
            ray.sizeDelta = new Vector2(ray.sizeDelta.x, GetRayLength(listRayAngle[i], screenTop));
            ray.localScale = Vector3.one;
        }

        phase = EffectPhase.Opened;
        coOpenChest = StartCoroutine(CoRevealResult(true));
    }

    // 열린 상자를 보여 주는 시간만 건너뛰고 바로 상자를 치운다. 광선처럼 아직 도는 연출은 상자와 함께 사라지게 그대로 둔다
    private void SkipChestWait()
    {
        if (coOpenChest != null)
        {
            StopCoroutine(coOpenChest);
        }

        coOpenChest = StartCoroutine(CoRevealResult(false));
    }

    // 상자를 치우고 결과 패널에 보상을 다 채운 모습으로 바로 맞춘 뒤 닫기를 켠다
    private void SkipToResult()
    {
        StopEffect();

        SetColor(imgFlash, imgFlash.color, 0f);
        canvasGroupChestArea.alpha = 0f;
        canvasGroupChestArea.blocksRaycasts = false;

        rectResultPanel.gameObject.SetActive(true);
        rectResultPanel.localScale = Vector3.one;

        // 아이콘이 박히는 중이었으면 이미 만든 칸은 그대로 두고 나머지만 만든다
        for (int i = listSlot.Count; i < rewards.Count; i++)
        {
            CreateRewardSlot(rewards[i]);
        }
        foreach (RewardSlot slot in listSlot)
        {
            slot.canvasGroup.alpha = 1f;
            slot.rect.localScale = Vector3.one;
        }

        OnRewardsShown();
    }

    // 보상이 나올 자리를 비우려고 상자와 빛을 작아지며 사라지게 한다
    private Sequence HideChest()
    {
        canvasGroupChestArea.blocksRaycasts = false;

        Sequence sequence = CreateSequence();
        sequence.Join(canvasGroupChestArea.DOFade(0f, chestHideDuration).SetEase(Ease.InQuad));
        sequence.Join(rectChestArea.DOScale(chestHideScale, chestHideDuration).SetEase(Ease.InBack));
        AddTween(sequence);
        return sequence;
    }

    // 결과 패널을 켜고 팝업이 열릴 때와 같은 크기 연출로 띄운다
    private Sequence ShowResultPanel()
    {
        rectResultPanel.gameObject.SetActive(true);
        rectResultPanel.localScale = Vector3.one * GameConstants.PopupAnimation.OpenStartScale;

        Sequence sequence = CreateSequence();
        sequence.Join(rectResultPanel.DOScale(1f, GameConstants.PopupAnimation.OpenDuration)
            .SetEase(GameConstants.PopupAnimation.OpenScaleEase));
        AddTween(sequence);
        return sequence;
    }

    // 열림 애니메이션이 어디까지 재생됐는지(0~1). 아직 Open 상태가 아니면 0이다
    // Animator 창에서 Open 상태의 Speed나 키프레임을 바꿔도 실제 재생 위치를 따른다
    private float GetOpenAnimationTime()
    {
        if (animatorChest.IsInTransition(0))
        {
            return 0f;
        }

        AnimatorStateInfo state = animatorChest.GetCurrentAnimatorStateInfo(0);
        return state.shortNameHash == AnimOpenState ? state.normalizedTime : 0f;
    }

    // 상자가 떨리는 동안 뒤쪽 빛과 뚜껑 틈 빛이 점점 밝아진다. 틈 빛은 두께가 바뀌며 깜빡인다
    private Sequence PlayLeak()
    {
        imgLeakGlow.gameObject.SetActive(true);
        imgSeamLight.gameObject.SetActive(true);
        imgLeakGlow.rectTransform.localScale = Vector3.one * leakGlowStartScale;

        Sequence sequence = CreateSequence();
        sequence.Join(rectChest.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato, 90f, false, false));
        sequence.Join(rectChest.DOShakeRotation(shakeDuration, new Vector3(0f, 0f, shakeAngle), shakeVibrato, 90f, false));
        sequence.Join(imgLeakGlow.DOFade(leakGlowAlpha, shakeDuration).SetEase(Ease.InQuad));
        sequence.Join(imgLeakGlow.rectTransform.DOScale(1f, shakeDuration).SetEase(Ease.InQuad));
        sequence.Join(imgSeamLight.DOFade(seamAlpha, shakeDuration).SetEase(Ease.InQuad));
        AddTween(sequence);

        tweenSeamFlicker = imgSeamLight.rectTransform.DOScaleY(seamFlickerScale, seamFlickerDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        AddTween(tweenSeamFlicker);
        return sequence;
    }

    // 화면 전체가 순간 하얘졌다가 걷힌다
    private void PlayFlash()
    {
        Sequence sequence = CreateSequence();
        sequence.Append(imgFlash.DOFade(flashAlpha, flashInDuration));
        sequence.Append(imgFlash.DOFade(0f, flashOutDuration));
        AddTween(sequence);
    }

    // 뚜껑이 들리는 중에 글로우가 순간 크게 번졌다가 자리 잡는다. 앞부분은 뚜껑과 겹치지 않는 몸통만 덮는다
    private void PlayGlowBurst()
    {
        imgChestFrontOpening.gameObject.SetActive(true);
        imgGlow.gameObject.SetActive(true);
        imgGlow.rectTransform.localScale = Vector3.one * glowStartScale;

        float flashDuration = glowDuration * 0.3f;
        Sequence sequence = CreateSequence();
        sequence.Insert(0f, rectChestArea.DOPunchScale(Vector3.one * stagePunch, glowDuration, 6, 0.5f));
        sequence.Insert(0f, imgLeakGlow.DOFade(0f, glowDuration));
        sequence.Insert(0f, imgGlow.DOFade(glowAlpha, flashDuration));
        sequence.Insert(0f, imgGlow.rectTransform.DOScale(glowPunchScale, flashDuration).SetEase(Ease.OutQuad));
        sequence.Insert(flashDuration, imgGlow.rectTransform.DOScale(Vector3.one, glowDuration - flashDuration)
            .From(Vector3.one * glowPunchScale, false)
            .SetEase(Ease.InOutSine));
        AddTween(sequence);
    }

    // 광선이 rayInterval 간격으로 하나씩, 바깥쪽에서 돌아 들어오며 입구에서 짧게 튀어나온 뒤 화면 위 끝까지 길어진다. 제자리에 닿으면 멈춘다
    private void PlayRays()
    {
        rectRays.gameObject.SetActive(true);
        float screenTop = GetScreenTopY();

        Sequence sequence = CreateSequence();
        sequence.Insert(0f, canvasGroupRays.DOFade(rayAlpha, rayDuration * 0.3f));
        for (int i = 0; i < listRayAngle.Count; i++)
        {
            RectTransform ray = listRay[i].rectTransform;
            float angle = listRayAngle[i];
            // 기울어진 광선은 기운 쪽 바깥에서, 가운데 광선은 번갈아 가며 한쪽에서 들어온다
            float side = angle > 0f ? 1f : angle < 0f ? -1f : (i % 2 == 0 ? 1f : -1f);
            ray.localRotation = Quaternion.Euler(0f, 0f, angle + side * rayRotation);

            // 짧게 튀어나올 때는 화면 끝까지의 길이에 대한 비율만큼 줄여 둔다
            float length = GetRayLength(angle, screenTop);
            ray.sizeDelta = new Vector2(ray.sizeDelta.x, length);

            float start = i * rayInterval;
            sequence.Insert(start, ray.DOLocalRotate(new Vector3(0f, 0f, angle), rayDuration).SetEase(Ease.OutBack));
            sequence.Insert(start, ray.DOScaleY(rayStartLength / length, rayPopDuration).SetEase(Ease.OutQuad));
            sequence.Insert(start + rayPopDuration, ray.DOScaleY(1f, rayGrowDuration).SetEase(Ease.OutCubic));
        }
        AddTween(sequence);
    }

    // 기울어진 광선은 그만큼 더 길어야 위 끝에 닿는다
    private float GetRayLength(float angle, float screenTop)
    {
        return Mathf.Max(screenTop / Mathf.Cos(angle * Mathf.Deg2Rad), rayStartLength);
    }

    // 화면 위 끝이 광선 시작점(Rays)에서 얼마나 위에 있는지(Rays 기준 유닛)
    private float GetScreenTopY()
    {
        var canvasRect = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        var corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        return rectRays.InverseTransformPoint(corners[1]).y; // 1번이 왼쪽 위
    }

    // 보상 아이콘이 iconInterval 간격으로 하나씩, 크게 나타났다가 줄어들며 칸에 박힌다. 다 박히면 닫기를 켠다
    private void ShowRewards()
    {
        Sequence sequence = CreateSequence();
        for (int i = 0; i < rewards.Count; i++)
        {
            RewardSlot slot = CreateRewardSlot(rewards[i]);
            slot.canvasGroup.alpha = 0f;
            slot.rect.localScale = Vector3.one * iconStartScale;

            float start = i * iconInterval;
            sequence.Insert(start, slot.canvasGroup.DOFade(1f, iconDuration * 0.5f));
            sequence.Insert(start, slot.rect.DOScale(1f, iconDuration).SetEase(Ease.InQuad));
            // 박히는 순간 가로로 퍼지고 세로로 눌렸다가 돌아온다. 같은 크기를 앞에서도 바꾸므로 시작 값을 직접 정한다
            Vector3 squash = new Vector3(1f + iconLandPunch, 1f - iconLandPunch, 1f);
            sequence.Insert(start + iconDuration,
                slot.rect.DOScale(Vector3.one, iconDuration).From(squash, false).SetEase(Ease.OutBack));
        }
        sequence.OnComplete(OnRewardsShown);
        AddTween(sequence);
    }

    private void OnRewardsShown()
    {
        phase = EffectPhase.Finished;
        finishedTime = Time.unscaledTime;
        coOpenChest = null;
        btnClose.Interactable = true;
    }

    private RewardSlot CreateRewardSlot(RewardResponse reward)
    {
        GameObject obj = Instantiate(objSlotTemplate, rectRewardGrid);
        obj.name = $"Slot_{listSlot.Count:00}";
        obj.SetActive(true);

        var slot = new RewardSlot(obj);
        slot.imgIcon.sprite = getRewardIcon?.Invoke(reward);
        slot.imgIcon.enabled = slot.imgIcon.sprite != null;

        Sprite gradeFrame = GetGradeFrame(reward);
        slot.imgGradeFrame.sprite = gradeFrame;
        slot.imgGradeFrame.enabled = gradeFrame != null;

        // 유물 1개처럼 1이면 수량을 쓰지 않는다
        slot.txtAmount.text = reward.amount > 1 ? reward.amount.ToString("N0") : string.Empty;

        listSlot.Add(slot);
        return slot;
    }

    private void ClearRewardSlots()
    {
        foreach (RewardSlot slot in listSlot)
        {
            Destroy(slot.rect.gameObject);
        }
        listSlot.Clear();
    }

    // 유물이 아니거나 등급을 모르면 테두리를 쓰지 않는다
    private Sprite GetGradeFrame(RewardResponse reward)
    {
        if (reward.rewardType != RewardType.Artifact)
        {
            return null;
        }

        ArtifactData artifact = gameDB.artifacts.FirstOrDefault(data => data.code == reward.rewardCode);
        Sprite frame = null;
        switch (artifact?.grade)
        {
            case ArtifactGrade.Common:
                frame = gradeFrameCommon;
                break;
            case ArtifactGrade.Rare:
                frame = gradeFrameRare;
                break;
            case ArtifactGrade.Epic:
                frame = gradeFrameEpic;
                break;
            case ArtifactGrade.Legendary:
                frame = gradeFrameLegendary;
                break;
        }

        return frame;
    }

    private void OnClickClose()
    {
        Close();
    }

    private Sequence CreateSequence()
    {
        return DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void AddTween(Tween tween)
    {
        listTween.Add(tween.SetUpdate(true).SetLink(gameObject));
    }

    private void StopEffect()
    {
        if (coOpenChest != null)
        {
            StopCoroutine(coOpenChest);
            coOpenChest = null;
        }

        foreach (Tween tween in listTween)
        {
            tween.Kill();
        }
        listTween.Clear();
    }

    private static void SetColor(Image image, Color color, float alpha)
    {
        image.color = new Color(color.r, color.g, color.b, alpha);
    }

    private Color GetGradeColor(ArtifactGrade grade)
    {
        Color color = colorCommon;
        switch (grade)
        {
            case ArtifactGrade.Rare:
                color = colorRare;
                break;
            case ArtifactGrade.Epic:
                color = colorEpic;
                break;
            case ArtifactGrade.Legendary:
                color = colorLegendary;
                break;
        }

        return color;
    }

    private static int GetRayCount(ArtifactGrade grade)
    {
        int count = 1;
        switch (grade)
        {
            case ArtifactGrade.Rare:
                count = 2;
                break;
            case ArtifactGrade.Epic:
                count = 3;
                break;
            case ArtifactGrade.Legendary:
                count = 4;
                break;
        }

        return count;
    }

    // 유물이 아닌 보상만 있으면 일반 등급으로 본다
    private static ArtifactGrade GetBestGrade(IReadOnlyList<RewardResponse> rewards, GameDB gameDB)
    {
        ArtifactGrade bestGrade = ArtifactGrade.Common;
        foreach (RewardResponse reward in rewards)
        {
            if (reward.rewardType != RewardType.Artifact)
            {
                continue;
            }

            ArtifactData artifact = gameDB.artifacts.FirstOrDefault(data => data.code == reward.rewardCode);
            if (artifact != null && artifact.grade > bestGrade)
            {
                bestGrade = artifact.grade;
            }
        }

        return bestGrade;
    }
}
