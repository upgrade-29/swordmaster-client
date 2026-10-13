// 전송 계층의 실패 정보를 Shop 구매 상태가 소비하는 결과로 변환한다.
public static class ShopPurchaseFailureMapper
{
    // 서버가 응답한 실패는 상태 코드로 분류하고, 응답을 받지 못하면 서버 처리 여부를 알 수 없는 것으로 본다.
    // 응답을 읽지 못한 경우는 다시 보내도 같은 응답을 받으므로 재전송하지 않는다.
    public static PurchaseFailure FromHttpError(HttpError error)
    {
        PurchaseFailure failure = null;
        switch (error.Type)
        {
            case HttpErrorType.Server:
                failure = FromHttpStatus(error.StatusCode, error.Message);
                break;
            case HttpErrorType.Network:
                failure = ResultUnknown(error.Message);
                break;
            case HttpErrorType.Parse:
            case HttpErrorType.Client:
                failure = new PurchaseFailure(PurchaseFailureKind.ServiceUnavailable, error.Message);
                break;
        }

        return failure;
    }

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
                return new PurchaseFailure(PurchaseFailureKind.InsufficientCurrency, message);
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
}
