using Newtonsoft.Json;

// 로그인과 회원가입 요청을 만든다. 만든 요청은 HttpRequest.Request로 보낸다
public static class AuthApi
{
    public const int UnauthorizedStatusCode = 401; // 리프레시 토큰이나 액세스 토큰이 만료됐거나 무효다

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

    public class RefreshRequest
    {
        public readonly string refreshToken;

        [JsonConstructor]
        public RefreshRequest(string refreshToken)
        {
            this.refreshToken = refreshToken;
        }
    }

    // 로그인과 리프레시가 같은 토큰 쌍을 돌려준다
    public class TokenResponse
    {
        public readonly string accessToken;
        public readonly string refreshToken; // 자동 로그인에 쓴다. 기기에 저장해 두고 다음 실행 때 /api/auth/refresh로 보낸다

        [JsonConstructor]
        public TokenResponse(string accessToken, string refreshToken)
        {
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;
        }
    }

    public static HttpRequestInfo<TokenResponse> Login(string email, string password)
    {
        return HttpRequestFactory.Post<TokenResponse>("api/auth/login", new LoginRequest(email, password));
    }
    public static HttpRequestInfo<TokenResponse> GuestLogin()
    {
        return HttpRequestFactory.Post<TokenResponse>("api/auth/login/guest");
    }

    // 저장해 둔 리프레시 토큰으로 새 토큰 쌍을 받는다. 보낸 리프레시 토큰은 무효가 되고, 만료됐거나 무효면 401을 받는다
    public static HttpRequestInfo<TokenResponse> Refresh(string refreshToken)
    {
        return HttpRequestFactory.Post<TokenResponse>("api/auth/refresh", new RefreshRequest(refreshToken));
    }

    // 서버에 저장된 리프레시 토큰을 지운다. 공통 헤더의 액세스 토큰으로 누구인지 알리므로 본문은 없다
    // 액세스 토큰이 이미 만료됐으면 401을 받는다. 기기에서는 응답과 관계없이 로그아웃하므로 에러 로그를 남기지 않는다
    public static HttpRequestInfo<EmptyResponse> Logout()
    {
        return HttpRequestFactory.Post<EmptyResponse>("api/auth/logout", silentStatusCodes: new[] { UnauthorizedStatusCode });
    }

    // 성공하면 본문 없이 201을 받는다
    public static HttpRequestInfo<EmptyResponse> Signup(string email, string password)
    {
        return HttpRequestFactory.Post<EmptyResponse>("api/auth/signup", new SignupRequest(email, password));
    }

    public class GoogleLoginRequest
    {
        public readonly string idToken;

        [JsonConstructor]
        public GoogleLoginRequest(string idToken)
        {
            this.idToken = idToken;
        }
    }

    public static HttpRequestInfo<TokenResponse> GoogleLogin(
        string idToken
    )
    {
        return HttpRequestFactory.Post<TokenResponse>(
            "api/auth/login/google",
            new GoogleLoginRequest(idToken)
        );
    }

    public class IdentityLinkResponse
    {
        public readonly string provider;
        public readonly bool linked;

        [JsonConstructor]
        public IdentityLinkResponse(string provider, bool linked)
        {
            this.provider = provider;
            this.linked = linked;
        }
    }

    public static HttpRequestInfo<IdentityLinkResponse> LinkGoogle(string idToken)
    {
        return HttpRequestFactory.Post<IdentityLinkResponse>(
            "api/user/identities/google",
            new GoogleLoginRequest(idToken)
        );
    }

}
