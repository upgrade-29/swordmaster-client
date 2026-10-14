using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

// IBattleSessionService의 서버 통신 구현. BattleApi로 만든 요청을 HttpRequest로 보내고 Task로 기다린다
// 서버 주소와 Authorization 헤더는 HttpConfig 공통 설정을 쓰고, 401은 HttpErrorHandler가 로그인 씬으로 돌려보낸다
// 서버가 전투 에러 코드(ALREADY_IN_PROGRESS 등)로 거절하면 BattleRequestException으로 바꿔 던진다
// 코드가 없는 실패(통신 오류, 동시 요청 409, 401 등)는 ServerRequestException으로 던진다. BattleManager는 이것만 다시 보낸다
public class HttpBattleSessionService : IBattleSessionService
{
    public Task<BattleApi.BattleSessionInfoResponse> GetActiveSessionAsync(string gameDataVersion)
    {
        return Send(BattleApi.GetActiveSession(gameDataVersion));
    }

    public Task<BattleApi.BattleSessionInfoResponse> StartBattleAsync(int stage, string gameDataVersion)
    {
        return Send(BattleApi.Start(stage, gameDataVersion));
    }

    public Task<BattleApi.BattleResultVerifyResponse> ReportSubStageAsync(BattleApi.BattleResultVerifyRequest request,
        string gameDataVersion)
    {
        return Send(BattleApi.Report(request, gameDataVersion));
    }

    public Task<BattleApi.BattleEndResponse> AbandonAsync(string battleId)
    {
        return Send(BattleApi.Abandon(battleId));
    }

    // 전투 중 세부스테이지마다 보내는 요청이라 로딩 표시는 하지 않는다
    private static Task<T> Send<T>(HttpRequestInfo<T> requestInfo) where T : class
    {
        var completion = new TaskCompletionSource<T>();
        HttpRequest.Request(requestInfo, (error, result) =>
        {
            if (error == null)
                completion.SetResult(result);
            else
                completion.SetException(ToException(error));
        }, showLoading: false);
        return completion.Task;
    }

    private static Exception ToException(HttpError error)
    {
        if (TryParseErrorEnum(error.Code, out BattleRequestErrorEnum errorEnum))
            return new BattleRequestException(errorEnum, $"{error.StatusCode} {error.Code}: {error.Message}");

        return new ServerRequestException(error.StatusCode, error.Code, error.Message);
    }

    private static bool TryParseErrorEnum(string code, out BattleRequestErrorEnum errorEnum)
    {
        errorEnum = BattleRequestErrorEnum.None;
        if (string.IsNullOrEmpty(code))
            return false;

        try
        {
            errorEnum = JsonConvert.DeserializeObject<BattleRequestErrorEnum>($"\"{code}\"");
        }
        catch (JsonException)
        {
            return false; // 클라가 모르는 코드
        }

        return errorEnum != BattleRequestErrorEnum.None;
    }
}
