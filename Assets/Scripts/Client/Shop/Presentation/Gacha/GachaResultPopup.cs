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
    [ReadOnly] [SerializeField] private ShopButtonView nextButton;

    public event Action OnNextRequestedEvent = delegate { };

    protected override void Awake()
    {
        base.Awake();

        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Root/Title");
        rewardIcon = GameUtil.Bind<Image>(transform, "Root/RewardIcon");
        rewardNameText = GameUtil.Bind<TextMeshProUGUI>(transform, "Root/RewardName");
        rewardAmountText = GameUtil.Bind<TextMeshProUGUI>(transform, "Root/RewardAmount");
        progressText = GameUtil.Bind<TextMeshProUGUI>(transform, "Root/ProgressText");
        nextButton = GameUtil.Bind<ShopButtonView>(transform, "Root/NextButton");

        nextButton.OnClickEvent += OnClickNext;
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.OnClickEvent -= OnClickNext;
        }
    }

    // 보상 타입 아이콘을 재사용하고 개별 유물 Sprite 매핑 없이 이름 또는 코드 문자열을 표시한다.
    public void Show(PurchaseRewardResult reward, int index, int totalCount,
        ShopVisualCatalogSO visualCatalog, GameDB gameDB)
    {
        titleText.text = "가챠 결과";
        rewardIcon.sprite = visualCatalog.GetRewardTypeIcon(reward.rewardType);
        rewardNameText.text = GetRewardName(reward, gameDB);
        rewardAmountText.text = reward.amount.ToString();
        progressText.text = $"{index + 1} / {totalCount}";
        nextButton.SetLabel(index + 1 == totalCount ? "닫기" : "다음");
        nextButton.SetInteractable(true);
    }

    private static string GetRewardName(PurchaseRewardResult reward, GameDB gameDB)
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
        OnNextRequestedEvent.Invoke();
    }
}
