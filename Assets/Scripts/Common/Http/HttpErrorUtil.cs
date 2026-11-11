// 여러 화면이 HttpError를 같은 기준으로 판단하도록 모은 메서드
public static class HttpErrorUtil
{
    private const int InternalServerErrorStatusCode = 500;
    private const int ServiceUnavailableStatusCode = 503;

    // 응답을 받지 못했거나 서버 내부 오류이면 서버가 요청을 처리했는지 알 수 없다
    public static bool IsResultUnknown(HttpError error)
    {
        if (error.Type == HttpErrorType.Network)
        {
            return true;
        }

        return error.Type == HttpErrorType.Server
            && (error.StatusCode == InternalServerErrorStatusCode || error.StatusCode == ServiceUnavailableStatusCode);
    }
}
