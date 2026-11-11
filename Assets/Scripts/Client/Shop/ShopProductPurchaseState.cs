// ShopProductItem의 표시 상태
public enum ShopProductPurchaseState
{
    Ready,
    Purchasing,
    ResultUnknown,  // 구매 결과를 알 수 없어 재전송을 기다리는 상태. 로딩을 표시하지만 눌러서 재전송 팝업을 다시 열 수 있다
    SoldOut,
    LimitReached,
    Unavailable,
}
