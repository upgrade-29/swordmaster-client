using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// GET api/players/me, POST api/players 응답. 로그인한 계정의 게임 플레이어 정보
public class PlayerResponse
{
    public readonly long userId;
    public readonly string nickname;
    public readonly SwordResponse swordResponse;
    public readonly CurrenciesResponse currenciesResponse;
    public readonly IReadOnlyList<ArtifactResponse> artifactResponses;
    public readonly StageProgress stageProgress;

    [JsonConstructor]
    public PlayerResponse(long userId, string nickname, SwordResponse swordResponse,
        CurrenciesResponse currenciesResponse, IReadOnlyList<ArtifactResponse> artifactResponses,
        StageProgress stageProgress)
    {
        this.userId = userId;
        this.nickname = nickname;
        this.swordResponse = swordResponse;
        this.currenciesResponse = currenciesResponse;
        this.artifactResponses = artifactResponses ?? new List<ArtifactResponse>();
        this.stageProgress = stageProgress ?? new StageProgress(0);
    }

    // 로비와 전투가 쓰는 클라 유저 모델로 바꾼다
    public User ToUser()
    {
        return new User(userId, nickname,
            new UserCurrencies(currenciesResponse.gold, currenciesResponse.diamond),
            new UserSword(swordResponse.level),
            artifactResponses.Select(artifact => artifact.ToUserArtifact()).ToList(),
            new StageProgress(stageProgress.ClearedStage));
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
    }
}
