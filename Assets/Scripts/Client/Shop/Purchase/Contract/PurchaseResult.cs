using System.Collections.Generic;

// 서버가 확정한 보상 목록과 최종 재화를 구매 흐름에 전달한다.
public class PurchaseResult
{
    public IReadOnlyList<PurchaseRewardResult> Rewards { get; }
    public UserCurrencies Currencies { get; }

    public PurchaseResult(IReadOnlyList<PurchaseRewardResult> rewards, UserCurrencies currencies)
    {
        Rewards = rewards ?? new List<PurchaseRewardResult>();
        Currencies = currencies;
    }
}
