using Newtonsoft.Json;

public class ArtifactData
{
    public readonly string code;
    public readonly string name;
    public readonly ArtifactGrade grade;
    public readonly StatType statType;
    public readonly double baseValue;
    public readonly double valuePerLevel;

    [JsonConstructor]
    public ArtifactData(string code, string name, ArtifactGrade grade,
        StatType statType, double baseValue, double valuePerLevel)
    {
        this.code = code;
        this.name = name;
        this.grade = grade;
        this.statType = statType;
        this.baseValue = baseValue;
        this.valuePerLevel = valuePerLevel;
    }
}
