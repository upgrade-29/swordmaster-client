using System;

// 현재 진행 중인 구매 1건의 수명, 중복 입력 방지, 요청 완료/실패/결과불명 처리를 담당한다.
// 한 클라이언트는 동시에 하나의 상점 구매만 진행한다. 실제 성공/실패 판정은 서버 응답을 그대로 따른다.
public class ShopPurchaseCoordinator
{
    private readonly IShopPurchaseService purchaseService;

    private string currentProductCode;
    private string currentPurchaseRequestId;

    public ShopPurchaseCoordinatorState State { get; private set; } = ShopPurchaseCoordinatorState.Idle;
    public bool IsPurchasing => State != ShopPurchaseCoordinatorState.Idle;

    public event Action<string, PurchaseResult> OnPurchaseSucceededEvent = delegate { };
    public event Action<string, string> OnPurchaseFailedEvent = delegate { };
    public event Action<string> OnPurchaseResultUnknownEvent = delegate { };

    public ShopPurchaseCoordinator(IShopPurchaseService purchaseService)
    {
        this.purchaseService = purchaseService;
    }

    // 이미 구매가 진행 중이면 새 구매 시작을 거부한다.
    public bool TryStartPurchase(string productCode)
    {
        if (IsPurchasing == true)
        {
            return false;
        }

        currentProductCode = productCode;
        currentPurchaseRequestId = Guid.NewGuid().ToString();
        State = ShopPurchaseCoordinatorState.Requesting;

        purchaseService.RequestPurchase(currentProductCode, currentPurchaseRequestId, OnRequestSucceeded, OnRequestFailed);
        return true;
    }

    // ResultUnknown 이후 같은 거래를 같은 purchaseRequestId로 다시 조회/재시도한다.
    public bool TryRetry()
    {
        if (State != ShopPurchaseCoordinatorState.Reconciling)
        {
            return false;
        }

        State = ShopPurchaseCoordinatorState.Requesting;
        purchaseService.RequestPurchase(currentProductCode, currentPurchaseRequestId, OnRequestSucceeded, OnRequestFailed);
        return true;
    }

    // 재조회 없이도 Idle로 되돌려야 하는 경우(사용자가 재시도를 포기함 등) 호출한다.
    public void CompleteReconcile()
    {
        if (State != ShopPurchaseCoordinatorState.Reconciling)
        {
            return;
        }

        State = ShopPurchaseCoordinatorState.Idle;
    }

    // 서버가 구매를 확정한 결과를 그대로 상위에 전달하고 Idle로 복귀한다.
    private void OnRequestSucceeded(PurchaseResult result)
    {
        string productCode = currentProductCode;
        State = ShopPurchaseCoordinatorState.Idle;
        OnPurchaseSucceededEvent.Invoke(productCode, result);
    }

    // 실패 종류에 따라 분기한다: 결과불명(ResultUnknown)이면 Reconciling 상태로 재시도 대기, 그 외 실패는 즉시 Idle로 되돌린다.
    private void OnRequestFailed(PurchaseFailure failure)
    {
        if (failure.Kind == PurchaseFailureKind.ResultUnknown)
        {
            State = ShopPurchaseCoordinatorState.Reconciling;
            OnPurchaseResultUnknownEvent.Invoke(currentProductCode);
            return;
        }

        string productCode = currentProductCode;
        State = ShopPurchaseCoordinatorState.Idle;
        OnPurchaseFailedEvent.Invoke(productCode, failure.Message);
    }
}
