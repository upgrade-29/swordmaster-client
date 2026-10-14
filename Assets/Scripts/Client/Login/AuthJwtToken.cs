// 로그인으로 받은 JWT 토큰과, 그 토큰을 발급한 서버 주소. 씬이 바뀌어도 유지되도록 정적으로 들고 있는다
// 토큰은 HttpConfig의 Authorization 공통 헤더에도 같이 넣어서, HttpRequest로 보내는 요청에 붙게 한다
// 전투(AuthorizedServerClient)와 채팅은 여기서 토큰과 서버 주소를 직접 꺼내 쓴다
public static class AuthJwtToken
{
    private const string AuthorizationHeader = "Authorization";

    public static string Value { get; private set; }
    public static string ServerUrl { get; private set; } // 끝의 '/'는 뺀 값
    public static bool HasValue => string.IsNullOrEmpty(Value) == false;
    // 로그인한 상태에서 서버가 토큰을 거절해 로그인 씬으로 돌아가는 중인지. 로그인 화면이 안내 문구를 띄운 뒤 지운다
    public static bool IsExpired { get; private set; }

    public static void Set(string accessToken, string serverUrl)
    {
        Value = accessToken;
        ServerUrl = serverUrl.TrimEnd('/');
        IsExpired = false;
        HttpConfig.SetHeader(AuthorizationHeader, $"Bearer {accessToken}");
    }

    // 로그인한 상태에서 서버가 401로 토큰을 거절했을 때
    public static void Expire()
    {
        Clear();
        IsExpired = true;
    }

    public static void ClearExpired()
    {
        IsExpired = false;
    }

    // 로그아웃하거나 서버가 401로 토큰을 거절했을 때
    public static void Clear()
    {
        Value = null;
        ServerUrl = null;
        HttpConfig.RemoveHeader(AuthorizationHeader);
    }
}
