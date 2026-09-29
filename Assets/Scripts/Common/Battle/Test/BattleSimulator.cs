using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// 전투를 처음부터 끝까지 계산해서 BattleResult를 만든다. 서버가 생기면 서버에서 실행한다
public class BattleSimulator
{
    // config에 값이 아직 없을 때 쓰는 기본값
    private const double DefaultNormalTimeLimit = 30;
    private const double DefaultBossTimeLimit = 60;

    // 부동소수점 오차 때문에 같은 시각의 공격이 미세하게 어긋나는 것을 같은 시각으로 본다
    private const double TimeEpsilon = 1e-9;

    private readonly Random random;
    private readonly double critMultiplier;
    private readonly double healRate;
    private readonly double normalTimeLimit;
    private readonly double bossTimeLimit;

    // 테스트할 때는 시드를 고정한 Random을 넣으면 같은 전투가 나온다
    public BattleSimulator(GameDB gameDB, Random random)
    {
        this.random = random;
        critMultiplier = GetConfig(gameDB, "CRIT_MULTIPLIER", 1.5);
        healRate = GetConfig(gameDB, "HEAL_RATE", 0);
        normalTimeLimit = GetConfig(gameDB, "MAX_BATTLE_TIME_NORMAL", DefaultNormalTimeLimit);
        bossTimeLimit = GetConfig(gameDB, "MAX_BATTLE_TIME_BOSS", DefaultBossTimeLimit);
    }

    // 적 한 마리와 싸운다. 공격 간격은 1 / 공격 속도이고, 같은 시각이면 플레이어가 먼저 공격한다
    public BattleWave SimulateWave(CombatStat player, double playerStartHp, EnemyData enemy, CombatStat enemyStat, long killGold)
    {
        var timeLimit = enemy.boss ? bossTimeLimit : normalTimeLimit;
        var playerInterval = 1 / player.attackSpeed;
        var enemyInterval = 1 / enemyStat.attackSpeed;

        var playerHp = playerStartHp;
        var enemyHp = enemyStat.maxHp;
        var playerAttackCount = 0;
        var enemyAttackCount = 0;
        var events = new List<BattleEvent>();

        while (true)
        {
            // 간격을 계속 더하면 오차가 쌓이므로 횟수 × 간격으로 계산한다
            var nextPlayerTime = (playerAttackCount + 1) * playerInterval;
            var nextEnemyTime = (enemyAttackCount + 1) * enemyInterval;
            var isPlayerTurn = nextPlayerTime <= nextEnemyTime + TimeEpsilon;
            var time = isPlayerTurn ? nextPlayerTime : nextEnemyTime;

            if (time > timeLimit + TimeEpsilon)
                return CreateWave(enemy, enemyStat, playerStartHp, events, timeLimit, WaveOutcome.TimeOver, 0, 0);

            if (isPlayerTurn)
            {
                playerAttackCount++;
                var attack = Attack(player, playerHp, enemyHp);
                playerHp += attack.lifesteal;
                enemyHp = attack.targetHp;
                events.Add(new BattleEvent(time, BattleSide.Player, attack.damage, attack.isCrit, attack.lifesteal, playerHp, enemyHp));
            }
            else
            {
                enemyAttackCount++;
                var attack = Attack(enemyStat, enemyHp, playerHp);
                enemyHp += attack.lifesteal;
                playerHp = attack.targetHp;
                events.Add(new BattleEvent(time, BattleSide.Enemy, attack.damage, attack.isCrit, attack.lifesteal, playerHp, enemyHp));
            }

            if (enemyHp <= 0)
            {
                var healOnKill = Math.Min(player.maxHp * healRate, player.maxHp - playerHp);
                return CreateWave(enemy, enemyStat, playerStartHp, events, time, WaveOutcome.EnemyDead, healOnKill, killGold);
            }

            if (playerHp <= 0)
                return CreateWave(enemy, enemyStat, playerStartHp, events, time, WaveOutcome.PlayerDead, 0, 0);
        }
    }

    // 흡혈량은 치명타를 포함한 데미지 기준이고, 최대 체력을 넘지 않는다
    private (double damage, bool isCrit, double lifesteal, double targetHp) Attack(CombatStat attacker, double attackerHp, double targetHp)
    {
        var isCrit = random.NextDouble() < attacker.critRate;
        var damage = attacker.attack * (isCrit ? critMultiplier : 1);
        var lifesteal = Math.Min(damage * attacker.lifesteal, attacker.maxHp - attackerHp);
        return (damage, isCrit, lifesteal, Math.Max(0, targetHp - damage));
    }

    private static BattleWave CreateWave(EnemyData enemy, CombatStat enemyStat, double playerStartHp,
        List<BattleEvent> events, double duration, WaveOutcome outcome, double healOnKill, long gold)
    {
        return new BattleWave(enemy.enemyCode, enemy.boss, enemyStat, playerStartHp, events, duration, outcome, healOnKill, gold);
    }

    private static double GetConfig(GameDB gameDB, string key, double defaultValue)
    {
        var config = gameDB.config.FirstOrDefault(data => data.key == key);
        return config == null ? defaultValue : double.Parse(config.value, CultureInfo.InvariantCulture);
    }
}
