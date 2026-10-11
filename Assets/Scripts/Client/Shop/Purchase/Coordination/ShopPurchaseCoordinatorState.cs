// 성공과 명시적 실패는 이벤트를 전달한 뒤 Idle로 돌아가는 통과 상태다.
public enum ShopPurchaseCoordinatorState
{
    Idle,
    Requesting,
    Reconciling,
}
