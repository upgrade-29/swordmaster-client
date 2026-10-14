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
        public readonly IReadOnlyList<ArtifactResponse> changedArtifacts;

        [JsonConstructor]
        public PurchaseResponse(IReadOnlyList<RewardResponse> rewards, CurrenciesResponse currencies,
            IReadOnlyList<ArtifactResponse> changedArtifacts = null)
        {
            this.rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            this.currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
            this.changedArtifacts = changedArtifacts;
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
    }

    public class CurrenciesResponse
    {
        public readonly long gold;
        public readonly long diamond;

        [JsonConstructor]
        public CurrenciesResponse(long gold, long diamond)
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
