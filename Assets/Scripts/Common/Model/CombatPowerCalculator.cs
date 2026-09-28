using System;

public static class CombatPowerCalculator
{
    // 기대 DPS  = 공격력 × 공격 속도 × (1 + 치명타 확률 × (치명타 배율 − 1))
    // 유효 체력 = 최대 체력 + (기대 DPS × 생명력 흡수율 × 기준 전투 시간)
    // 전투력    = √(기대 DPS × 유효 체력) × 10
    public static long Calculate(CombatStat stat, double critMultiplier, double baseBattleTime)
    {
        var expectedDps = GetExpectedDps(stat, critMultiplier);
        var effectiveHp = stat.maxHp + expectedDps * stat.lifesteal * baseBattleTime;
        return (long)Math.Floor(Math.Sqrt(expectedDps * effectiveHp) * 10);
    }

    public static double GetExpectedDps(CombatStat stat, double critMultiplier)
    {
        return stat.attack * stat.attackSpeed * (1 + stat.critRate * (critMultiplier - 1));
    }
}
