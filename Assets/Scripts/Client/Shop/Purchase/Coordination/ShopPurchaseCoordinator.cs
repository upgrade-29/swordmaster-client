using System;

// 현재 진행 중인 구매 1건의 수명, 중복 입력 방지, 요청 완료/실패/결과불명 처리를 담당한다.
// 한 클라이언트는 동시에 하나의 상점 구매만 진행한다. 실제 성공/실패 판정은 서버 응답을 그대로 따른다.
public class ShopPurchaseCoordinator
{
    private readonly IShopPurchaseService purchaseService;

    private string currentProductCode;
    private Guid currentRequestId;

    public ShopPurchaseCoordinatorState State { get; private set; } = ShopPurchaseCoordinatorState.Idle;
    public bool IsPurchasing => State != ShopPurchaseCoordinatorState.Idle;

    public event Action<string, PurchaseResult> OnPurchaseSucceededEvent = delegate { };
    public event Action<string, string> OnPurchaseFailedEvent = delegate { };
    public event Action<string> OnAuthenticationRequiredEvent = delegate { };
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
        currentRequestId = Guid.NewGuid();
        State = ShopPurchaseCoordinatorState.Requesting;

        RequestCurrentPurchase();
        return true;
    }

    // 결과 불명 뒤 사용자가 선택한 경우에만 동일 UUID로 구매를 재전송한다.
    public bool TryRetry()
    {
        if (State != ShopPurchaseCoordinatorState.Reconciling)
        {
            return false;
        }

        State = ShopPurchaseCoordinatorState.Requesting;
        RequestCurrentPurchase();
        return true;
    }

    // 결과 확인 팝업만 닫고 거래 상태와 동일 UUID는 유지한다.
    public void DismissReconcile()
    {
    }

    // 현재 거래 식별자를 바꾸지 않고 서비스 경계에 구매 요청을 전달한다.
    private void RequestCurrentPurchase()
    {
        purchaseService.RequestPurchase(new ShopPurchaseRequest(currentRequestId, currentProductCode),
            OnRequestSucceeded, OnRequestFailed);
    }

    // 서버가 구매를 확정한 결과를 그대로 상위에 전달하고 Idle로 복귀한다.
    private void OnRequestSucceeded(PurchaseResult result)
    {
        string productCode = currentProductCode;
        State = ShopPurchaseCoordinatorState.Idle;
        OnPurchaseSucceededEvent.Invoke(productCode, result);
    }

    // 결과 불명만 재전송 대기 상태로 남기고 인증 실패는 공용 인증 경계에 넘긴다.
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

        if (failure.Kind == PurchaseFailureKind.AuthenticationRequired)
        {
            OnAuthenticationRequiredEvent.Invoke(productCode);
            return;
        }

        OnPurchaseFailedEvent.Invoke(productCode, failure.Message);
    }
}
