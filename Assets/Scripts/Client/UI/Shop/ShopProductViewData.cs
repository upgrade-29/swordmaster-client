using UnityEngine;

// ShopProductItemView가 서버 DTO를 직접 참조하지 않도록 ShopDataMapper가 만들어 전달하는 표시 전용 데이터
public class ShopProductViewData
{
    // ShopProductItemView가 PurchaseRequested 이벤트에 실어 전달할 상품 식별자. 표시용 값은 아니다.
    public string ProductCode { get; }
    public string DisplayName { get; }
    public string PriceText { get; }
    public Sprite CurrencyIcon { get; }
    public ShopProductRewardView RepresentativeRewardView { get; }
    public Sprite ProductVisual { get; }

    public ShopProductViewData(string productCode, string displayName, string priceText, Sprite currencyIcon,
        ShopProductRewardView representativeRewardView, Sprite productVisual)
    {
        ProductCode = productCode;
        DisplayName = displayName;
        PriceText = priceText;
        CurrencyIcon = currencyIcon;
        RepresentativeRewardView = representativeRewardView;
        ProductVisual = productVisual;
    }
}
