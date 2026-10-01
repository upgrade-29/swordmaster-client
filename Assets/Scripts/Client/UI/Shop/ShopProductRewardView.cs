using UnityEngine;

// 상품 카드에 표시하는 대표 보상 1개의 표현 데이터
public class ShopProductRewardView
{
    public Sprite RewardIcon { get; }
    public string AmountText { get; }

    public ShopProductRewardView(Sprite rewardIcon, string amountText)
    {
        RewardIcon = rewardIcon;
        AmountText = amountText;
    }
}
