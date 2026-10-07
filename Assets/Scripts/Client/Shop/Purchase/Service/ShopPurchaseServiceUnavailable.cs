using System;

// 실서버 전송 경계가 준비되기 전 런타임 구매를 명시적으로 실패시킨다.
public class ShopPurchaseServiceUnavailable : IShopPurchaseService
{
    // 공용 인증과 서버 전송이 준비되지 않은 런타임 경로를 구매 실패로 명시한다.
    public void RequestPurchase(ShopPurchaseRequest request, Action<PurchaseResult> onSuccess,
        Action<PurchaseFailure> onFailure)
    {
        onFailure(new PurchaseFailure(PurchaseFailureKind.ServiceUnavailable,
            "구매 서버 서비스가 아직 연결되지 않았습니다."));
    }
}
