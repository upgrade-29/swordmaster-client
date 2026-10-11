using System.Collections.Generic;

// ShopScreenView가 카테고리별 섹션 하나를 그리기 위해 받는 표시 전용 데이터
public class ShopProductSectionViewData
{
    public string CategoryDisplayName { get; }
    public IReadOnlyList<ShopProductViewData> Products { get; }

    public ShopProductSectionViewData(string categoryDisplayName, IReadOnlyList<ShopProductViewData> products)
    {
        CategoryDisplayName = categoryDisplayName;
        Products = products;
    }
}
