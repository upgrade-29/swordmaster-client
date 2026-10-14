using UnityEngine.SceneManagement;

/// <summary>
/// 모든 요청의 에러를 콜백보다 먼저 받아서 공통으로 처리합니다. 등록 없이 첫 요청부터 항상 호출됩니다.
/// 처리했다면 true를 반환합니다. 콜백에는 IsHandled=true로 전달되므로, 호출부에서 에러 팝업을 다시 띄우지 않습니다.
/// </summary>
public static class HttpErrorHandler
{
    private const int UnauthorizedStatusCode = 401;

    // 로그인한 상태에서 401을 받으면 토큰이 만료됐거나 거절된 것이므로, 토큰을 지우고 로그인 씬으로 돌아간다
    // 토큰 없이 보낸 요청의 401(로그인할 때 비밀번호가 틀린 경우 등)은 호출부가 처리하도록 처리하지 않은 것으로 돌려준다
    public static bool OnError(HttpError error)
    {
        if (error.Type != HttpErrorType.Server || error.StatusCode != UnauthorizedStatusCode)
        {
            return false;
        }

        // 세션이 없으면 로그인한 적이 없거나 플레이를 끝내는 중이다. Instance로 새 세션을 만들지 않도록 먼저 확인한다
        if (GameSession.IsInitialize() == false)
        {
            return false;
        }

        GameSession session = GameSession.Instance;

        // 동시에 보낸 요청의 401이 뒤따라 도착해도 한 번만 처리한다
        if (session.IsExpired)
        {
            return true;
        }

        if (session.IsLoggedIn == false)
        {
            return false;
        }

        session.Expire();
        SceneManager.LoadScene(SceneNames.Login);
        return true;
    }
}
