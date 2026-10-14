using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상품 1개의 이름/가격/보상/구매 입력 표시를 담당.

public class ShopProductItem : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image productVisual;
    [ReadOnly] [SerializeField] private TextMeshProUGUI productName;
    [ReadOnly] [SerializeField] private ShopRewardItem rewardItem;
    [ReadOnly] [SerializeField] private Button actionButton;
    [ReadOnly] [SerializeField] private Image actionIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI actionLabel;
    [ReadOnly] [SerializeField] private GameObject objActionLoading;

    private ShopProductData product;
    public ShopProductData Product => product;

    public event Action<ShopProductItem> OnClickPurchaseEvent = delegate { };

    private void Awake()
    {
        productVisual = GameUtil.Bind<Image>(transform, "ProductVisual");
        productName = GameUtil.Bind<TextMeshProUGUI>(transform, "Info/ProductName");
        rewardItem = GameUtil.Bind<ShopRewardItem>(transform, "Info/ShopRewardItem");
        actionButton = GameUtil.Bind<Button>(transform, "ActionButton");
        actionIcon = GameUtil.Bind<Image>(transform, "ActionButton/Icon");
        actionLabel = GameUtil.Bind<TextMeshProUGUI>(transform, "ActionButton/Label");
        objActionLoading = GameUtil.Bind<RectTransform>(transform, "ActionButton/Loading").gameObject;

        actionButton.onClick.AddListener(OnClickActionButton);
    }

    public void SetProduct(ShopProductData product, ShopProductRewardData representativeReward,
        ShopVisualCatalogSO visualCatalog)
    {
        this.product = product;

        productVisual.sprite = visualCatalog.GetProductIcon(product.iconCode);
        productName.text = product.name;
        rewardItem.SetReward(representativeReward, visualCatalog);

        // ActionButton의 Label/Icon으로 가격 정보를 표시한다.
        Sprite currencyIcon = visualCatalog.GetCurrencyIcon(product.priceType);
        actionIcon.sprite = currencyIcon;
        actionIcon.gameObject.SetActive(currencyIcon != null);

        SetPurchaseState(ShopProductPurchaseState.Ready);
    }

    // 구매 진행 상태에 따라 ActionButton의 로딩/상호작용 가능 여부/라벨을 전환한다.
    public void SetPurchaseState(ShopProductPurchaseState state)
    {
        switch (state)
        {
            case ShopProductPurchaseState.Ready:
                objActionLoading.SetActive(false);
                actionButton.interactable = true;
                actionLabel.text = product.price.ToString();
                break;

            case ShopProductPurchaseState.Purchasing:
                objActionLoading.SetActive(true);
                actionButton.interactable = false;
                break;

            case ShopProductPurchaseState.ResultUnknown:
                objActionLoading.SetActive(true);
                actionButton.interactable = true;
                break;

            case ShopProductPurchaseState.SoldOut:
                objActionLoading.SetActive(false);
                actionButton.interactable = false;
                actionLabel.text = "품절";
                break;

            case ShopProductPurchaseState.LimitReached:
                objActionLoading.SetActive(false);
                actionButton.interactable = false;
                actionLabel.text = "구매 제한";
                break;

            case ShopProductPurchaseState.Unavailable:
                objActionLoading.SetActive(false);
                actionButton.interactable = false;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void OnClickActionButton()
    {
        OnClickPurchaseEvent.Invoke(this);
    }
}
