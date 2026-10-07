// 구매 요청이 서버에서 최종 처리됐는지와 화면이 취할 후속 동작을 구분한다.
public enum PurchaseFailureKind
{
    BadRequest,
    AuthenticationRequired,
    ProductNotFound,
    InsufficientCurrency,
    ResultUnknown,
    ServiceUnavailable,
}
