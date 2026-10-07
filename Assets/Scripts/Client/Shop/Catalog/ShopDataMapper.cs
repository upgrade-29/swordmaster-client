using System.Collections.Generic;
using System.Linq;

// GameDB DTO → ShopProductModel → ShopProductViewData 변환만 담당.
public static class ShopDataMapper
{
    // GameDB의 shopProducts/shopProductRewards를 productCode 기준으로 묶어 ShopProductModel 목록을 만든다.
    public static IReadOnlyList<ShopProductModel> ToProductModels(GameDB gameDB)
    {
        return gameDB.shopProducts
            .Select(productData => new ShopProductModel(
                productData,
                gameDB.shopProductRewards
                    .Where(reward => reward.productCode == productData.productCode)
                    .ToList()))
            .ToList();
    }

    // ShopProductModel을 Category 기준으로 묶어 ShopScreenView가 섹션 단위로 그릴 수 있는 목록을 만든다.
    // 섹션 순서는 서버가 내려준 상품 목록(ShopCatalog.Products)에서 각 카테고리가 처음 등장하는 순서를 따른다.
    public static IReadOnlyList<ShopProductSectionViewData> ToSectionViewDataList(
        IReadOnlyList<ShopProductModel> products, ShopVisualCatalogSO visualCatalog)
    {
        return products
            .GroupBy(product => product.Category)
            .Select(group => new ShopProductSectionViewData(
                visualCatalog.GetCategoryLabel(group.Key),
                group.Select(product => ToViewData(product, visualCatalog)).ToList()))
            .ToList();
    }

    // ShopProductModel과 표현 전용 아이콘 매핑을 결합해 ShopProductItemView가 바인딩할 ViewData를 만든다.
    // 대표 보상은 Rewards의 첫 항목을 사용한다.
    public static ShopProductViewData ToViewData(ShopProductModel product, ShopVisualCatalogSO visualCatalog)
    {
        ShopProductRewardData representativeReward = product.Rewards.FirstOrDefault();

        return new ShopProductViewData(
            product.ProductCode,
            product.Name,
            product.Price.ToString(),
            visualCatalog.GetCurrencyIcon(product.PriceType),
            ToRewardView(representativeReward, visualCatalog),
            visualCatalog.GetProductIcon(product.IconCode));
    }

    private static ShopProductRewardView ToRewardView(ShopProductRewardData reward, ShopVisualCatalogSO visualCatalog)
    {
        if (reward == null)
        {
            return null;
        }

        return new ShopProductRewardView(
            visualCatalog.GetRewardTypeIcon(reward.rewardType),
            reward.amount.ToString());
    }
}
