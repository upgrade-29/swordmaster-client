using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// 로그인 토큰이 필요한 서버 요청을 보낸다. 로그인한 서버(AuthJwtToken.ServerUrl)로 Authorization 헤더를 붙여 보낸다
// 게임 데이터로 계산하는 요청은 gameDataVersion을 넘기면 X-Game-Data-Version 헤더에 담는다
// 실패하면 ServerRequestException을 던진다. 토큰이 거절되면(401) 저장된 토큰을 지운다
public class AuthorizedServerClient
{
    public const string GameDataVersionHeader = "X-Game-Data-Version";

    private readonly int timeoutSeconds;

    public AuthorizedServerClient(int timeoutSeconds = 10)
    {
        this.timeoutSeconds = timeoutSeconds;
    }

    // 응답 본문이 비어 있으면 null
    public async Task<T> GetAsync<T>(string path, string gameDataVersion = null) where T : class
    {
        string json = await SendAsync(UnityWebRequest.kHttpVerbGET, path, null, gameDataVersion);
        return Deserialize<T>(json);
    }

    // 응답 본문이 비어 있으면 null
    public async Task<T> PostAsync<T>(string path, object body, string gameDataVersion = null) where T : class
    {
        string json = await SendAsync(UnityWebRequest.kHttpVerbPOST, path, body, gameDataVersion);
        return Deserialize<T>(json);
    }

    private async Task<string> SendAsync(string method, string path, object body, string gameDataVersion)
    {
        if (AuthJwtToken.HasValue == false)
            throw new ServerRequestException(401, null, "로그인이 필요합니다.");

        using (var request = new UnityWebRequest($"{AuthJwtToken.ServerUrl}/{path.TrimStart('/')}", method))
        {
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {AuthJwtToken.Value}");
            if (gameDataVersion != null)
                request.SetRequestHeader(GameDataVersionHeader, gameDataVersion);
            request.timeout = timeoutSeconds;

            var completion = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => completion.SetResult(true);
            await completion.Task;

            if (request.result == UnityWebRequest.Result.Success)
                return request.downloadHandler.text;

            throw ToException(request);
        }
    }

    private static ServerRequestException ToException(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.ProtocolError)
            return new ServerRequestException(0, null, $"서버에 연결할 수 없습니다. ({request.error})");

        // 토큰이 만료됐거나 잘못됐다. 다시 로그인해야 한다. 401은 본문 형식이 정해져 있지 않아 읽지 않는다
        if (request.responseCode == 401)
        {
            AuthJwtToken.Clear();
            return new ServerRequestException(401, null, "로그인이 만료되었습니다. 다시 로그인해 주세요.");
        }

        ErrorResponse error = null;
        try
        {
            error = JsonConvert.DeserializeObject<ErrorResponse>(request.downloadHandler.text);
        }
        catch (JsonException)
        {
            // 서버의 에러 형식이 아니다. 상태 코드만으로 처리한다
        }

        string message = string.IsNullOrEmpty(error?.message)
            ? $"서버 오류가 발생했습니다. ({request.responseCode})"
            : error.message;
        return new ServerRequestException(request.responseCode, error?.code, message);
    }

    private static T Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (JsonException e)
        {
            Debug.LogError($"[AuthorizedServerClient] 응답 파싱 실패 ({typeof(T).Name}): {e.Message}\n{json}");
            throw new ServerRequestException(200, null, "서버 응답을 읽을 수 없습니다.");
        }
    }

    // 서버 에러 응답 본문 {message, code}. code는 없을 수 있다
    private class ErrorResponse
    {
        public string message;
        public string code;
    }
}
