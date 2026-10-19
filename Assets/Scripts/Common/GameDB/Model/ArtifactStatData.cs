using Newtonsoft.Json;

public class ArtifactStatData
{
    public readonly string code;
    public readonly StatType statType;
    public readonly double baseValue;
    public readonly double valuePerLevel;

    [JsonConstructor]
    public ArtifactStatData(string code, StatType statType, double baseValue, double valuePerLevel)
    {
        this.code = code;
        this.statType = statType;
        this.baseValue = baseValue;
        this.valuePerLevel = valuePerLevel;
    }
}
