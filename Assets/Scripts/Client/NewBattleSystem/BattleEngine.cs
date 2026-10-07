using System;

// 전투 규칙. 클라는 이걸로 실제 전투를 진행하고, 서버는 같은 코드로 다시 돌려서 클라가 보낸 결과를 검증한다
// 세부스테이지 번호(subStageIndex)는 0부터 센다. index상으로 일반 적 enemyCount-1까지 일반 적, enemyCount은 보스
public class BattleEngine
{
    private readonly BattleDB battleDB;

    public BattleEngine(BattleDB battleDB)
    {
        this.battleDB = battleDB;
    }

    public static int GetSubStageCount(StageData stage)
    {
        return stage.enemyCount + 1;
    }

    public static bool IsBossSubStage(StageData stage, int subStageIndex)
    {
        return subStageIndex == stage.enemyCount;
    }

    // 세부스테이지마다 난수를 따로 쓴다. 서버가 앞 세부스테이지를 다시 돌리지 않고 해당 세부스테이지만 검증할 수 있다
    public static ulong GetSubStageSeed(ulong battleSeed, int subStageIndex)
    {
        return SplitMix64.Mix(battleSeed + (ulong)subStageIndex);
    }

    public EnemyData GetEnemy(StageData stage, int subStageIndex)
    {
        return battleDB.GetEnemy(IsBossSubStage(stage, subStageIndex) ? stage.bossCode : stage.enemyCode);
    }

    public static long GetKillGold(StageData stage, int subStageIndex)
    {
        return IsBossSubStage(stage, subStageIndex) ? stage.bossGold : stage.goldPerEnemy;
    }

    // 제한 시간 안에 적을 처치하지 못하면 패배
    public double GetTimeLimit(EnemyData enemy)
    {
        return enemy.boss ? battleDB.bossTimeLimit : battleDB.normalTimeLimit;
    }

    // n번째 공격 시각 = n × 공격속도(초당 공격횟수)의 역수. 공격속도가 1.5일때, 2번째 공격 시간은 2 × (1 / 1.5) = 1.333초
    public static double GetAttackTime(CombatStat attacker, int attackNumber)
    {
        return attackNumber * (1 / attacker.attackSpeed);
    }

    // 흡혈량은 치명타를 포함한 데미지 기준이고, 최대 체력을 넘지 않는다
    public (double damage, bool isCrit, double lifesteal, double targetHp) Attack(CombatStat attacker, double attackerHp,
        double targetHp, SplitMix64 random)
    {
        var isCrit = random.NextDouble() < attacker.critRate;
        var damage = attacker.attack * (isCrit ? battleDB.critMultiplier : 1);
        var lifesteal = Math.Min(damage * attacker.lifesteal, attacker.maxHp - attackerHp);
        return (damage, isCrit, lifesteal, Math.Max(0, targetHp - damage));
    }

    // 적을 처치하면 최대 체력의 일정 비율을 회복한다. 최대 체력은 넘지 않는다
    public double GetHealOnKill(CombatStat player, double playerHp)
    {
        return Math.Min(player.maxHp * battleDB.healRate, player.maxHp - playerHp);
    }
}
