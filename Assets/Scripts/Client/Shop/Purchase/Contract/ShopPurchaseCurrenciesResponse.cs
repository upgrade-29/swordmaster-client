// 서버 구매 응답의 재화 잔액을 전송 형식으로 보관한다.
public class ShopPurchaseCurrenciesResponse
{
    public long Gold { get; }
    public int Diamond { get; }

    public ShopPurchaseCurrenciesResponse(long gold, int diamond)
    {
        Gold = gold;
        Diamond = diamond;
    }

    // 서버의 정수 재화 값을 기존 사용자 재화 모델로 변환한다.
    public UserCurrencies ToUserCurrencies()
    {
        return new UserCurrencies(Gold, Diamond);
    }
}
