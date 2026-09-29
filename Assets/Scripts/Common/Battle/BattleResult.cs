using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// 전투 API 응답. 클라이언트는 계산하지 않고 이 값대로 연출하고 반영한다
public class BattleResult
{
    public readonly int stage;
    public readonly bool isVictory;
    public readonly CombatStat playerStat;
    public readonly IReadOnlyList<BattleWave> waves;
    public readonly BattleRewards rewards;

    // 보상까지 반영한 뒤의 유저 상태. 클라이언트는 덮어쓰기만 한다
    public readonly UserCurrencies currencies;
    public readonly StageProgress stageProgress;
    public readonly IReadOnlyList<UserArtifact> changedArtifacts;

    // UTC. 남은 쿨타임은 stageProgress.NextBattleAvailableAt - serverTime 으로 계산한다
    public readonly DateTime serverTime;

    [JsonConstructor]
    public BattleResult(int stage, bool isVictory, CombatStat playerStat, IReadOnlyList<BattleWave> waves,
        BattleRewards rewards, UserCurrencies currencies, StageProgress stageProgress,
        IReadOnlyList<UserArtifact> changedArtifacts, DateTime serverTime)
    {
        this.stage = stage;
        this.isVictory = isVictory;
        this.playerStat = playerStat;
        this.waves = waves ?? new List<BattleWave>();
        this.rewards = rewards;
        this.currencies = currencies;
        this.stageProgress = stageProgress;
        this.changedArtifacts = changedArtifacts ?? new List<UserArtifact>();
        this.serverTime = serverTime;
    }
}
