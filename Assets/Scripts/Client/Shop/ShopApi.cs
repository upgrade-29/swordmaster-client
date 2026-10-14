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
        public readonly UserCurrencies currencies;
        public readonly IReadOnlyList<ArtifactResponse> changedArtifacts;

        [JsonConstructor]
        public PurchaseResponse(IReadOnlyList<RewardResponse> rewards, UserCurrencies currencies,
            IReadOnlyList<ArtifactResponse> changedArtifacts = null)
        {
            this.rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            this.currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
            this.changedArtifacts = changedArtifacts;
        }
    }

    // 같은 requestId로 다시 보내면 서버는 다시 처리하지 않고 처음 응답을 돌려준다
    public static HttpRequestInfo<PurchaseResponse> Purchase(Guid requestId, string productCode)
    {
        return HttpRequestFactory.Post<PurchaseResponse>("api/shop/purchase", new PurchaseRequest(requestId, productCode));
    }
}
