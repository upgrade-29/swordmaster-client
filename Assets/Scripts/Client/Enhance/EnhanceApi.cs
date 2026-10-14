using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// 검과 아티팩트의 강화, 판매, 장착 요청을 만든다. 만든 요청은 HttpRequest.Request로 보낸다
// 강화와 판매는 같은 requestId로 다시 보내면 서버가 다시 처리하지 않고 처음 응답을 돌려준다
public static class EnhanceApi
{
    [JsonConverter(typeof(UpperSnakeEnumConverter))]
    public enum EnhanceResultType
    {
        None = 0,
        Success,
        Destroyed,
    }

    // expectedLevel은 클라가 알고 있는 현재 검 레벨이다. 서버 레벨과 다르면 LEVEL_MISMATCH로 거절한다
    public class SwordRequest
    {
        public readonly Guid requestId;
        public readonly int expectedLevel;

        [JsonConstructor]
        public SwordRequest(Guid requestId, int expectedLevel)
        {
            this.requestId = requestId;
            this.expectedLevel = expectedLevel;
        }
    }

    public class ArtifactEnhanceRequest
    {
        public readonly Guid requestId;

        [JsonConstructor]
        public ArtifactEnhanceRequest(Guid requestId)
        {
            this.requestId = requestId;
        }
    }

    public class SwordEnhanceResponse
    {
        public readonly EnhanceResultType result;
        public readonly SwordResponse swordResponse;
        public readonly UserCurrencies currenciesResponse;

        [JsonConstructor]
        public SwordEnhanceResponse(EnhanceResultType result, SwordResponse swordResponse, UserCurrencies currenciesResponse)
        {
            this.result = result;
            this.swordResponse = swordResponse;
            this.currenciesResponse = currenciesResponse;
        }
    }

    public class SwordSellResponse
    {
        public readonly SwordResponse swordResponse;
        public readonly IReadOnlyList<PurchaseRewardResult> rewardResponses;
        public readonly UserCurrencies currenciesResponse;

        [JsonConstructor]
        public SwordSellResponse(SwordResponse swordResponse, IReadOnlyList<PurchaseRewardResult> rewardResponses,
            UserCurrencies currenciesResponse)
        {
            this.swordResponse = swordResponse;
            this.rewardResponses = rewardResponses ?? new List<PurchaseRewardResult>();
            this.currenciesResponse = currenciesResponse;
        }
    }

    // artifactResponses에는 강화한 아티팩트만 들어 있다
    public class ArtifactEnhanceResponse
    {
        public readonly IReadOnlyList<ArtifactResponse> artifactResponses;
        public readonly UserCurrencies currenciesResponse;

        [JsonConstructor]
        public ArtifactEnhanceResponse(IReadOnlyList<ArtifactResponse> artifactResponses, UserCurrencies currenciesResponse)
        {
            this.artifactResponses = artifactResponses ?? new List<ArtifactResponse>();
            this.currenciesResponse = currenciesResponse;
        }
    }

    public class SwordResponse
    {
        public readonly int level;

        [JsonConstructor]
        public SwordResponse(int level)
        {
            this.level = level;
        }
    }

    public static HttpRequestInfo<SwordEnhanceResponse> EnhanceSword(Guid requestId, int expectedLevel)
    {
        return HttpRequestFactory.Post<SwordEnhanceResponse>("api/swords/enhance", new SwordRequest(requestId, expectedLevel));
    }

    public static HttpRequestInfo<SwordSellResponse> SellSword(Guid requestId, int expectedLevel)
    {
        return HttpRequestFactory.Post<SwordSellResponse>("api/swords/sell", new SwordRequest(requestId, expectedLevel));
    }

    public static HttpRequestInfo<ArtifactEnhanceResponse> EnhanceArtifact(Guid requestId, string artifactCode)
    {
        return HttpRequestFactory.Post<ArtifactEnhanceResponse>($"api/artifacts/{Uri.EscapeDataString(artifactCode)}/enhance",
            new ArtifactEnhanceRequest(requestId));
    }

    // 장착과 해제의 응답은 장착한 아티팩트 목록이다(슬롯 순서). 아무것도 장착하지 않았으면 빈 배열이다
    // 이미 다른 슬롯에 장착한 아티팩트를 장착하면, 서버가 원래 슬롯에서 해제하고 요청한 슬롯으로 옮긴다
    public static HttpRequestInfo<List<ArtifactResponse>> EquipArtifact(string artifactCode, int slot)
    {
        return HttpRequestFactory.Put<List<ArtifactResponse>>($"api/artifacts/{Uri.EscapeDataString(artifactCode)}/slots/{slot.ToString()}");
    }

    public static HttpRequestInfo<List<ArtifactResponse>> UnequipArtifact(int slot)
    {
        return HttpRequestFactory.Delete<List<ArtifactResponse>>($"api/artifacts/slots/{slot.ToString()}");
    }
}
