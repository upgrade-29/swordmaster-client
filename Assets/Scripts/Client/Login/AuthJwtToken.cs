// 로그인으로 받은 JWT 토큰과, 그 토큰을 발급한 서버 주소. 씬이 바뀌어도 유지되도록 정적으로 들고 있는다
// 토큰이 필요한 요청은 AuthorizedServerClient가 여기서 꺼내 Authorization 헤더에 붙이고, 같은 서버로 보낸다
public static class AuthJwtToken
{
    public static string Value { get; private set; }
    public static string ServerUrl { get; private set; } // 끝의 '/'는 뺀 값
    public static bool HasValue => string.IsNullOrEmpty(Value) == false;

    public static void Set(string accessToken, string serverUrl)
    {
        Value = accessToken;
        ServerUrl = serverUrl.TrimEnd('/');
    }

    // 로그아웃하거나 서버가 401로 토큰을 거절했을 때
    public static void Clear()
    {
        Value = null;
        ServerUrl = null;
    }
}
