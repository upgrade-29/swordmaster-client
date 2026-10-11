using System;

// 서버가 확정한 성공 결과를 사용자 재화와 활성 화면에 반영한다.
public class ShopPurchaseSuccessApplier
{
    private readonly UserCurrencies userCurrencies;
    private readonly Action<string, ShopProductPurchaseState> setItemPurchaseState;

    public ShopPurchaseSuccessApplier(UserCurrencies userCurrencies,
        Action<string, ShopProductPurchaseState> setItemPurchaseState)
    {
        this.userCurrencies = userCurrencies ?? throw new ArgumentNullException(nameof(userCurrencies));
        this.setItemPurchaseState = setItemPurchaseState ??
            throw new ArgumentNullException(nameof(setItemPurchaseState));
    }

    // 재화는 항상 서버 최종값으로 반영하고, 활성 탭에만 구매 완료 표시를 요청한다.
    public void Apply(string productCode, PurchaseResult result, bool isViewActive)
    {
        userCurrencies.SyncFrom(result.Currencies);

        if (isViewActive == false)
        {
            return;
        }

        setItemPurchaseState.Invoke(productCode, ShopProductPurchaseState.Ready);
    }
}
