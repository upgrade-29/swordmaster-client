using Newtonsoft.Json;

public class EnhanceEquippedArtifactResult
{
    public readonly int slot;
    public readonly string artifactCode;
    public readonly int level;
    public readonly int materialCount;

    [JsonConstructor]
    public EnhanceEquippedArtifactResult(int slot, string artifactCode, int level, int materialCount)
    {
        this.slot = slot;
        this.artifactCode = artifactCode;
        this.level = level;
        this.materialCount = materialCount;
    }
}
