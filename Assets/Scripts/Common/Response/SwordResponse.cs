using Newtonsoft.Json;

// 서버가 내려주는 검 상태. 플레이어 정보와 검 강화/판매 응답이 같이 쓴다
public class SwordResponse
{
    public readonly int level;

    [JsonConstructor]
    public SwordResponse(int level)
    {
        this.level = level;
    }
}
