using Newtonsoft.Json;

// 로그인과 회원가입 요청을 만든다. 만든 요청은 HttpRequest.Request로 보낸다
public static class AuthApi
{
    public class LoginRequest
    {
        public readonly string email;
        public readonly string password;

        [JsonConstructor]
        public LoginRequest(string email, string password)
        {
            this.email = email;
            this.password = password;
        }
    }

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

    public class LoginResponse
    {
        public readonly string accessToken;

        [JsonConstructor]
        public LoginResponse(string accessToken)
        {
            this.accessToken = accessToken;
        }
    }

    public static HttpRequestInfo<LoginResponse> Login(string email, string password)
    {
        return HttpRequestFactory.Post<LoginResponse>("api/auth/login", new LoginRequest(email, password));
    }

    // 성공하면 본문 없이 201을 받는다
    public static HttpRequestInfo<EmptyResponse> Signup(string email, string password)
    {
        return HttpRequestFactory.Post<EmptyResponse>("api/auth/signup", new SignupRequest(email, password));
    }
}
