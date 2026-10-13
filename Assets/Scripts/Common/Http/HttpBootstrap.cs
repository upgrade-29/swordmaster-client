using UnityEngine;
using UnityEngine.SceneManagement;

// 부트스트랩 씬이 없어서, 어느 씬에서 시작하든 전역 에러 핸들러가 적용되도록 앱이 시작될 때 자동으로 등록한다
// HttpRequest 오브젝트는 싱글턴이 처음 요청할 때 만든다. 부트스트랩 씬을 만들면 이 등록을 그쪽으로 옮긴다
public static class HttpBootstrap
{
    private const int UnauthorizedStatusCode = 401;
    private const string LoginSceneName = "LoginScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        HttpRequest.SetGlobalErrorHandler(OnGlobalHttpError);
    }

    // 로그인한 상태에서 401을 받으면 토큰이 만료됐거나 거절된 것이므로, 토큰을 지우고 로그인 씬으로 돌아간다
    // 토큰 없이 보낸 요청의 401(로그인할 때 비밀번호가 틀린 경우 등)은 호출부가 처리하도록 처리하지 않은 것으로 돌려준다
    private static bool OnGlobalHttpError(HttpError error)
    {
        if (error.Type != HttpErrorType.Server || error.StatusCode != UnauthorizedStatusCode)
        {
            return false;
        }

        // 동시에 보낸 요청의 401이 뒤따라 도착해도 한 번만 처리한다
        if (AuthJwtToken.IsExpired)
        {
            return true;
        }

        if (AuthJwtToken.HasValue == false)
        {
            return false;
        }

        AuthJwtToken.Expire();
        SceneManager.LoadScene(LoginSceneName);
        return true;
    }
}
