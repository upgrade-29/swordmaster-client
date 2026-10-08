using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class StageProgress
{
    [JsonProperty("clearedStage")] private int clearedStage;

    public int ClearedStage => clearedStage;
    public int NextStage => clearedStage + 1;

    [JsonConstructor]
    public StageProgress(int clearedStage)
    {
        this.clearedStage = clearedStage;
    }

    public void SetCleared(int stage)
    {
        clearedStage = stage;
    }
}
