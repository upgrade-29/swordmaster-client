using Newtonsoft.Json;

public class ArtifactData
{
    public readonly string artifactCode;
    public readonly string name;
    public readonly ArtifactGrade grade;
    public readonly string iconCode;
    public readonly StatType statType;
    public readonly double baseValue;
    public readonly double valuePerLevel;

    [JsonConstructor]
    public ArtifactData(string artifactCode, string name, ArtifactGrade grade, string iconCode,
        StatType statType, double baseValue, double valuePerLevel)
    {
        this.artifactCode = artifactCode;
        this.name = name;
        this.grade = grade;
        this.iconCode = iconCode;
        this.statType = statType;
        this.baseValue = baseValue;
        this.valuePerLevel = valuePerLevel;
    }
}
