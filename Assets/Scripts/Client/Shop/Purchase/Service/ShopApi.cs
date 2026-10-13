using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// 상점 구매 요청을 만든다. 만든 요청은 HttpRequest.Request로 보낸다
public static class ShopApi
{
    public class PurchaseRequest
    {
        public readonly Guid requestId;
        public readonly string productCode;

        [JsonConstructor]
        public PurchaseRequest(Guid requestId, string productCode)
        {
            this.requestId = requestId;
            this.productCode = productCode;
        }
    }

    public class PurchaseResponse
    {
        public readonly IReadOnlyList<RewardResponse> rewards;
        public readonly CurrenciesResponse currencies;

        [JsonConstructor]
        public PurchaseResponse(IReadOnlyList<RewardResponse> rewards, CurrenciesResponse currencies)
        {
            this.rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            this.currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
        }

        // 응답의 전체 보상과 서버가 확정한 재화를 기능 모델로 보존한다.
        public PurchaseResult ToPurchaseResult()
        {
            var listReward = new List<PurchaseRewardResult>(rewards.Count);
            foreach (RewardResponse reward in rewards)
            {
                listReward.Add(reward.ToPurchaseRewardResult());
            }

            return new PurchaseResult(listReward, currencies.ToUserCurrencies());
        }
    }

    public class RewardResponse
    {
        public readonly RewardType rewardType;
        public readonly string rewardCode;
        public readonly long amount;

        [JsonConstructor]
        public RewardResponse(RewardType rewardType, string rewardCode, long amount)
        {
            if (rewardCode == null && rewardType != RewardType.Gold)
            {
                throw new ArgumentException("GOLD 이외 보상의 rewardCode는 null일 수 없습니다.", nameof(rewardCode));
            }

            this.rewardType = rewardType;
            this.rewardCode = rewardCode;
            this.amount = amount;
        }

        // 서버 보상 1건을 화면과 상태가 사용하는 결과 모델로 변환한다.
        public PurchaseRewardResult ToPurchaseRewardResult()
        {
            return new PurchaseRewardResult(rewardType, rewardCode, amount);
        }
    }

    public class CurrenciesResponse
    {
        public readonly long gold;
        public readonly int diamond;

        [JsonConstructor]
        public CurrenciesResponse(long gold, int diamond)
        {
            this.gold = gold;
            this.diamond = diamond;
        }

        // 서버의 정수 재화 값을 기존 사용자 재화 모델로 변환한다.
        public UserCurrencies ToUserCurrencies()
        {
            return new UserCurrencies(gold, diamond);
        }
    }

    // 같은 requestId로 다시 보내면 서버는 다시 처리하지 않고 처음 응답을 돌려준다
    public static HttpRequestInfo<PurchaseResponse> Purchase(Guid requestId, string productCode)
    {
        return HttpRequestFactory.Post<PurchaseResponse>("api/shop/purchase", new PurchaseRequest(requestId, productCode));
    }
}
