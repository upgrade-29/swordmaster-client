using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상품 1개의 이름/가격/보상/구매 입력 표시를 담당.

public class ShopProductItem : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image productVisual;
    [ReadOnly] [SerializeField] private TextMeshProUGUI productName;
    [ReadOnly] [SerializeField] private ShopRewardItem rewardItem;
    [ReadOnly] [SerializeField] private ButtonUI actionButton;
    [ReadOnly] [SerializeField] private Image actionIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI actionLabel;
    [ReadOnly] [SerializeField] private GameObject objActionLoading;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroupPrice;

    [ReadOnly(true)] [SerializeField] private float loadingRotationDuration = 1f; // 로딩 아이콘이 한 바퀴 도는 시간

    private ShopProductData product;
    public ShopProductData Product => product;

    public event Action<ShopProductItem> OnClickPurchaseEvent = delegate { };

    private void Awake()
    {
        // 카드 전체가 구매 버튼이다
        GameUtil.Bind(gameObject, ref actionButton);
        productName = GameUtil.Bind<TextMeshProUGUI>(transform, "ProductName");
        productVisual = GameUtil.Bind<Image>(transform, "VisualFrame/ProductVisual");
        rewardItem = GameUtil.Bind<ShopRewardItem>(transform, "VisualFrame/ShopRewardItem");
        actionIcon = GameUtil.Bind<Image>(transform, "Price/Icon");
        actionLabel = GameUtil.Bind<TextMeshProUGUI>(transform, "Price/Label");
        objActionLoading = GameUtil.Bind<RectTransform>(transform, "Loading").gameObject;
        GameUtil.BindOrAdd(GameUtil.Bind<RectTransform>(transform, "Price").gameObject, ref canvasGroupPrice);

        actionButton.OnClickEvent += OnClickActionButton;

        objActionLoading.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), loadingRotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    public void SetProduct(ShopProductData product, ShopProductRewardData representativeReward,
        Sprite productIcon, Sprite currencyIcon, Sprite rewardIcon)
    {
        this.product = product;

        productVisual.sprite = productIcon;
        productName.text = product.name;
        rewardItem.SetReward(representativeReward, rewardIcon);

        // ActionButton의 Label/Icon으로 가격 정보를 표시한다.
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
                SetLoading(false);
                actionButton.Interactable = true;
                actionLabel.text = product.price.ToString(GameConstants.NumberFormats.Integer);
                break;

            case ShopProductPurchaseState.Purchasing:
                SetLoading(true);
                actionButton.Interactable = false;
                break;

            case ShopProductPurchaseState.ResultUnknown:
                SetLoading(true);
                actionButton.Interactable = true;
                break;

            case ShopProductPurchaseState.SoldOut:
                SetLoading(false);
                actionButton.Interactable = false;
                actionLabel.text = "품절";
                break;

            case ShopProductPurchaseState.LimitReached:
                SetLoading(false);
                actionButton.Interactable = false;
                actionLabel.text = "구매 제한";
                break;

            case ShopProductPurchaseState.Unavailable:
                SetLoading(false);
                actionButton.Interactable = false;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    // 로딩 아이콘이 가격 줄 가운데에 겹치므로 로딩 중에는 가격을 숨긴다. 끄면 카드 레이아웃이 줄어들어서 투명도로 숨긴다
    private void SetLoading(bool loading)
    {
        objActionLoading.SetActive(loading);
        canvasGroupPrice.alpha = loading
            ? 0f
            : 1f;
    }

    private void OnClickActionButton()
    {
        OnClickPurchaseEvent.Invoke(this);
    }
}
