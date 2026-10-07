using System;

// 세부스테이지 한 판(적 한 마리와의 전투)의 진행 상태
// 클라는 Advance로 매 프레임 조금씩 진행하고, 서버는 RunToEnd로 끝까지 한 번에 돌려서 클라가 보낸 결과와 비교한다
// 몇 번에 나눠 진행하든 공격 순서와 난수 사용 순서가 같으므로 결과는 항상 같다
public class BattleSubStage
{
    // 부동소수점 오차 때문에 같은 시각의 공격이 미세하게 어긋나는 것을 같은 시각으로 본다
    private const double TimeEpsilon = 1e-9;

    public event Action<BattleEvent> AttackApplied; // 공격 한 번이 끝날 때마다. 서버는 구독하지 않는다

    private readonly BattleEngine engine;
    private readonly SplitMix64 random; // 이 세부스테이지 전용. 바깥에서 꺼내 쓰면 클라와 서버의 결과가 달라진다
    private readonly CombatStat playerStat;
    private readonly long killGoldOnWin;

    private int playerAttackCount;
    private int enemyAttackCount;

    public int SubStageIndex { get; }
    public EnemyData Enemy { get; }
    public CombatStat EnemyStat { get; }
    public double TimeLimit { get; }
    public double PlayerStartHp { get; }

    public double Elapsed { get; private set; }
    public double PlayerHp { get; private set; }
    public double EnemyHp { get; private set; }

    // 끝난 뒤에만 의미가 있는 값
    public BattleSubStageResultEnum ResultEnum { get; private set; }
    public double Duration { get; private set; } // 끝난 시각. 처치/사망이면 마지막 공격 시각, 시간 초과면 제한 시간
    public double HealOnKill { get; private set; } // 처치하지 못했으면 0
    public long KillGold { get; private set; } // 처치하지 못했으면 0

    public bool IsFinished => ResultEnum != BattleSubStageResultEnum.None;
    public bool IsBoss => Enemy.boss;
    public int PlayerAttackCount => playerAttackCount;
    public int EnemyAttackCount => enemyAttackCount;

    public BattleSubStage(BattleEngine engine, StageData stage, int subStageIndex, CombatStat playerStat,
        double playerStartHp, ulong battleSeed)
    {
        this.engine = engine;
        this.playerStat = playerStat;
        random = new SplitMix64(BattleEngine.GetSubStageSeed(battleSeed, subStageIndex));

        SubStageIndex = subStageIndex;
        Enemy = engine.GetEnemy(stage, subStageIndex);
        EnemyStat = StatCalculator.CalculateEnemy(Enemy, stage);
        TimeLimit = engine.GetTimeLimit(Enemy);
        PlayerStartHp = playerStartHp;

        PlayerHp = playerStartHp;
        EnemyHp = EnemyStat.maxHp;
        killGoldOnWin = BattleEngine.GetKillGold(stage, subStageIndex);
    }

    // deltaTime만큼 시간을 흘리고, 그동안 일어난 공격을 시간 순서대로 모두 적용한다
    public void Advance(double deltaTime)
    {
        if (IsFinished)
            return;

        Elapsed = Math.Min(Elapsed + deltaTime, TimeLimit);

        while (IsFinished == false)
        {
            // 같은 시각이면 플레이어가 먼저 공격한다
            var nextPlayerTime = BattleEngine.GetAttackTime(playerStat, playerAttackCount + 1);
            var nextEnemyTime = BattleEngine.GetAttackTime(EnemyStat, enemyAttackCount + 1);
            var isPlayerTurn = nextPlayerTime <= nextEnemyTime + TimeEpsilon;
            var time = isPlayerTurn ? nextPlayerTime : nextEnemyTime;

            if (time > TimeLimit + TimeEpsilon)
            {
                // 제한 시간 안에 더 이상 공격이 없다. 제한 시간까지 흘렀으면 시간 초과로 끝낸다
                if (Elapsed >= TimeLimit)
                    Finish(BattleSubStageResultEnum.TimeOver, TimeLimit);
                return;
            }

            if (time > Elapsed)
                return;

            ApplyAttack(isPlayerTurn, time);
        }
    }

    // 서버 검증용. 남은 전투를 끝까지 진행한다
    public void RunToEnd()
    {
        while (IsFinished == false)
            Advance(TimeLimit);
    }

    private void ApplyAttack(bool isPlayerTurn, double time)
    {
        BattleEvent battleEvent;
        if (isPlayerTurn)
        {
            playerAttackCount++;
            (double damage, bool isCrit, double lifesteal, double targetHp) = engine.Attack(playerStat, PlayerHp, EnemyHp, random);
            PlayerHp += lifesteal;
            EnemyHp = targetHp;
            battleEvent = new BattleEvent(time, BattleSide.Player, damage, isCrit, lifesteal, PlayerHp, EnemyHp);
        }
        else
        {
            enemyAttackCount++;
            (double damage, bool isCrit, double lifesteal, double targetHp) = engine.Attack(EnemyStat, EnemyHp, PlayerHp, random);
            EnemyHp += lifesteal;
            PlayerHp = targetHp;
            battleEvent = new BattleEvent(time, BattleSide.Enemy, damage, isCrit, lifesteal, PlayerHp, EnemyHp);
        }

        if (EnemyHp <= 0)
        {
            HealOnKill = engine.GetHealOnKill(playerStat, PlayerHp);
            KillGold = killGoldOnWin;
            Finish(BattleSubStageResultEnum.EnemyDead, time);
        }
        else if (PlayerHp <= 0)
        {
            Finish(BattleSubStageResultEnum.PlayerDead, time);
        }

        AttackApplied?.Invoke(battleEvent);
    }

    private void Finish(BattleSubStageResultEnum resultEnum, double duration)
    {
        ResultEnum = resultEnum;
        Duration = duration;
    }
}
