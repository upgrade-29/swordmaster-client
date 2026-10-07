// 구매 요청이 실패했을 때 ShopPurchaseCoordinator에 전달되는 결과.
public class PurchaseFailure
{
    public PurchaseFailureKind Kind { get; }
    public string Message { get; }

    public PurchaseFailure(PurchaseFailureKind kind, string message)
    {
        Kind = kind;
        Message = message;
    }
}
