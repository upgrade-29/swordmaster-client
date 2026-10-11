using System;

// 구매 요청의 전송 구현과 화면·상태 흐름을 분리하는 기능 경계다.
public interface IShopPurchaseService
{
    // 구매 기능 모델을 전달하고 서버 확정 결과 또는 분류된 실패를 반환한다.
    void RequestPurchase(ShopPurchaseRequest request, Action<PurchaseResult> onSuccess,
        Action<PurchaseFailure> onFailure);
}
