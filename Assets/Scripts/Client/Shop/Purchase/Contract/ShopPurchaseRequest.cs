using System;

// 동일 거래 재전송에 사용하는 요청 ID와 상품 코드를 전송 경계에 전달한다.
public class ShopPurchaseRequest
{
    public Guid RequestId { get; }
    public string ProductCode { get; }

    public ShopPurchaseRequest(Guid requestId, string productCode)
    {
        RequestId = requestId;
        ProductCode = productCode;
    }
}
