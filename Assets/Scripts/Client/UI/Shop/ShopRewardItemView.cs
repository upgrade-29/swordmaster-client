using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상품의 대표 보상 1개만 표시한다. 보상 목록 전체나 지급 로직을 갖지 않는다.
public class ShopRewardItemView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image icon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI amountText;

    private void Awake()
    {
        icon = GameUtil.Bind<Image>(transform, "Icon");
        amountText = GameUtil.Bind<TextMeshProUGUI>(transform, "AmountText");
    }

    // 표시할 대표 보상이 없으면(rewardView == null) 항목 자체를 숨긴다.
    public void Bind(ShopProductRewardView rewardView)
    {
        gameObject.SetActive(rewardView != null);
        if (rewardView == null)
        {
            return;
        }

        icon.sprite = rewardView.RewardIcon;
        amountText.text = rewardView.AmountText;
    }
}
