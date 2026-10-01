using System;
using System.Collections.Generic;

// 실제 서버 없이 IShopPurchaseService의 정상/재화부족/인증실패/Timeout(ResultUnknown)/동일 purchaseRequestId 재전송
// 시나리오를 시뮬레이션하는 테스트 전용 Fake. asmdef 미도입 상태라 Unity Test Runner가 자동 수집하는
// 정식 단위 테스트는 아니며, Coordinator/Controller 동작을 수동(Play Mode 부트스트랩) 또는 향후 테스트 코드에서
// 재사용하기 위한 더블이다.
public class FakeShopPurchaseService : IShopPurchaseService
{
    public enum Scenario
    {
        Success,            // 구매 성공. NextRewards/NextCurrencies를 그대로 응답에 담아 onSuccess로 반환한다.
        InsufficientFunds,  // 재화 부족으로 서버가 거부한 상황(FailedKnown). 재시도 없이 즉시 실패 처리된다.
        AuthFailure,        // 인증 실패로 서버가 거부한 상황(FailedKnown). 재시도 없이 즉시 실패 처리된다.
        Timeout,            // 응답 지연/연결 끊김 등으로 서버 처리 여부를 알 수 없는 상황(ResultUnknown). Reconciling 상태로 전환되어 재시도 흐름을 탄다.
    }

    // 다음 RequestPurchase 호출(캐시되지 않은 새 요청에 한함)에 적용할 시나리오.
    public Scenario NextScenario { get; set; } = Scenario.Success;

    // 성공 시나리오에서 응답에 담을 최종 재화. 필요하면 테스트에서 미리 설정한다.
    public UserCurrencies NextCurrencies { get; set; } = new UserCurrencies(0, 0);

    // 성공 시나리오에서 응답에 담을 지급 보상 목록.
    public List<PurchaseRewardResult> NextRewards { get; set; } = new List<PurchaseRewardResult>();

    // 서버 Idempotency 시뮬레이션: 같은 purchaseRequestId로 이미 성공한 요청은 재처리하지 않고 같은 결과를 그대로 반환한다.
    // ResultUnknown/FailedKnown은 캐시하지 않는다 — 재시도 시 서버가 실제로 확정한 결과를 새로 알려주는 흐름을 재현하기 위함이다.
    private readonly Dictionary<string, PurchaseResult> succeededRequests = new Dictionary<string, PurchaseResult>();

    public void RequestPurchase(string productCode, string purchaseRequestId,
        Action<PurchaseResult> onSuccess, Action<PurchaseFailure> onFailure)
    {
        if (succeededRequests.TryGetValue(purchaseRequestId, out PurchaseResult cachedResult))
        {
            onSuccess(cachedResult);
            return;
        }

        switch (NextScenario)
        {
            case Scenario.Success:
                PurchaseResult result = new PurchaseResult(productCode, purchaseRequestId, NextRewards, NextCurrencies);
                succeededRequests[purchaseRequestId] = result;
                onSuccess(result);
                break;

            case Scenario.InsufficientFunds:
                onFailure(new PurchaseFailure(PurchaseFailureKind.FailedKnown, "재화가 부족합니다."));
                break;

            case Scenario.AuthFailure:
                onFailure(new PurchaseFailure(PurchaseFailureKind.FailedKnown, "인증에 실패했습니다."));
                break;

            case Scenario.Timeout:
                onFailure(new PurchaseFailure(PurchaseFailureKind.ResultUnknown, "요청이 시간 초과되었습니다."));
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
