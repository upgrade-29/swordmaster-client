using System;
using TMPro;
using UnityEngine;

// 스테이지 선택 카드. 받은 값을 보여주고 버튼 입력을 이벤트로 알리기만 한다
public class StageSelectView : MonoBehaviour
{
    private TMP_Text stageTitleText;
    private TMP_Text statusText;
    private TMP_Text enemyInfoText;
    private TMP_Text rewardValueText;
    private TMP_Text myPowerText;
    private TMP_Text recommendedPowerText;
    private ButtonUI prevButton;
    private ButtonUI nextButton;
    private ButtonUI startButton;

    public event Action PrevClicked;
    public event Action NextClicked;
    public event Action StartClicked;

    private void Awake()
    {
        stageTitleText = GameUtil.Bind<TMP_Text>(gameObject, "Card/StageTitle");
        statusText = GameUtil.Bind<TMP_Text>(gameObject, "Card/StatusLabel");
        enemyInfoText = GameUtil.Bind<TMP_Text>(gameObject, "Card/EnemyInfoText");
        rewardValueText = GameUtil.Bind<TMP_Text>(gameObject, "Card/RewardRow/RewardValue");
        myPowerText = GameUtil.Bind<TMP_Text>(gameObject, "Card/PowerRow/MyPower/Value");
        recommendedPowerText = GameUtil.Bind<TMP_Text>(gameObject, "Card/PowerRow/RecommendedPower/Value");
        prevButton = GameUtil.Bind<ButtonUI>(gameObject, "Card/ImageArea/PrevButton");
        nextButton = GameUtil.Bind<ButtonUI>(gameObject, "Card/ImageArea/NextButton");
        startButton = GameUtil.Bind<ButtonUI>(gameObject, "Card/StartButton");

        prevButton.OnClickEvent += OnClickPrev;
        nextButton.OnClickEvent += OnClickNext;
        startButton.OnClickEvent += OnClickStart;
    }

    private void OnDestroy()
    {
        prevButton.OnClickEvent -= OnClickPrev;
        nextButton.OnClickEvent -= OnClickNext;
        startButton.OnClickEvent -= OnClickStart;
    }

    public void Show(StageData stage, StageState state, long myPower)
    {
        stageTitleText.text = $"STAGE {stage.stage}";
        statusText.text = GetStatusLabel(state);
        enemyInfoText.text = $"일반 적 {stage.enemyCount}기 + 보스";
        rewardValueText.text = GetRewardLabel(stage);
        myPowerText.text = myPower.ToString("N0");
        recommendedPowerText.text = stage.recommendedPower.ToString("N0");
        startButton.Interactable = state != StageState.Locked;
    }

    public void SetArrowsInteractable(bool hasPrev, bool hasNext)
    {
        prevButton.Interactable = hasPrev;
        nextButton.Interactable = hasNext;
    }

    private static string GetStatusLabel(StageState state)
    {
        switch (state)
        {
            case StageState.Cleared: return "클리어";
            case StageState.Available: return "도전 가능";
            case StageState.Locked: return "잠김";
            default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    // 일반 적을 모두 잡고 보스까지 이겼을 때 받는 골드의 합
    private static string GetRewardLabel(StageData stage)
    {
        long gold = stage.goldPerEnemy * stage.enemyCount + stage.bossGold;
        return stage.artifactDropRate > 0 ? $"골드 {gold:N0} + 아티팩트" : $"골드 {gold:N0}";
    }

    private void OnClickPrev() => PrevClicked?.Invoke();
    private void OnClickNext() => NextClicked?.Invoke();
    private void OnClickStart() => StartClicked?.Invoke();
}
