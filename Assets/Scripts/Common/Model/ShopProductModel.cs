using System.Collections.Generic;

// GameDB의 ShopProductData와 해당 상품의 ShopProductRewardData 전체를 묶은 런타임 표시용 모델
public class ShopProductModel
{
    public string ProductCode { get; }
    public string Name { get; }
    public ShopCategory Category { get; }
    public string IconCode { get; }
    public CurrencyType PriceType { get; }
    public long Price { get; }
    public IReadOnlyList<ShopProductRewardData> Rewards { get; }

    public ShopProductModel(ShopProductData productData, IReadOnlyList<ShopProductRewardData> rewards)
    {
        ProductCode = productData.productCode;
        Name = productData.name;
        Category = productData.category;
        IconCode = productData.iconCode;
        PriceType = productData.priceType;
        Price = productData.price;
        Rewards = rewards ?? new List<ShopProductRewardData>();
    }
}
