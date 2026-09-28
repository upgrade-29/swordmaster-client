using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class UserArtifact
{
    [JsonProperty("artifactCode")] private string artifactCode;
    [JsonProperty("level")] private int level;
    [JsonProperty("materialCount")] private int materialCount;
    [JsonProperty("equippedSlot")] private int? equippedSlot; // 장착하지 않았으면 null

    public string ArtifactCode => artifactCode;
    public int Level => level;
    public int MaterialCount => materialCount;
    public int? EquippedSlot => equippedSlot;
    public bool IsEquipped => equippedSlot.HasValue;

    [JsonConstructor]
    public UserArtifact(string artifactCode, int level, int materialCount, int? equippedSlot)
    {
        this.artifactCode = artifactCode;
        this.level = level;
        this.materialCount = materialCount;
        this.equippedSlot = equippedSlot;
    }

    public void AddMaterial(int count)
    {
        materialCount += count;
    }

    public void LevelUp(int usedMaterialCount)
    {
        materialCount -= usedMaterialCount;
        level++;
    }

    public void SetEquippedSlot(int? slot)
    {
        equippedSlot = slot;
    }
}
