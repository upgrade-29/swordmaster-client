using System.Collections.Generic;
using System.Linq;

// 현재 GameDB의 상점 상품을 읽기 전용 런타임 목록으로 보관한다.
public class ShopCatalog
{
    private readonly IReadOnlyList<ShopProductModel> products;

    public IReadOnlyList<ShopProductModel> Products => products;

    public ShopCatalog(GameDB gameDB)
    {
        products = ShopDataMapper.ToProductModels(gameDB);
    }

    public ShopProductModel GetProduct(string productCode)
    {
        return products.FirstOrDefault(product => product.ProductCode == productCode);
    }
}
