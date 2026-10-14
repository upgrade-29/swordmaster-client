using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// 전투 API 요청을 만들고, 주고받는 요청/응답 DTO를 정의한다. 만든 요청은 HttpRequest.Request로 보낸다
// 게임 데이터로 계산하는 요청은 클라의 GameDB.version을 X-Game-Data-Version 헤더에 담는다. 서버와 다르면 거절된다
public static class BattleApi
{
    public const string GameDataVersionHeader = "X-Game-Data-Version"; // 게임 데이터 버전을 서버에 전달하는 헤더 이름

    public class BattleStartRequest
    {
        public readonly int stage;

        [JsonConstructor]
        public BattleStartRequest(int stage)
        {
            this.stage = stage;
        }
    }

    public class BattleAbandonRequest
    {
        public readonly string battleId;

        [JsonConstructor]
        public BattleAbandonRequest(string battleId)
        {
            this.battleId = battleId;
        }
    }

    // 세부스테이지 하나가 끝났을 때 클라가 서버에 보내는 요약. 서버는 같은 시드로 다시 돌려서 이 값과 비교한다
    // 공격 이벤트 전체는 보내지 않는다. 시드와 스탯이 같으면 서버가 똑같이 재현할 수 있다
    // 이 결과를 계산한 GameDB.version은 본문이 아니라 X-Game-Data-Version 헤더로 보낸다
    public class BattleResultVerifyRequest
    {
        public readonly string battleId;
        public readonly int subStageIndex;
        public readonly BattleSubStageResultEnum resultEnum;
        public readonly double playerHp; // 끝났을 때의 체력. 처치 회복은 더하기 전
        public readonly double enemyHp;
        public readonly double duration;
        public readonly int playerAttackCount;
        public readonly int enemyAttackCount;

        [JsonConstructor]
        public BattleResultVerifyRequest(string battleId, int subStageIndex, BattleSubStageResultEnum resultEnum,
            double playerHp, double enemyHp, double duration, int playerAttackCount, int enemyAttackCount)
        {
            this.battleId = battleId;
            this.subStageIndex = subStageIndex;
            this.resultEnum = resultEnum;
            this.playerHp = playerHp;
            this.enemyHp = enemyHp;
            this.duration = duration;
            this.playerAttackCount = playerAttackCount;
            this.enemyAttackCount = enemyAttackCount;
        }

        public static BattleResultVerifyRequest From(string battleId, BattleSubStage subStage)
        {
            return new BattleResultVerifyRequest(battleId, subStage.SubStageIndex, subStage.ResultEnum,
                subStage.PlayerHp, subStage.EnemyHp, subStage.Duration, subStage.PlayerAttackCount,
                subStage.EnemyAttackCount);
        }
    }

    // 전투 시작 응답이자 재접속 응답. 서버의 배틀 세션 상태를 그대로 내려준다
    // 클라는 subStageIndex 세부스테이지를 처음부터 진행한다. 재접속이면 끊긴 세부스테이지를 처음부터 다시 하고,
    // 시드가 같으므로 결과도 같다
    public class BattleSessionInfoResponse
    {
        public readonly string battleId;
        public readonly int stage;
        public readonly ulong battleSeed;
        public readonly CombatStat playerStat; // 서버가 계산한 값. 클라도 검증도 이 값으로 계산해야 결과가 같다
        public readonly int subStageIndex; // 지금 진행할 세부스테이지 (0부터)
        public readonly double playerHp; // 이 세부스테이지를 시작할 때의 체력
        public readonly long accumulatedGold; // 지금까지 처치해서 모은 골드. 전투가 끝날 때 한 번에 반영된다
        public readonly DateTime serverTime; // UTC

        [JsonConstructor]
        public BattleSessionInfoResponse(string battleId, int stage, ulong battleSeed, CombatStat playerStat,
            int subStageIndex, double playerHp, long accumulatedGold, DateTime serverTime)
        {
            this.battleId = battleId;
            this.stage = stage;
            this.battleSeed = battleSeed;
            this.playerStat = playerStat;
            this.subStageIndex = subStageIndex;
            this.playerHp = playerHp;
            this.accumulatedGold = accumulatedGold;
            this.serverTime = serverTime;
        }
    }

    // 서버가 세부스테이지 보고를 검증한 뒤의 응답. 보고가 서버 계산과 다르면 응답 대신 에러가 오고 세션은 끝난다
    // battleEnd가 null이면 다음 세부스테이지를 진행하고, 있으면 전투가 끝난 것이다
    public class BattleResultVerifyResponse
    {
        public readonly int subStageIndex;
        public readonly BattleSubStageResultEnum resultEnum;
        public readonly int nextSubStageIndex;
        public readonly double nextPlayerHp; // 처치 회복까지 더한 다음 세부스테이지 시작 체력
        public readonly long accumulatedGold; // 이번 처치까지 모은 골드
        public readonly BattleEndResponse battleEnd;

        [JsonIgnore] public bool IsBattleEnded => battleEnd != null;

        [JsonConstructor]
        public BattleResultVerifyResponse(int subStageIndex, BattleSubStageResultEnum resultEnum,
            int nextSubStageIndex, double nextPlayerHp, long accumulatedGold, BattleEndResponse battleEnd)
        {
            this.subStageIndex = subStageIndex;
            this.resultEnum = resultEnum;
            this.nextSubStageIndex = nextSubStageIndex;
            this.nextPlayerHp = nextPlayerHp;
            this.accumulatedGold = accumulatedGold;
            this.battleEnd = battleEnd;
        }
    }

    // 전투가 끝났을 때(마지막 세부스테이지 승리, 패배, 포기) 세션에 모아 둔 보상을 한 번에 반영한 결과
    // 서버는 이 시점에 세션을 지운다
    public class BattleEndResponse
    {
        public readonly int stage;
        public readonly bool isVictory;
        public readonly BattleRewardsResponse rewards; // 패배해도 그때까지 처치한 적의 골드는 준다

        // 보상까지 반영한 뒤의 유저 상태. 클라는 덮어쓰기만 한다
        public readonly UserCurrencies currencies; // 보상까지 반영한 전체 재화 {gold, diamond}
        public readonly StageProgress stageProgress;
        public readonly IReadOnlyList<ArtifactResponse> changedArtifacts; // 드랍으로 새로 생기거나 바뀐 아티팩트

        public readonly DateTime serverTime; // UTC

        [JsonConstructor]
        public BattleEndResponse(int stage, bool isVictory, BattleRewardsResponse rewards,
            UserCurrencies currencies, StageProgress stageProgress,
            IReadOnlyList<ArtifactResponse> changedArtifacts, DateTime serverTime)
        {
            this.stage = stage;
            this.isVictory = isVictory;
            this.rewards = rewards;
            this.currencies = currencies;
            this.stageProgress = stageProgress;
            this.changedArtifacts = changedArtifacts ?? new List<ArtifactResponse>();
            this.serverTime = serverTime;
        }

        // 서버가 확정한 값으로 클라 유저를 덮어쓴다. 이미 반영된 유저에 다시 해도 결과는 같다
        public void ApplyTo(User user)
        {
            if (currencies != null)
                user.Currencies.SyncFrom(currencies);

            if (stageProgress != null)
                user.StageProgress.SetCleared(stageProgress.ClearedStage);

            // 전투로는 새 아티팩트를 얻거나, 가진 아티팩트의 재료 개수가 늘어나는 것만 바뀐다
            foreach (ArtifactResponse changed in changedArtifacts)
            {
                UserArtifact owned = user.GetArtifact(changed.code);
                if (owned == null)
                    user.AddArtifact(changed.ToUserArtifact());
                else
                    owned.AddMaterial(changed.materialCount - owned.MaterialCount);
            }
        }
    }

    public class BattleRewardsResponse
    {
        public readonly long gold;
        public readonly IReadOnlyList<string> artifactCodes; // 드랍한 아티팩트. 이미 있으면 재료로 들어감

        [JsonConstructor]
        public BattleRewardsResponse(long gold, IReadOnlyList<string> artifactCodes)
        {
            this.gold = gold;
            this.artifactCodes = artifactCodes ?? new List<string>();
        }
    }

    // 서버가 내려주는 보유 아티팩트 한 종류. 같은 아티팩트를 또 얻으면 재료 개수가 늘어난다
    public class ArtifactResponse
    {
        public readonly string code;
        public readonly int level;
        public readonly int materialCount;
        public readonly int? equipSlot; // 장착하지 않았으면 null

        [JsonConstructor]
        public ArtifactResponse(string code, int level, int materialCount, int? equipSlot)
        {
            this.code = code;
            this.level = level;
            this.materialCount = materialCount;
            this.equipSlot = equipSlot;
        }

        public UserArtifact ToUserArtifact()
        {
            return new UserArtifact(code, level, materialCount, equipSlot);
        }
    }

    // GET api/battle/session 요청. 진행 중인 세션이 없으면 서버가 빈 본문을 주고, 콜백의 result는 null이다
    public static HttpRequestInfo<BattleSessionInfoResponse> GetActiveSession(string gameDataVersion)
    {
        return HttpRequestFactory.Get<BattleSessionInfoResponse>("api/battle/session",
            headers: CreateVersionHeaders(gameDataVersion));
    }

    // POST api/battles 요청
    public static HttpRequestInfo<BattleSessionInfoResponse> Start(int stage, string gameDataVersion)
    {
        return HttpRequestFactory.Post<BattleSessionInfoResponse>("api/battles", new BattleStartRequest(stage),
            CreateVersionHeaders(gameDataVersion));
    }

    // POST api/battle/report 요청
    public static HttpRequestInfo<BattleResultVerifyResponse> Report(BattleResultVerifyRequest request,
        string gameDataVersion)
    {
        return HttpRequestFactory.Post<BattleResultVerifyResponse>("api/battle/report", request,
            CreateVersionHeaders(gameDataVersion));
    }

    // POST api/battle/abandon 요청
    public static HttpRequestInfo<BattleEndResponse> Abandon(string battleId)
    {
        return HttpRequestFactory.Post<BattleEndResponse>("api/battle/abandon", new BattleAbandonRequest(battleId));
    }

    private static Dictionary<string, string> CreateVersionHeaders(string gameDataVersion)
    {
        return new Dictionary<string, string> { { GameDataVersionHeader, gameDataVersion } };
    }
}
