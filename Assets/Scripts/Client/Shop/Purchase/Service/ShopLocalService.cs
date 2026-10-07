using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

// EditMode와 개발 확인에서만 서버 응답을 흉내 내는 테스트 대역이다.
public class ShopLocalService : IShopPurchaseService
{
    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

    private readonly Dictionary<string, PurchaseResult> succeededRequests = new Dictionary<string, PurchaseResult>();

    // 테스트 데이터로 구매 결과를 만들고 동일 요청 ID에는 저장된 결과를 다시 전달한다.
    public async void RequestPurchase(ShopPurchaseRequest request, Action<PurchaseResult> onSuccess,
        Action<PurchaseFailure> onFailure)
    {
        await Task.Yield();

        if (succeededRequests.TryGetValue(request.RequestId.ToString(), out PurchaseResult succeededResult))
        {
            onSuccess(CopyAsResponse(succeededResult));
            return;
        }

        ShopProductData product = GameDB.shopProducts.FirstOrDefault(x => x.productCode == request.ProductCode);
        if (product == null)
        {
            onFailure(new PurchaseFailure(PurchaseFailureKind.ProductNotFound, "존재하지 않는 상품입니다."));
            return;
        }

        if (User.Currencies.Get(product.priceType) < product.price)
        {
            onFailure(new PurchaseFailure(PurchaseFailureKind.InsufficientCurrency, GetNotEnoughMessage(product.priceType)));
            return;
        }

        User.Currencies.Add(product.priceType, -product.price);

        var rewards = new List<PurchaseRewardResult>();
        foreach (var reward in GameDB.shopProductRewards.Where(x => x.productCode == request.ProductCode))
        {
            GiveReward(reward);
            rewards.Add(new PurchaseRewardResult(reward.rewardType, reward.rewardCode, reward.amount));
        }

        TestLobbyDataLoader.Instance.NotifyChangeCurrencies();

        var result = new PurchaseResult(rewards, User.Currencies);
        succeededRequests[request.RequestId.ToString()] = CopyAsResponse(result);
        onSuccess(CopyAsResponse(result));
    }

    // 현재 테스트 대역이 지원하는 보상만 사용자 재화에 반영한다.
    private void GiveReward(ShopProductRewardData reward)
    {
        if (reward.rewardType != RewardType.Gold)
        {
            throw new NotSupportedException($"{reward.rewardType.ToString()} reward is not supported yet.");
        }

        User.Currencies.Add(CurrencyType.Gold, reward.amount);
    }

    private static string GetNotEnoughMessage(CurrencyType currencyType)
    {
        return currencyType == CurrencyType.Gold ? "골드가 부족합니다." : "다이아가 부족합니다.";
    }

    // 서버 응답처럼 JSON을 거쳐서 넘긴다. 서버 역할의 User와 객체를 공유하지 않게 된다
    private static T CopyAsResponse<T>(T result)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(result));
    }
}
