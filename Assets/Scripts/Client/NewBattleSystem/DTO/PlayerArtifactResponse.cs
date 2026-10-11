using Newtonsoft.Json;

// 서버가 내려주는 보유 아티팩트 한 개. 같은 아티팩트를 여러 개 가질 수 있어 id로 구분한다
public class PlayerArtifactResponse
{
    public readonly long id;
    public readonly string code;
    public readonly int level;
    public readonly int? equipSlot; // 장착하지 않았으면 null

    [JsonConstructor]
    public PlayerArtifactResponse(long id, string code, int level, int? equipSlot)
    {
        this.id = id;
        this.code = code;
        this.level = level;
        this.equipSlot = equipSlot;
    }
}
