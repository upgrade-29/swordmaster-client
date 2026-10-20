using Newtonsoft.Json;

public class ShopProductData
{
    public readonly string productCode;
    public readonly string name;
    public readonly ShopCategory category;
    public readonly string iconCode;
    public readonly PriceType priceType;
    public readonly long price;

    [JsonConstructor]
    public ShopProductData(string productCode, string name, ShopCategory category, string iconCode,
        PriceType priceType, long price)
    {
        this.productCode = productCode;
        this.name = name;
        this.category = category;
        this.iconCode = iconCode;
        this.priceType = priceType;
        this.price = price;
    }
}
