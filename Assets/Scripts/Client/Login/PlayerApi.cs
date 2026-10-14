using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// 로그인한 계정의 게임 플레이어 요청을 만든다. 만든 요청은 HttpRequest.Request로 보낸다
// 서버는 회원가입 때 계정만 만들고 게임 플레이어는 만들지 않는다. 플레이어는 닉네임을 받아 따로 만든다
public static class PlayerApi
{
    public const int PlayerNotFoundStatusCode = 404;

    public class CreateRequest
    {
        public readonly string nickname;

        [JsonConstructor]
        public CreateRequest(string nickname)
        {
            this.nickname = nickname;
        }
    }

    // GET api/players/me, POST api/players의 응답. 로그인한 계정의 게임 플레이어 정보
    public class PlayerResponse
    {
        public readonly long userId;
        public readonly string nickname;
        public readonly SwordResponse swordResponse;
        public readonly UserCurrencies currenciesResponse;
        public readonly IReadOnlyList<ArtifactResponse> artifactResponses;
        public readonly StageProgress stageProgress;

        [JsonConstructor]
        public PlayerResponse(long userId, string nickname, SwordResponse swordResponse,
            UserCurrencies currenciesResponse, IReadOnlyList<ArtifactResponse> artifactResponses,
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
                new UserCurrencies(currenciesResponse.Gold, currenciesResponse.Diamond),
                new UserSword(swordResponse.level),
                artifactResponses.Select(artifact => artifact.ToUserArtifact()).ToList(),
                new StageProgress(stageProgress.ClearedStage));
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

    // 처음 로그인한 계정은 플레이어가 없어서 404를 받는다. 정상 흐름이므로 실패 로그를 남기지 않는다
    public static HttpRequestInfo<PlayerResponse> GetMe()
    {
        return HttpRequestFactory.Get<PlayerResponse>("api/players/me", silentStatusCodes: new[] { PlayerNotFoundStatusCode });
    }

    public static HttpRequestInfo<PlayerResponse> Create(string nickname)
    {
        return HttpRequestFactory.Post<PlayerResponse>("api/players", new CreateRequest(nickname));
    }
}
