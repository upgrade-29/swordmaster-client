using Newtonsoft.Json;

public class ArtifactEnhanceData
{
    public readonly ArtifactGrade grade;
    public readonly int level; // 강화하기 전 레벨
    public readonly int materialCount;
    public readonly long gold;

    [JsonConstructor]
    public ArtifactEnhanceData(ArtifactGrade grade, int level, int materialCount, long gold)
    {
        this.grade = grade;
        this.level = level;
        this.materialCount = materialCount;
        this.gold = gold;
    }
}
