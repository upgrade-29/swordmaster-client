using System.Collections.Generic;
using Newtonsoft.Json;

// 적 한 마리와의 전투
public class BattleWave
{
    public readonly string enemyCode;
    public readonly bool isBoss;
    public readonly CombatStat enemyStat; // 스테이지 배율이 적용된 능력치
    public readonly double playerStartHp;
    public readonly IReadOnlyList<BattleEvent> events;
    public readonly double duration; // 웨이브가 끝난 시간(초). 시간 초과면 제한 시간
    public readonly WaveOutcome outcome;
    public readonly double healOnKill; // 처치 후 회복량. 처치하지 못했으면 0
    public readonly long gold; // 처치 보상. 처치하지 못했으면 0

    [JsonConstructor]
    public BattleWave(string enemyCode, bool isBoss, CombatStat enemyStat, double playerStartHp,
        IReadOnlyList<BattleEvent> events, double duration, WaveOutcome outcome, double healOnKill, long gold)
    {
        this.enemyCode = enemyCode;
        this.isBoss = isBoss;
        this.enemyStat = enemyStat;
        this.playerStartHp = playerStartHp;
        this.events = events ?? new List<BattleEvent>();
        this.duration = duration;
        this.outcome = outcome;
        this.healOnKill = healOnKill;
        this.gold = gold;
    }
}
