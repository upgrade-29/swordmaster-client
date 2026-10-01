using System.Collections.Generic;
using Newtonsoft.Json;

// POST /api/shop/purchase 성공 응답. 정확한 필드명은 서버 계약 확정 전까지 가정값이다.
public class PurchaseResult
{
    public readonly string productCode;
    public readonly string transactionId;
    public readonly IReadOnlyList<PurchaseRewardResult> rewards;
    public readonly UserCurrencies currencies;

    [JsonConstructor]
    public PurchaseResult(string productCode, string transactionId,
        IReadOnlyList<PurchaseRewardResult> rewards, UserCurrencies currencies)
    {
        this.productCode = productCode;
        this.transactionId = transactionId;
        this.rewards = rewards ?? new List<PurchaseRewardResult>();
        this.currencies = currencies;
    }
}
