using System;

// IShopPurchaseService의 서버 구현. ShopApi로 요청을 만들어 HttpRequest로 보낸다
public class HttpShopPurchaseService : IShopPurchaseService
{
    public void RequestPurchase(ShopPurchaseRequest request, Action<PurchaseResult> onSuccess,
        Action<PurchaseFailure> onFailure)
    {
        HttpRequest.Request(ShopApi.Purchase(request.RequestId, request.ProductCode),
            (error, response) => OnPurchaseResponse(error, response, onSuccess, onFailure));
    }

    private static void OnPurchaseResponse(HttpError error, ShopApi.PurchaseResponse response,
        Action<PurchaseResult> onSuccess, Action<PurchaseFailure> onFailure)
    {
        if (error != null)
        {
            onFailure(ShopPurchaseFailureMapper.FromHttpError(error));
            return;
        }

        onSuccess(response.ToPurchaseResult());
    }
}
