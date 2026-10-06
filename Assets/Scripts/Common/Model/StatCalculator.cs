using System;
using System.Collections.Generic;

public static class StatCalculator
{
    // 검 능력치에 장착한 아티팩트의 능력치를 더한다
    public static CombatStat CalculatePlayer(SwordData sword, IEnumerable<(ArtifactData data, int level)> equippedArtifacts)
    {
        var attack = sword.attackPower;
        var attackSpeed = sword.attackSpeed;
        var maxHp = sword.maxHp;
        var critRate = 0.0;
        var lifesteal = 0.0;

        foreach (var (data, level) in equippedArtifacts)
        {
            var value = GetArtifactValue(data, level);
            switch (data.statType)
            {
                case StatType.AttackSpeed: attackSpeed += value; break;
                case StatType.MaxHp: maxHp += value; break;
                case StatType.CritRate: critRate += value; break;
                case StatType.Lifesteal: lifesteal += value; break;
                default: throw new ArgumentOutOfRangeException(nameof(data.statType), data.statType, data.code);
            }
        }

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

    public static double GetArtifactValue(ArtifactData data, int level)
    {
        return data.baseValue + data.valuePerLevel * (level - 1);
    }
}
