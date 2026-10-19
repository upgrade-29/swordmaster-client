using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 가챠 결과 한 건의 아이콘과 텍스트를 표시하고 다음 입력만 Presenter에 전달한다.
public class GachaResultPopup : BasePopup
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI titleText;
    [ReadOnly] [SerializeField] private Image rewardIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI rewardNameText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI rewardAmountText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI progressText;
    [ReadOnly] [SerializeField] private ButtonUI nextButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI nextButtonLabel;

    private RewardResponse reward;
    private int index;
    private int totalCount;
    private Sprite rewardSprite;
    private GameDB gameDB;
    private Action onNext;

    protected override void Awake()
    {
        base.Awake();

        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Header/Title");
        rewardIcon = GameUtil.Bind<Image>(transform, "Panel/RewardBox/RewardIcon");
        rewardNameText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RewardBox/RewardName");
        rewardAmountText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RewardBox/RewardAmount");
        progressText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/ProgressText");
        nextButton = GameUtil.Bind<ButtonUI>(transform, "Panel/Footer/NextButton");
        nextButtonLabel = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Footer/NextButton/Label");

        nextButton.OnClickEvent += OnClickNext;
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.OnClickEvent -= OnClickNext;
        }
    }

    // 보상 한 건마다 다시 Setup하고 Open한다. 이미 열려 있으면 Open이 새 보상만 다시 반영한다
    public void Setup(RewardResponse reward, int index, int totalCount, Sprite rewardSprite, GameDB gameDB,
        Action onNext)
    {
        this.reward = reward;
        this.index = index;
        this.totalCount = totalCount;
        this.rewardSprite = rewardSprite;
        this.gameDB = gameDB;
        this.onNext = onNext;
    }

    // 보상 타입 아이콘을 재사용하고 개별 유물 Sprite 매핑 없이 이름 또는 코드 문자열을 표시한다.
    protected override void OnOpen()
    {
        base.OnOpen();

        titleText.text = "가챠 결과";
        rewardIcon.sprite = rewardSprite;
        rewardNameText.text = GetRewardName(reward, gameDB);
        rewardAmountText.text = reward.amount.ToString();
        progressText.text = $"{index + 1} / {totalCount}";
        nextButtonLabel.text = index + 1 == totalCount ? "닫기" : "다음";
        nextButton.Interactable = true;
    }

    private static string GetRewardName(RewardResponse reward, GameDB gameDB)
    {
        if (reward.rewardType == RewardType.Artifact && string.IsNullOrEmpty(reward.rewardCode) == false)
        {
            ArtifactData artifact = gameDB.artifacts.FirstOrDefault(data => data.code == reward.rewardCode);
            return artifact != null ? artifact.name : reward.rewardCode;
        }

        return string.IsNullOrEmpty(reward.rewardCode) ? reward.rewardType.ToString() : reward.rewardCode;
    }

    private void OnClickNext()
    {
        onNext?.Invoke();
    }
}
