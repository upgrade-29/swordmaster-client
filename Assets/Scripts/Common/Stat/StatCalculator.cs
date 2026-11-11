using System;
using System.Collections.Generic;
using System.Linq;

public static class StatCalculator
{
    // 유저의 검 레벨과 장착한 아티팩트로 능력치를 계산한다
    public static CombatStat CalculatePlayer(GameDB gameDB, User user)
    {
        SwordData sword = gameDB.swords.First(x => x.level == user.Sword.Level);
        var equippedArtifacts = user.EquippedArtifacts.Select(x => (gameDB.GetArtifactStats(x.ArtifactCode), x.Level));
        return CalculatePlayer(sword, equippedArtifacts);
    }

    // 검 능력치에 장착한 아티팩트의 능력치를 더한다. 공격 속도만 검 공격 속도에 대한 비율이다
    public static CombatStat CalculatePlayer(SwordData sword, IEnumerable<(IReadOnlyList<ArtifactStatData> stats, int level)> equippedArtifacts)
    {
        var attack = sword.attackPower;
        var attackSpeedRate = 0.0;
        var maxHp = sword.maxHp;
        var critRate = 0.0;
        var lifesteal = 0.0;

        foreach (var (stats, level) in equippedArtifacts)
        {
            foreach (ArtifactStatData stat in stats)
            {
                var value = GetArtifactValue(stat, level);
                switch (stat.statType)
                {
                    case StatType.AttackSpeed: attackSpeedRate += value; break;
                    case StatType.MaxHp: maxHp += value; break;
                    case StatType.CritRate: critRate += value; break;
                    case StatType.Lifesteal: lifesteal += value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(stat.statType), stat.statType, stat.code);
                }
            }
        }

        var attackSpeed = sword.attackSpeed * (1 + attackSpeedRate);
        return new CombatStat(attack, attackSpeed, Math.Floor(maxHp), critRate, lifesteal);
    }

    // 스테이지 배율은 공격력과 최대 체력에만 곱한다
    public static CombatStat CalculateEnemy(EnemyData enemy, StageData stage)
    {
        var stat = enemy.stat;
        return new CombatStat(
            stat.attack * stage.statMultiplier,
            stat.attackSpeed,
            Math.Floor(stat.maxHp * stage.statMultiplier),
            stat.critRate,
            stat.lifesteal);
    }

    public static double GetArtifactValue(ArtifactStatData stat, int level)
    {
        return stat.baseValue + stat.valuePerLevel * (level - 1);
    }
}
