using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상품의 대표 보상 1개만 표시한다. 보상 목록 전체나 지급 로직을 갖지 않는다.
public class ShopRewardItem : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image icon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI amountText;

    private void Awake()
    {
        icon = GameUtil.Bind<Image>(transform, "Icon");
        amountText = GameUtil.Bind<TextMeshProUGUI>(transform, "AmountText");
    }

    // 표시할 대표 보상이 없으면(reward == null) 항목 자체를 숨긴다.
    public void SetReward(ShopProductRewardData reward, Sprite rewardIcon)
    {
        gameObject.SetActive(reward != null);
        if (reward == null)
        {
            return;
        }

        // 가챠 상자는 뽑기 횟수라서 아이콘 없이 X10처럼 표시한다
        bool isGachaBox = reward.rewardType == RewardType.GachaBox;
        icon.gameObject.SetActive(isGachaBox == false);
        icon.sprite = rewardIcon;
        amountText.text = isGachaBox
            ? $"X{reward.amount.ToString(GameConstants.NumberFormats.Integer)}"
            : reward.amount.ToString(GameConstants.NumberFormats.Integer);
    }
}
