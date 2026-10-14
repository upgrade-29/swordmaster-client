using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

// EditMode와 개발 확인에서만 서버 응답을 흉내 내는 테스트 대역이다.
public class ShopLocalService : IShopPurchaseService
{
    private const string GoldGachaOneProductCode = "GOLD_GACHA_1";
    private const string GoldGachaTenProductCode = "GOLD_GACHA_10";
    private const long GoldGachaOnePrice = 100;
    private const long GoldGachaTenPrice = 1000;
    private const long LocalGachaGoldReward = 100;

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
        bool isLocalGachaProduct = TryGetLocalGachaRollCount(request.ProductCode, out int gachaRollCount,
            out long gachaPrice);
        if (product == null && isLocalGachaProduct == false)
        {
            onFailure(new PurchaseFailure(PurchaseFailureKind.ProductNotFound, "존재하지 않는 상품입니다."));
            return;
        }

        CurrencyType priceType = product != null ? product.priceType : CurrencyType.Diamond;
        long price = product != null ? product.price : gachaPrice;
        if (User.Currencies.Get(priceType) < price)
        {
            onFailure(new PurchaseFailure(PurchaseFailureKind.InsufficientCurrency, GetNotEnoughMessage(priceType)));
            return;
        }

        User.Currencies.Add(priceType, -price);

        var rewards = new List<PurchaseRewardResult>();
        var changedArtifacts = new Dictionary<string, ArtifactResponse>();
        if (isLocalGachaProduct == true)
        {
            GiveLocalGachaRewards(gachaRollCount, rewards);
        }
        else
        {
            foreach (ShopProductRewardData reward in GameDB.shopProductRewards.Where(x => x.productCode == request.ProductCode))
            {
                ArtifactResponse changedArtifact = GiveReward(reward);
                if (changedArtifact != null)
                {
                    changedArtifacts[changedArtifact.code] = changedArtifact;
                }

                rewards.Add(new PurchaseRewardResult(reward.rewardType, reward.rewardCode, reward.amount));
            }
        }

        TestLobbyDataLoader.Instance.NotifyChangeCurrencies();

        var result = new PurchaseResult(rewards, User.Currencies, changedArtifacts.Values.ToList());
        succeededRequests[request.RequestId.ToString()] = CopyAsResponse(result);
        onSuccess(CopyAsResponse(result));
    }

    // 테스트 대역에서 서버가 확정한 결과를 만들고, 유물은 최종 스냅샷으로 응답에 보존한다.
    private ArtifactResponse GiveReward(ShopProductRewardData reward)
    {
        if (reward.rewardType == RewardType.Gold)
        {
            User.Currencies.Add(CurrencyType.Gold, reward.amount);
            return null;
        }

        if (reward.rewardType != RewardType.Artifact)
        {
            throw new NotSupportedException($"{reward.rewardType.ToString()} reward is not supported yet.");
        }

        if (string.IsNullOrEmpty(reward.rewardCode))
        {
            throw new InvalidOperationException("ARTIFACT 보상에는 rewardCode가 필요합니다.");
        }

        int amount = checked((int)reward.amount);
        UserArtifact ownedArtifact = User.GetArtifact(reward.rewardCode);
        if (ownedArtifact == null)
        {
            var createdArtifact = new UserArtifact(reward.rewardCode, 1, amount, null);
            User.AddArtifact(createdArtifact);
            return new ArtifactResponse(createdArtifact.ArtifactCode, createdArtifact.Level,
                createdArtifact.MaterialCount, createdArtifact.EquippedSlot);
        }

        ownedArtifact.SyncFromServerSnapshot(ownedArtifact.Level,
            checked(ownedArtifact.MaterialCount + amount), ownedArtifact.EquippedSlot);
        return new ArtifactResponse(ownedArtifact.ArtifactCode, ownedArtifact.Level,
            ownedArtifact.MaterialCount, ownedArtifact.EquippedSlot);
    }

    // 실제 확률을 구현하지 않고 가챠 UI와 순차 결과 처리만 확인할 수 있는 고정 결과를 만든다.
    private void GiveLocalGachaRewards(int rollCount, List<PurchaseRewardResult> rewards)
    {
        for (int i = 0; i < rollCount; i++)
        {
            User.Currencies.Add(CurrencyType.Gold, LocalGachaGoldReward);
            rewards.Add(new PurchaseRewardResult(RewardType.Gold, null, LocalGachaGoldReward));
        }
    }

    // 상품 시트 반영 전에도 확정된 1회/10회 상품 코드로 테스트 대역을 실행할 수 있게 한다.
    private static bool TryGetLocalGachaRollCount(string productCode, out int rollCount, out long price)
    {
        if (productCode == GoldGachaOneProductCode)
        {
            rollCount = 1;
            price = GoldGachaOnePrice;
            return true;
        }

        if (productCode == GoldGachaTenProductCode)
        {
            rollCount = 10;
            price = GoldGachaTenPrice;
            return true;
        }

        rollCount = 0;
        price = 0;
        return false;
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
