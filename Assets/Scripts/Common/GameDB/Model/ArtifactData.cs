using Newtonsoft.Json;

public class ArtifactData
{
    public readonly string code;
    public readonly string name;
    public readonly ArtifactGrade grade;

    [JsonConstructor]
    public ArtifactData(string code, string name, ArtifactGrade grade)
    {
        this.code = code;
        this.name = name;
        this.grade = grade;
    }
}
