using Newtonsoft.Json;

// POST api/auth/signup 요청 본문. 필드명은 서버 계약 확정 전까지 가정값이다.
public class SignupRequest
{
    public readonly string email;
    public readonly string password;

    [JsonConstructor]
    public SignupRequest(string email, string password)
    {
        this.email = email;
        this.password = password;
    }
}
