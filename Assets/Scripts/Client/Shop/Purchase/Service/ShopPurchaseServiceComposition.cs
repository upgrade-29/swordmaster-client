using System;

// 실행 환경에 맞는 구매 서비스 구현체를 한 곳에서 선택한다.
public static class ShopPurchaseServiceComposition
{
    // 런타임 구매는 공용 인증·전송 구현이 제공될 때까지 명시적으로 사용할 수 없다.
    public static IShopPurchaseService CreateRuntime()
    {
        return new ShopPurchaseServiceUnavailable();
    }

    // EditMode 등 테스트는 필요한 대역을 이 단일 조립 지점에서 명시적으로 주입한다.
    public static IShopPurchaseService CreateForTesting(IShopPurchaseService purchaseService)
    {
        return purchaseService ?? throw new ArgumentNullException(nameof(purchaseService));
    }
}
