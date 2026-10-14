using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

// IBattleSessionService의 서버 통신 구현. 로그인한 서버의 전투 API를 부른다
// 서버가 전투 에러 코드(ALREADY_IN_PROGRESS 등)로 거절하면 BattleRequestException으로 바꿔 던진다
// 코드가 없는 실패(통신 오류, 동시 요청 409, 401 등)는 ServerRequestException 그대로 던진다. BattleManager는 이것만 다시 보낸다
public class HttpBattleSessionService : IBattleSessionService
{
    private const string SessionPath = "api/battle/session";
    private const string StartPath = "api/battles";
    private const string ReportPath = "api/battle/report";
    private const string AbandonPath = "api/battle/abandon";

    private readonly AuthorizedServerClient client;

    public HttpBattleSessionService(AuthorizedServerClient client)
    {
        this.client = client;
    }

    public Task<BattleApi.BattleSessionInfoResponse> GetActiveSessionAsync(string gameDataVersion)
    {
        // 세션이 없으면 서버가 빈 본문을 주고, client가 null로 돌려준다
        return Send(() => client.GetAsync<BattleApi.BattleSessionInfoResponse>(SessionPath, gameDataVersion));
    }

    public Task<BattleApi.BattleSessionInfoResponse> StartBattleAsync(int stage, string gameDataVersion)
    {
        return Send(() => client.PostAsync<BattleApi.BattleSessionInfoResponse>(StartPath, new BattleApi.StartRequest(stage), gameDataVersion));
    }

    public Task<BattleApi.BattleResultVerifyResponse> ReportSubStageAsync(BattleApi.BattleResultVerifyRequest request,
        string gameDataVersion)
    {
        return Send(() => client.PostAsync<BattleApi.BattleResultVerifyResponse>(ReportPath, request, gameDataVersion));
    }

    public Task<BattleApi.BattleEndResponse> AbandonAsync(string battleId)
    {
        return Send(() => client.PostAsync<BattleApi.BattleEndResponse>(AbandonPath, new BattleApi.AbandonRequest(battleId)));
    }

    private static async Task<T> Send<T>(Func<Task<T>> request)
    {
        try
        {
            return await request();
        }
        catch (ServerRequestException e) when (TryParseErrorEnum(e.Code, out BattleRequestErrorEnum errorEnum))
        {
            throw new BattleRequestException(errorEnum, $"{e.StatusCode} {e.Code}: {e.Message}");
        }
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
