using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상품 1개의 이름/가격/보상/구매 입력 표시를 담당.

public class ShopProductItemView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image productVisual;
    [ReadOnly] [SerializeField] private TextMeshProUGUI productName;
    [ReadOnly] [SerializeField] private ShopRewardItemView rewardItem;
    [ReadOnly] [SerializeField] private ShopButtonView actionButton;

    private string productCode;
    private string priceText;

    public string ProductCode => productCode;

    public event Action<string> OnPurchaseRequestedEvent = delegate { };

    private void Awake()
    {
        productVisual = GameUtil.Bind<Image>(transform, "ProductVisual");
        productName = GameUtil.Bind<TextMeshProUGUI>(transform, "Info/ProductName");
        rewardItem = GameUtil.Bind<ShopRewardItemView>(transform, "Info/ShopRewardItem");
        actionButton = GameUtil.Bind<ShopButtonView>(transform, "ActionButton");

        actionButton.OnClickEvent += OnClickActionButton;
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.OnClickEvent -= OnClickActionButton;
        }
    }

    // Bind를 여러 번 호출해도 이전 상품의 상태가 남지 않도록 항상 Ready 상태로 초기화한다.
    public void Bind(ShopProductViewData viewData)
    {
        productCode = viewData.ProductCode;
        priceText = viewData.PriceText;

        productVisual.sprite = viewData.ProductVisual;
        productName.text = viewData.DisplayName;
        rewardItem.Bind(viewData.RepresentativeRewardView);

        // ActionButton의 Label/Icon으로 가격 정보를 표시한다.
        actionButton.SetIcon(viewData.CurrencyIcon);

        SetPurchaseState(ShopProductPurchaseState.Ready);
    }

    // 구매 진행 상태에 따라 ActionButton의 로딩/상호작용 가능 여부/라벨을 전환한다.
    public void SetPurchaseState(ShopProductPurchaseState state)
    {
        switch (state)
        {
            case ShopProductPurchaseState.Ready:
                actionButton.SetLoading(false);
                actionButton.SetInteractable(true);
                actionButton.SetLabel(priceText);
                break;

            case ShopProductPurchaseState.Purchasing:
                actionButton.SetLoading(true);
                break;

            case ShopProductPurchaseState.SoldOut:
                actionButton.SetLoading(false);
                actionButton.SetInteractable(false);
                actionButton.SetLabel("품절");
                break;

            case ShopProductPurchaseState.LimitReached:
                actionButton.SetLoading(false);
                actionButton.SetInteractable(false);
                actionButton.SetLabel("구매 제한");
                break;

            case ShopProductPurchaseState.Unavailable:
                actionButton.SetLoading(false);
                actionButton.SetInteractable(false);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void OnClickActionButton()
    {
        OnPurchaseRequestedEvent.Invoke(productCode);
    }
}
