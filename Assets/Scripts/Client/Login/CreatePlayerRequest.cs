using Newtonsoft.Json;

// POST api/players 요청 본문. 서버가 아직 닉네임을 받지 않아 필드명은 서버 계약 확정 전까지 가정값이다.
public class CreatePlayerRequest
{
    public readonly string nickname;

    [JsonConstructor]
    public CreatePlayerRequest(string nickname)
    {
        this.nickname = nickname;
    }
}
