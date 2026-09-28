using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class StageProgress
{
    [JsonProperty("clearedStage")] private int clearedStage;
    // UTC
    [JsonProperty("nextBattleAvailableAt")] private DateTime nextBattleAvailableAt;

    public int ClearedStage => clearedStage;
    public int NextStage => clearedStage + 1;
    public DateTime NextBattleAvailableAt => nextBattleAvailableAt;

    [JsonConstructor]
    public StageProgress(int clearedStage, DateTime nextBattleAvailableAt)
    {
        this.clearedStage = clearedStage;
        this.nextBattleAvailableAt = nextBattleAvailableAt;
    }

    public bool CanBattle(DateTime utcNow) => utcNow >= nextBattleAvailableAt;

    public void SetCleared(int stage)
    {
        clearedStage = stage;
    }

    public void SetNextBattleAvailableAt(DateTime utcTime)
    {
        nextBattleAvailableAt = utcTime;
    }
}
