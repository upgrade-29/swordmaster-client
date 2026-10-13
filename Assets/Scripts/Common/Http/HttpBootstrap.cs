using UnityEngine;

// 부트스트랩 씬이 없어서, 어느 씬에서 시작하든 전역 에러 핸들러가 적용되도록 앱이 시작될 때 자동으로 등록한다
// HttpRequest 오브젝트는 싱글턴이 처음 요청할 때 만든다. 부트스트랩 씬을 만들면 이 등록을 그쪽으로 옮긴다
public static class HttpBootstrap
{
    private const int UnauthorizedStatusCode = 401;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        HttpRequest.SetGlobalErrorHandler(OnGlobalHttpError);
    }

    // 토큰이 만료됐거나 거절됐으면 저장된 토큰을 지운다. 화면 처리는 호출부에 맡기므로 처리하지 않은 것으로 돌려준다
    private static bool OnGlobalHttpError(HttpError error)
    {
        if (error.Type == HttpErrorType.Server && error.StatusCode == UnauthorizedStatusCode)
        {
            AuthJwtToken.Clear();
        }

        return false;
    }
}
