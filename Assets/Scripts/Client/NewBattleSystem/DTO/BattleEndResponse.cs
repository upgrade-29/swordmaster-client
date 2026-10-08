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
    public readonly UserCurrencies currencies;
    public readonly StageProgress stageProgress;
    public readonly IReadOnlyList<UserArtifact> changedArtifacts;

    public readonly DateTime serverTime; // UTC

    [JsonConstructor]
    public BattleEndResponse(int stage, bool isVictory, BattleRewards rewards, UserCurrencies currencies,
        StageProgress stageProgress, IReadOnlyList<UserArtifact> changedArtifacts, DateTime serverTime)
    {
        this.stage = stage;
        this.isVictory = isVictory;
        this.rewards = rewards;
        this.currencies = currencies;
        this.stageProgress = stageProgress;
        this.changedArtifacts = changedArtifacts ?? new List<UserArtifact>();
        this.serverTime = serverTime;
    }
}
