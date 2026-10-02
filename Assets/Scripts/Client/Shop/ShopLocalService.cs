using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

// 서버가 없기 때문에 임시 코드. User는 서버 DB에 있는 유저 역할이며 구매 결과가 이 객체에 반영된다
public class ShopLocalService : IShopPurchaseService
{
    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

    private readonly Dictionary<string, PurchaseResult> succeededRequests = new Dictionary<string, PurchaseResult>();

    public async void RequestPurchase(string productCode, string purchaseRequestId,
        Action<PurchaseResult> onSuccess, Action<PurchaseFailure> onFailure)
    {
        await Task.Yield();

        if (succeededRequests.TryGetValue(purchaseRequestId, out PurchaseResult succeededResult))
        {
            onSuccess(CopyAsResponse(succeededResult));
            return;
        }

        ShopProductData product = GameDB.shopProducts.FirstOrDefault(x => x.productCode == productCode);
        if (product == null)
        {
            onFailure(new PurchaseFailure(PurchaseFailureKind.FailedKnown, "존재하지 않는 상품입니다."));
            return;
        }

        if (User.Currencies.Get(product.priceType) < product.price)
        {
            onFailure(new PurchaseFailure(PurchaseFailureKind.FailedKnown, GetNotEnoughMessage(product.priceType)));
            return;
        }

        User.Currencies.Add(product.priceType, -product.price);

        var rewards = new List<PurchaseRewardResult>();
        foreach (var reward in GameDB.shopProductRewards.Where(x => x.productCode == productCode))
        {
            GiveReward(reward);
            rewards.Add(new PurchaseRewardResult(reward.rewardType, reward.rewardCode, reward.amount));
        }

        var result = new PurchaseResult(productCode, purchaseRequestId, rewards, User.Currencies);
        succeededRequests[purchaseRequestId] = CopyAsResponse(result);
        onSuccess(CopyAsResponse(result));
    }

    private void GiveReward(ShopProductRewardData reward)
    {
        switch (reward.rewardType)
        {
            case RewardType.Gold:
                User.Currencies.Add(CurrencyType.Gold, reward.amount);
                return;
            case RewardType.Artifact:
                GiveArtifact(reward.rewardCode, (int)reward.amount);
                return;
        }

        throw new NotSupportedException($"{reward.rewardType.ToString()} reward is not supported yet.");
    }

    // 처음 얻으면 1개는 1레벨로 보유하고 나머지는 재료로 쌓는다
    private void GiveArtifact(string artifactCode, int amount)
    {
        UserArtifact artifact = User.GetArtifact(artifactCode);
        if (artifact == null)
        {
            User.AddArtifact(new UserArtifact(artifactCode, 1, amount - 1, null));
            return;
        }

        artifact.AddMaterial(amount);
    }

    private static string GetNotEnoughMessage(CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case CurrencyType.Gold:
                return "골드가 부족합니다.";
            case CurrencyType.Diamond:
                return "다이아가 부족합니다.";
        }
        return "재화가 부족합니다.";
    }

    // 서버 응답처럼 JSON을 거쳐서 넘긴다. 서버 역할의 User와 객체를 공유하지 않게 된다
    private static T CopyAsResponse<T>(T result)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(result));
    }
}
