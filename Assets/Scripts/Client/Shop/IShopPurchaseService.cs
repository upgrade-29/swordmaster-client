using System;

// 상점 구매 요청의 최소 인터페이스 경계. 실제 HTTP/JWT 구현(기존 Network Layer)은 이 인터페이스 뒤에서 이루어지며,
// 현재 프로젝트에는 아직 그 구현체가 없다.
public interface IShopPurchaseService
{
    // 지정한 상품의 구매를 서버에 요청하고 결과를 콜백으로 전달한다. purchaseRequestId는 재시도 시에도 동일 거래로 식별하기 위한 값이다.
    void RequestPurchase(string productCode, string purchaseRequestId,
        Action<PurchaseResult> onSuccess, Action<PurchaseFailure> onFailure);
}
