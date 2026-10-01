// shop-network-flow.md 4절: 서버가 명확히 실패를 응답했는지(FailedKnown), Timeout/연결 끊김으로 결과를 알 수 없는지(ResultUnknown) 구분한다.
public enum PurchaseFailureKind
{
    FailedKnown,
    ResultUnknown,
}
