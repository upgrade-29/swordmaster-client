// shop-network-flow.md 4절 상태 머신의 관찰 가능한 상태.
// Succeeded/FailedKnown은 즉시 Idle로 돌아가는 통과 상태라 별도 값 없이 이벤트로만 알린다.
public enum ShopPurchaseCoordinatorState
{
    Idle,
    Requesting,
    Reconciling,
}
