using System;

// 실행 환경에 맞는 구매 서비스 구현체를 한 곳에서 선택한다.
public static class ShopPurchaseServiceComposition
{
    // 로그인했으면 서버로 구매하고, 로그인하지 않았으면 구매를 명시적으로 실패시킨다.
    public static IShopPurchaseService CreateRuntime()
    {
        if (AuthJwtToken.HasValue)
        {
            return new HttpShopPurchaseService();
        }

        return new ShopPurchaseServiceUnavailable();
    }

    // EditMode 등 테스트는 필요한 대역을 이 단일 조립 지점에서 명시적으로 주입한다.
    public static IShopPurchaseService CreateForTesting(IShopPurchaseService purchaseService)
    {
        return purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
    }
}
