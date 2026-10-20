using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// 랭킹 조회 요청을 만든다. 만든 요청은 HttpRequest.Request로 보낸다
public static class RankingApi
{
    // 랭킹 한 줄. 상위 목록의 항목과 내 기록이 같은 모양이다
    public class RankingEntryResponse
    {
        public readonly int rank;
        public readonly string nickname;
        public readonly long score;

        [JsonConstructor]
        public RankingEntryResponse(int rank, string nickname, long score)
        {
            this.rank = rank;
            this.nickname = nickname;
            this.score = score;
        }
    }

    // GET api/rankings의 응답
    // 상위 목록과 인원 수는 집계본 기준이고, 내 기록만 실시간 기준이라 서로 어긋날 수 있다
    public class RankingResponse
    {
        public readonly IReadOnlyList<RankingEntryResponse> rankings; // 상위 목록. 1등부터 순서대로
        public readonly RankingEntryResponse me;                      // 내 기록
        public readonly bool rankChanged;                             // 내 기록이 집계본과 달라 me.rank가 예상 순위이면 true. 집계에 아직 없을 때도 true
        public readonly long total;                                   // 인원 수
        public readonly DateTime? updatedAt;                          // 상위 목록 집계 시각. UTC. 집계 전이면 null
        public readonly DateTime nextUpdateAt;                        // 다음 집계 시각. UTC
        public readonly DateTime serverTime;                          // 응답을 만든 서버 시각. UTC. 기기 시계 대신 남은 시간 계산에 쓴다

        [JsonConstructor]
        public RankingResponse(IReadOnlyList<RankingEntryResponse> rankings, RankingEntryResponse me, bool rankChanged,
            long total, DateTime? updatedAt, DateTime nextUpdateAt, DateTime serverTime)
        {
            this.rankings = rankings ?? new List<RankingEntryResponse>();
            this.me = me;
            this.rankChanged = rankChanged;
            this.total = total;
            this.updatedAt = updatedAt;
            this.nextUpdateAt = nextUpdateAt;
            this.serverTime = serverTime;
        }

        // 서버 시각 기준으로 다음 집계까지 남은 시간
        public TimeSpan GetTimeUntilNextUpdate()
        {
            return nextUpdateAt - serverTime;
        }
    }

    public static HttpRequestInfo<RankingResponse> Get(RankingType type)
    {
        var query = new Dictionary<string, object> { { "type", ToQueryValue(type) } };
        return HttpRequestFactory.Get<RankingResponse>("api/rankings", query);
    }

    // 쿼리는 enum을 직접 변환해야 하므로 서버 RankingType 이름으로 바꾼다
    private static string ToQueryValue(RankingType type)
    {
        switch (type)
        {
            case RankingType.SwordLevel:
                return "SWORD_LEVEL";
            case RankingType.StageCleared:
                return "STAGE_CLEARED";
            case RankingType.CombatPower:
                return "COMBAT_POWER";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
