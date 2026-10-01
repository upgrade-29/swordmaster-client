using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투가 끝난 뒤 뜨는 결과 팝업. 받은 값을 보여주고 버튼 입력을 이벤트로 알리기만 한다
public class BattleResultView : MonoBehaviour
{
    private const float ShowDuration = 0.3f;
    private static readonly Color VictoryColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color DefeatColor = new Color(1f, 0.4f, 0.4f);

    private CanvasGroup canvasGroup;
    private RectTransform window;
    private TMP_Text resultText;
    private TMP_Text stageText;
    private TMP_Text descriptionText;
    private TMP_Text rewardLabelText;
    private TMP_Text rewardValueText;
    private Button retryButton;
    private Button exitButton;
    private Sequence sequence;

    public event Action RetryClicked;
    public event Action ExitClicked;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref canvasGroup);
        window = GameUtil.Bind<RectTransform>(gameObject, "Window");
        resultText = GameUtil.Bind<TMP_Text>(gameObject, "Window/ResultText");
        stageText = GameUtil.Bind<TMP_Text>(gameObject, "Window/StageText");
        descriptionText = GameUtil.Bind<TMP_Text>(gameObject, "Window/DescriptionText");
        rewardLabelText = GameUtil.Bind<TMP_Text>(gameObject, "Window/RewardBox/RewardLabelText");
        rewardValueText = GameUtil.Bind<TMP_Text>(gameObject, "Window/RewardBox/RewardValueText");
        retryButton = GameUtil.Bind<Button>(gameObject, "Window/RetryButton");
        exitButton = GameUtil.Bind<Button>(gameObject, "Window/ExitButton");

        retryButton.onClick.AddListener(OnClickRetry);
        exitButton.onClick.AddListener(OnClickExit);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        sequence?.Kill();
        retryButton.onClick.RemoveListener(OnClickRetry);
        exitButton.onClick.RemoveListener(OnClickExit);
    }

    // 보상이 없으면(0골드, 드랍 없음) 안내 문구만 보여준다
    public void Show(BattleResult result, IReadOnlyList<string> artifactNames)
    {
        resultText.text = result.isVictory ? "성공" : "실패";
        resultText.color = result.isVictory ? VictoryColor : DefeatColor;
        stageText.text = $"STAGE {result.stage}";
        descriptionText.text = result.isVictory
            ? "보스를 처치하고 스테이지를 클리어했습니다."
            : "장비를 강화한 뒤 다시 도전해보세요.";

        SetRewards(result.rewards, artifactNames);

        sequence?.Kill();
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        window.localScale = Vector3.one * 0.8f;

        sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(1f, ShowDuration));
        sequence.Join(window.DOScale(1f, ShowDuration).SetEase(Ease.OutBack));
    }

    public void Hide()
    {
        sequence?.Kill();
        gameObject.SetActive(false);
    }

    private void SetRewards(BattleRewards rewards, IReadOnlyList<string> artifactNames)
    {
        if (rewards.gold <= 0 && artifactNames.Count == 0)
        {
            rewardLabelText.text = "보상 없음";
            rewardValueText.text = "";
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

        rewardLabelText.text = string.Join("\n", labels);
        rewardValueText.text = string.Join("\n", values);
    }

    private void OnClickRetry() => RetryClicked?.Invoke();
    private void OnClickExit() => ExitClicked?.Invoke();
}
