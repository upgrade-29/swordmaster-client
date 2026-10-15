using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 전투가 끝난 뒤 뜨는 결과 팝업. 받은 값을 보여주고 버튼 입력을 이벤트로 알리기만 한다
// ConfirmPopup의 제목/본문/확인/취소를 결과/설명/재도전/나가기로 쓰고, 스테이지와 보상 표시만 더한다
public class BattleResultPopup : ConfirmPopup
{
    private const float ShowDuration = 0.3f;
    private static readonly Color VictoryColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color DefeatColor = new Color(1f, 0.4f, 0.4f);

    [ReadOnly] [SerializeField] private CanvasGroup canvasGroup;
    [ReadOnly] [SerializeField] private RectTransform panel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI stageText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI rewardLabelText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI rewardValueText;

    private bool isVictory;
    private int stage;
    private string rewardLabel;
    private string rewardValue;
    private bool canRetry = true;
    private Sequence sequence;

    public event Action RetryClicked;
    public event Action ExitClicked;

    protected override void Awake()
    {
        base.Awake();

        GameUtil.Bind(gameObject, ref canvasGroup);
        panel = GameUtil.Bind<RectTransform>(transform, "Panel");
        stageText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/StageText");
        rewardLabelText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RewardBox/RewardLabelText");
        rewardValueText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RewardBox/RewardValueText");
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }

    // 보상이 없으면(0골드, 드랍 없음) 안내 문구만 보여준다
    public void Show(BattleApi.BattleEndResponse result, IReadOnlyList<string> artifactNames)
    {
        var description = result.isVictory
            ? "보스를 처치하고 스테이지를 클리어했습니다."
            : "장비를 강화한 뒤 다시 도전해보세요.";

        SetRewards(result.rewards, artifactNames);
        SetupResult(result.isVictory, result.stage, description);
        OpenWithAnimation();
    }

    // 요청이 거절됐을 때. 팝업이 열려 있으면(재도전 거절) 설명 문구만 바꾸고,
    // 닫혀 있으면(전투 중 검증 실패) 실패 팝업을 띄운다
    public void ShowRejected(int stage, string message)
    {
        if (IsOpened)
        {
            SetupResult(isVictory, this.stage, message);
            Open();
            return;
        }

        rewardLabel = "";
        rewardValue = "";
        SetupResult(false, stage, message);
        OpenWithAnimation();
    }

    // 전투 요청을 기다리는 동안은 재도전을 막는다. 처음 열기 전에 불려도 OnOpen에서 반영한다
    public void SetRetryInteractable(bool canRetry)
    {
        this.canRetry = canRetry;
        if (IsOpened)
            confirmButton.interactable = canRetry;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        confirmButton.interactable = canRetry;
        titleText.color = isVictory ? VictoryColor : DefeatColor;
        stageText.text = $"STAGE {stage}";
        rewardLabelText.text = rewardLabel;
        rewardValueText.text = rewardValue;
    }

    // 확인은 재도전, 취소는 나가기로 쓴다
    private void SetupResult(bool isVictory, int stage, string description)
    {
        this.isVictory = isVictory;
        this.stage = stage;
        Setup(isVictory ? "성공" : "실패", description, OnClickRetry, OnClickExit);
    }

    private void OpenWithAnimation()
    {
        Open();

        sequence?.Kill();
        canvasGroup.alpha = 0f;
        panel.localScale = Vector3.one * 0.8f;

        sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(1f, ShowDuration));
        sequence.Join(panel.DOScale(1f, ShowDuration).SetEase(Ease.OutBack));
    }

    private void SetRewards(BattleApi.BattleRewardsResponse rewards, IReadOnlyList<string> artifactNames)
    {
        if (rewards.gold <= 0 && artifactNames.Count == 0)
        {
            rewardLabel = "보상 없음";
            rewardValue = "";
            return;
        }

        var labels = new List<string>();
        var values = new List<string>();

        if (rewards.gold > 0)
        {
            labels.Add("골드");
            values.Add($"+{rewards.gold:N0}");
        }

        foreach (string artifactName in artifactNames)
        {
            labels.Add(artifactName);
            values.Add("+1");
        }

        rewardLabel = string.Join("\n", labels);
        rewardValue = string.Join("\n", values);
    }

    private void OnClickRetry() => RetryClicked?.Invoke();
    private void OnClickExit() => ExitClicked?.Invoke();
}
