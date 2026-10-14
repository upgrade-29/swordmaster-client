// 전송 계층의 실패 정보를 Shop 구매 상태가 소비하는 결과로 변환한다.
public static class ShopPurchaseFailureMapper
{
    // 전송 경계가 HTTP 상태를 Shop에 노출하지 않고 구매 흐름의 실패 종류로 변환한다.
    public static PurchaseFailure FromHttpStatus(int statusCode, string message)
    {
        switch (statusCode)
        {
            case 400:
                return new PurchaseFailure(PurchaseFailureKind.BadRequest, message);
            case 401:
                return new PurchaseFailure(PurchaseFailureKind.AuthenticationRequired, message);
            case 404:
                return new PurchaseFailure(PurchaseFailureKind.ProductNotFound, message);
            case 409:
                return FromConflictMessage(message);
            case 500:
            case 503:
                return ResultUnknown(message);
            default:
                return new PurchaseFailure(PurchaseFailureKind.ServiceUnavailable, message);
        }
    }

    // 타임아웃과 응답 유실은 서버 처리 결과를 확정할 수 없어 재전송 대기 상태로 분류한다.
    public static PurchaseFailure ResultUnknown(string message)
    {
        return new PurchaseFailure(PurchaseFailureKind.ResultUnknown, message);
    }

    // 409은 재화 부족과 결과 조회 만료가 같은 상태 코드를 공유하므로 서버 message 코드로 의미를 구분한다.
    private static PurchaseFailure FromConflictMessage(string message)
    {
        if (message == "REQUEST_EXPIRED")
        {
            return new PurchaseFailure(PurchaseFailureKind.RequestExpired, message);
        }

        if (message == "NOT_ENOUGH_GOLD" || message == "NOT_ENOUGH_DIAMOND")
        {
            return new PurchaseFailure(PurchaseFailureKind.InsufficientCurrency, message);
        }

        return new PurchaseFailure(PurchaseFailureKind.ServiceUnavailable, message);
    }
}
