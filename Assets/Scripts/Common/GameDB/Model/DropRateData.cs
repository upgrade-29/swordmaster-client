using Newtonsoft.Json;

public class DropRateData
{
    public readonly ArtifactGrade grade;
    public readonly double rate;

    [JsonConstructor]
    public DropRateData(ArtifactGrade grade, double rate)
    {
        this.grade = grade;
        this.rate = rate;
    }
}
