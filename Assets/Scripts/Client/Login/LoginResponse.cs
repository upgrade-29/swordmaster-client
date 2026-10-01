using Newtonsoft.Json;

// POST api/auth/login 성공 응답. 필드명은 서버 계약 확정 전까지 가정값이다.
public class LoginResponse
{
    public readonly string accessToken;

    [JsonConstructor]
    public LoginResponse(string accessToken)
    {
        this.accessToken = accessToken;
    }
}
