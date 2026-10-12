// 로그인 씬이 로비에 넘기는 값. 서버에서 받은 플레이어 정보로 만든 유저를 담는다
// 한 번 읽으면 지워진다. 값이 없으면(로그인을 거치지 않은 경우) TestLobbyDataLoader가 테스트 데이터를 읽는다
public static class LoginEntry
{
    private static User user;

    public static void Set(User user)
    {
        LoginEntry.user = user;
    }

    public static bool TryConsume(out User user)
    {
        user = LoginEntry.user;
        LoginEntry.user = null;
        return user != null;
    }
}
