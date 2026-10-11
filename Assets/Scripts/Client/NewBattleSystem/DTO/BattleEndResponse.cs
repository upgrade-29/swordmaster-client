using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// 전투가 끝났을 때(마지막 세부스테이지 승리, 패배, 포기) 세션에 모아 둔 보상을 한 번에 반영한 결과
// 서버는 이 시점에 세션을 지운다
public class BattleEndResponse
{
    public readonly int stage;
    public readonly bool isVictory;
    public readonly BattleRewards rewards; // 패배해도 그때까지 처치한 적의 골드는 준다

    // 보상까지 반영한 뒤의 유저 상태. 클라는 덮어쓰기만 한다
    public readonly IReadOnlyDictionary<CurrencyType, long> currencies; // 보상까지 반영한 전체 재화. {"GOLD": 130, "DIAMOND": 0}
    public readonly StageProgress stageProgress;
    public readonly IReadOnlyList<PlayerArtifactResponse> changedArtifacts; // 드랍으로 새로 생기거나 바뀐 아티팩트

    public readonly DateTime serverTime; // UTC

    [JsonConstructor]
    public BattleEndResponse(int stage, bool isVictory, BattleRewards rewards,
        IReadOnlyDictionary<CurrencyType, long> currencies, StageProgress stageProgress,
        IReadOnlyList<PlayerArtifactResponse> changedArtifacts, DateTime serverTime)
    {
        this.stage = stage;
        this.isVictory = isVictory;
        this.rewards = rewards;
        this.currencies = currencies ?? new Dictionary<CurrencyType, long>();
        this.stageProgress = stageProgress;
        this.changedArtifacts = changedArtifacts ?? new List<PlayerArtifactResponse>();
        this.serverTime = serverTime;
    }
}
