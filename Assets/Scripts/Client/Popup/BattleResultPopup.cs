using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투가 끝난 뒤 뜨는 결과 팝업. Setup으로 받은 결과를 보여주고, 재도전과 나가기 입력은 Setup으로 받은 콜백으로 돌려준다
public class BattleResultPopup : BasePopup
{
    private const float ShowDuration = 0.3f;
    private static readonly Color VictoryColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color DefeatColor = new Color(1f, 0.4f, 0.4f);

    [ReadOnly] [SerializeField] private CanvasGroup canvasGroup;
    [ReadOnly] [SerializeField] private RectTransform panel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI titleText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI descriptionText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI stageText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI rewardLabelText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI rewardValueText;
    [ReadOnly] [SerializeField] private Button retryButton;
    [ReadOnly] [SerializeField] private Button exitButton;

    private bool isVictory;
    private int stage;
    private string description;
    private string rewardLabel;
    private string rewardValue;
    private bool canRetry = true;
    private bool playOpenAnimation;
    private Action onRetry;
    private Action onExit;
    private Sequence seqOpen;

    protected override void Awake()
    {
        base.Awake();

        GameUtil.Bind(gameObject, ref canvasGroup);
        panel = GameUtil.Bind<RectTransform>(transform, "Panel");
        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/TitleBar/Title");
        descriptionText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Message");
        stageText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/StageText");
        rewardLabelText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RewardBox/RewardLabelText");
        rewardValueText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RewardBox/RewardValueText");
        retryButton = GameUtil.Bind<Button>(transform, "Panel/Buttons/ConfirmButton");
        exitButton = GameUtil.Bind<Button>(transform, "Panel/Buttons/CancelButton");

        retryButton.onClick.AddListener(OnClickRetry);
        exitButton.onClick.AddListener(OnClickExit);
    }

    private void OnDisable()
    {
        if (seqOpen != null)
        {
            seqOpen.Kill();
            seqOpen = null;
        }
    }

    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnClickRetry);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnClickExit);
        }
    }

    // 전투 결과를 받아 둔다. 보상이 없으면(0골드, 드랍 없음) 안내 문구만 보여준다
    public void Setup(BattleApi.BattleEndResponse result, IReadOnlyList<string> artifactNames, Action onRetry, Action onExit)
    {
        isVictory = result.isVictory;
        stage = result.stage;
        description = result.isVictory
            ? "보스를 처치하고 스테이지를 클리어했습니다."
            : "장비를 강화한 뒤 다시 도전해보세요.";
        SetRewards(result.rewards, artifactNames);

        this.onRetry = onRetry;
        this.onExit = onExit;
        playOpenAnimation = true;
    }

    // 요청이 거절됐을 때. 열려 있으면(재도전 거절) 설명 문구만 바꾸고, 닫혀 있으면(전투 중 검증 실패) 실패 결과로 채운다
    public void SetupRejected(int stage, string message, Action onRetry, Action onExit)
    {
        description = message;
        this.onRetry = onRetry;
        this.onExit = onExit;

        if (IsOpened)
        {
            playOpenAnimation = false;
            return;
        }

        isVictory = false;
        this.stage = stage;
        rewardLabel = string.Empty;
        rewardValue = string.Empty;
        playOpenAnimation = true;
    }

    // 전투 요청을 기다리는 동안은 재도전을 막는다. 처음 열기 전에 불려도 OnOpen에서 반영한다
    public void SetRetryInteractable(bool canRetry)
    {
        this.canRetry = canRetry;
        if (IsOpened)
        {
            retryButton.interactable = canRetry;
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        titleText.text = isVictory ? "성공" : "실패";
        titleText.color = isVictory ? VictoryColor : DefeatColor;
        descriptionText.text = description;
        stageText.text = $"STAGE {stage.ToString()}";
        rewardLabelText.text = rewardLabel;
        rewardValueText.text = rewardValue;
        retryButton.interactable = canRetry;

        // 결과를 새로 받았을 때만 연출한다. 열려 있는 팝업에 거절 문구만 바꿀 때는 연출하지 않는다
        if (playOpenAnimation)
        {
            playOpenAnimation = false;
            PlayOpenAnimation();
        }
    }

    private void PlayOpenAnimation()
    {
        if (seqOpen != null)
        {
            seqOpen.Kill();
            seqOpen = null;
        }

        canvasGroup.alpha = 0f;
        panel.localScale = Vector3.one * 0.8f;

        seqOpen = DOTween.Sequence();
        seqOpen.Append(canvasGroup.DOFade(1f, ShowDuration));
        seqOpen.Join(panel.DOScale(1f, ShowDuration).SetEase(Ease.OutBack));
    }

    private void SetRewards(BattleApi.BattleRewardsResponse rewards, IReadOnlyList<string> artifactNames)
    {
        if (rewards.gold <= 0 && artifactNames.Count == 0)
        {
            rewardLabel = "보상 없음";
            rewardValue = string.Empty;
            return;
        }

        var labels = new List<string>();
        var values = new List<string>();

        if (rewards.gold > 0)
        {
            labels.Add("골드");
            values.Add($"+{rewards.gold.ToString("N0")}");
        }

        foreach (string artifactName in artifactNames)
        {
            labels.Add(artifactName);
            values.Add("+1");
        }

        rewardLabel = string.Join("\n", labels);
        rewardValue = string.Join("\n", values);
    }

    private void OnClickRetry()
    {
        onRetry?.Invoke();
    }

    private void OnClickExit()
    {
        onExit?.Invoke();
    }
}
