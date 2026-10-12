using Newtonsoft.Json;

// 서버가 내려주는 보유 아티팩트 한 종류. 같은 아티팩트를 또 얻으면 재료 개수가 늘어난다
public class ArtifactResponse
{
    public readonly string code;
    public readonly int level;
    public readonly int materialCount;
    public readonly int? equipSlot; // 장착하지 않았으면 null

    [JsonConstructor]
    public ArtifactResponse(string code, int level, int materialCount, int? equipSlot)
    {
        this.code = code;
        this.level = level;
        this.materialCount = materialCount;
        this.equipSlot = equipSlot;
    }

    public UserArtifact ToUserArtifact()
    {
        return new UserArtifact(code, level, materialCount, equipSlot);
    }
}
