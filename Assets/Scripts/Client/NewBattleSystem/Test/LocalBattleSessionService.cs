using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

// 서버가 없기 때문에 임시 코드. 서버 대신 클라이언트 안에서 배틀 세션을 관리하고 세부스테이지 보고를 검증한다
// session 필드가 서버의 Redis 세션 역할이다. 유저가 하나뿐이라 세션도 하나만 둔다
public class LocalBattleSessionService : IBattleSessionService
{
    private const double MaxBattleSpeed = 2; // 배속 상한. 이보다 빨리 끝난 보고는 거절한다
    private const double TimeTolerance = 0.3; // 시간 검증 허용 오차(초). 통신 지연, 프레임 오차
    private const double ValueEpsilon = 1e-6; // 체력, 시간 비교 허용 오차. 플랫폼마다 double 계산이 아주 조금 다를 수 있다

    private readonly User user; // 서버 DB에 있는 유저 역할. 전투가 끝나면 보상이 이 객체에 반영된다
    private readonly BattleDB battleDB;
    private readonly BattleEngine engine;
    private readonly Random random; // 시드 발급용. 서버에서만 쓰므로 System.Random이어도 된다
    private readonly Func<DateTime> utcNow;

    private Session session;

    // utcNow는 테스트에서 시간을 직접 흘리고 싶을 때 넣는다. 없으면 DateTime.UtcNow
    public LocalBattleSessionService(GameDB gameDB, User user, Random random, Func<DateTime> utcNow = null)
    {
        this.user = user;
        this.random = random;
        this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        battleDB = new BattleDB(gameDB);
        engine = new BattleEngine(battleDB);
    }

    public async Task<BattleSessionInfo> GetActiveSessionAsync()
    {
        await Task.Yield();

        if (session == null)
            return null;

        // 재접속하면 클라는 지금 세부스테이지를 처음부터 다시 하므로 시간 검증도 지금부터 잰다
        session.subStageStartedAt = utcNow();
        return Clone(CreateSessionInfo());
    }

    public async Task<BattleSessionInfo> StartBattleAsync(int stageNumber)
    {
        await Task.Yield();

        if (session != null)
            throw new BattleRequestException($"Battle {session.battleId} is already in progress");

        // 이미 깬 스테이지는 다시 도전할 수 있고, 아직 열리지 않은 스테이지는 도전할 수 없다
        var nextStage = user.StageProgress.NextStage;
        if (stageNumber < 1 || stageNumber > nextStage)
            throw new BattleRequestException($"Stage {stageNumber} is locked. Next stage is {nextStage}");

        StageData stage = battleDB.GetStage(stageNumber);
        CombatStat playerStat = CalculatePlayerStat();
        session = new Session
        {
            battleId = Guid.NewGuid().ToString("N"),
            stage = stage,
            battleSeed = NextSeed(),
            playerStat = playerStat,
            subStageIndex = 0,
            playerHp = playerStat.maxHp,
            accumulatedGold = 0,
            subStageStartedAt = utcNow(),
        };

        return Clone(CreateSessionInfo());
    }

    public async Task<BattleResultVerifyResponse> ReportSubStageAsync(BattleResultVerifyRequest request)
    {
        await Task.Yield();

        ValidateBattleId(request.battleId);

        // 같은 시드와 스탯으로 다시 돌려서 클라가 보낸 요약과 비교한다
        var subStage = new BattleSubStage(engine, session.stage, session.subStageIndex, session.playerStat,
            session.playerHp, session.battleSeed);
        subStage.RunToEnd();

        string failReason = Verify(request, subStage, utcNow() - session.subStageStartedAt);
        if (failReason != null)
        {
            // 같은 세부스테이지를 계속 다시 보내며 시험해 보지 못하게, 검증에 실패하면 패배로 끝낸다
            var failedBattleId = session.battleId;
            EndBattle(false);
            throw new BattleRequestException($"Battle {failedBattleId} verification failed: {failReason}");
        }

        int verifiedIndex = session.subStageIndex;
        if (subStage.ResultEnum != BattleSubStageResultEnum.EnemyDead)
            return Clone(CreateVerifyResponse(subStage, EndBattle(false)));

        session.accumulatedGold += subStage.KillGold;
        if (verifiedIndex == BattleEngine.GetSubStageCount(session.stage) - 1)
            return Clone(CreateVerifyResponse(subStage, EndBattle(true)));

        // 체력은 다음 세부스테이지로 이어지고, 처치 회복을 더한다
        session.subStageIndex++;
        session.playerHp = subStage.PlayerHp + subStage.HealOnKill;
        session.subStageStartedAt = utcNow();
        return Clone(CreateVerifyResponse(subStage, null));
    }

    public async Task<BattleEndResult> AbandonAsync(string battleId)
    {
        await Task.Yield();

        ValidateBattleId(battleId);
        return Clone(EndBattle(false));
    }

    // 다르면 이유를, 같으면 null을 돌려준다
    private string Verify(BattleResultVerifyRequest request, BattleSubStage expected, TimeSpan elapsed)
    {
        if (request.subStageIndex != expected.SubStageIndex)
            return $"subStageIndex {request.subStageIndex} != {expected.SubStageIndex}";
        if (request.resultEnum != expected.ResultEnum)
            return $"result {request.resultEnum} != {expected.ResultEnum}";
        if (request.playerAttackCount != expected.PlayerAttackCount || request.enemyAttackCount != expected.EnemyAttackCount)
            return $"attack count {request.playerAttackCount}/{request.enemyAttackCount} != {expected.PlayerAttackCount}/{expected.EnemyAttackCount}";
        if (IsClose(request.duration, expected.Duration) == false)
            return $"duration {request.duration} != {expected.Duration}";
        if (IsClose(request.playerHp, expected.PlayerHp) == false || IsClose(request.enemyHp, expected.EnemyHp) == false)
            return $"hp {request.playerHp}/{request.enemyHp} != {expected.PlayerHp}/{expected.EnemyHp}";

        // 최대 배속으로 진행해도 이 시간보다 빨리 끝날 수는 없다
        var minSeconds = expected.Duration / MaxBattleSpeed - TimeTolerance;
        if (elapsed.TotalSeconds < minSeconds)
            return $"too fast {elapsed.TotalSeconds:F3}s < {minSeconds:F3}s";

        return null;
    }

    private void ValidateBattleId(string battleId)
    {
        if (session == null)
            throw new BattleRequestException("No battle in progress");
        if (session.battleId != battleId)
            throw new BattleRequestException($"Battle {battleId} is not the current battle");
    }

    // 세션에 모아 둔 보상을 한 번에 반영하고 세션을 지운다. 패배해도 그때까지 처치한 적의 골드는 준다
    private BattleEndResult EndBattle(bool isVictory)
    {
        StageProgress progress = user.StageProgress;
        user.Currencies.Add(CurrencyType.Gold, session.accumulatedGold);

        if (isVictory && session.stage.stage > progress.ClearedStage)
            progress.SetCleared(session.stage.stage);

        // TODO: 아티팩트 드랍 시점이 정해지면 여기서 드랍하고, 바뀐 아티팩트를 changedArtifacts에 담는다
        var droppedArtifactCodes = new List<string>();
        var changedArtifacts = new List<UserArtifact>();

        var result = new BattleEndResult(session.stage.stage, isVictory,
            new BattleRewards(session.accumulatedGold, droppedArtifactCodes),
            new UserCurrencies(user.Currencies.Gold, user.Currencies.Diamond),
            new StageProgress(progress.ClearedStage, progress.NextBattleAvailableAt),
            changedArtifacts, utcNow());

        session = null;
        return result;
    }

    private CombatStat CalculatePlayerStat()
    {
        SwordData sword = battleDB.GetSword(user.Sword.Level);
        IEnumerable<(ArtifactData data, int level)> equippedArtifacts = user.EquippedArtifacts
            .Select(artifact => (battleDB.GetArtifact(artifact.ArtifactCode), artifact.Level));
        return StatCalculator.CalculatePlayer(sword, equippedArtifacts);
    }

    private ulong NextSeed()
    {
        var bytes = new byte[8];
        random.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }

    private BattleSessionInfo CreateSessionInfo()
    {
        return new BattleSessionInfo(session.battleId, session.stage.stage, session.battleSeed, session.playerStat,
            session.subStageIndex, session.playerHp, session.accumulatedGold, utcNow());
    }

    // battleEnd가 있으면 세션은 이미 지워졌으므로 session을 쓰지 않는다
    private BattleResultVerifyResponse CreateVerifyResponse(BattleSubStage subStage, BattleEndResult battleEnd)
    {
        if (battleEnd != null)
            return new BattleResultVerifyResponse(subStage.SubStageIndex, subStage.ResultEnum, subStage.SubStageIndex,
                subStage.PlayerHp, battleEnd.rewards.gold, battleEnd);

        return new BattleResultVerifyResponse(subStage.SubStageIndex, subStage.ResultEnum, session.subStageIndex,
            session.playerHp, session.accumulatedGold, null);
    }

    private static bool IsClose(double a, double b)
    {
        return Math.Abs(a - b) <= ValueEpsilon;
    }

    // 서버 응답처럼 JSON을 거쳐서 넘긴다. 서버 쪽 객체와 공유하는 부분이 남지 않는다
    private static T Clone<T>(T value)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }

    // 서버의 Redis 세션에 저장되는 값
    private class Session
    {
        public string battleId;
        public StageData stage;
        public ulong battleSeed;
        public CombatStat playerStat;
        public int subStageIndex; // 지금 진행 중인 세부스테이지
        public double playerHp; // 지금 세부스테이지를 시작할 때의 체력
        public long accumulatedGold;
        public DateTime subStageStartedAt; // 시간 검증 기준. 세부스테이지 시작이나 재접속 때 갱신한다
    }
}
