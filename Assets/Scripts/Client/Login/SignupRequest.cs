using Newtonsoft.Json;

// POST api/auth/signup 요청 본문. 필드명은 서버 계약 확정 전까지 가정값이다.
public class SignupRequest
{
    public readonly string loginId;
    public readonly string password;
    public readonly string nickname;

    [JsonConstructor]
    public SignupRequest(string loginId, string password, string nickname)
    {
        this.loginId = loginId;
        this.password = password;
        this.nickname = nickname;
    }
}
