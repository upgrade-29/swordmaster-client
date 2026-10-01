using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// ILoginService의 HTTP 구현. POST {baseUrl}/api/auth/login 으로 요청하고, 응답을 LoginResult로 바꿔 돌려준다
public class HttpLoginService : ILoginService
{
    private const string LoginPath = "api/auth/login";

    private readonly string loginUrl;
    private readonly int timeoutSeconds;

    public HttpLoginService(string baseUrl, int timeoutSeconds)
    {
        loginUrl = $"{baseUrl.TrimEnd('/')}/{LoginPath}";
        this.timeoutSeconds = timeoutSeconds;
    }

    public async Task<LoginResult> LoginAsync(string loginId, string password)
    {
        string body = JsonConvert.SerializeObject(new LoginRequest(loginId, password));

        using (var request = new UnityWebRequest(loginUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;

            await SendAsync(request);

            return ToLoginResult(request);
        }
    }

    private static Task SendAsync(UnityWebRequest request)
    {
        var tcs = new TaskCompletionSource<bool>();
        request.SendWebRequest().completed += _ => tcs.SetResult(true);
        return tcs.Task;
    }

    private static LoginResult ToLoginResult(UnityWebRequest request)
    {
        switch (request.result)
        {
            case UnityWebRequest.Result.Success:
                return ParseSuccess(request.downloadHandler.text);

            case UnityWebRequest.Result.ConnectionError:
                // 타임아웃도 ConnectionError로 오기 때문에 에러 문구로 구분한다
                if (request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return LoginResult.Failure(LoginFailureKind.Timeout, "서버 응답 시간이 초과되었습니다.");
                }
                return LoginResult.Failure(LoginFailureKind.NetworkError, "서버에 연결할 수 없습니다.");

            case UnityWebRequest.Result.ProtocolError:
                if (request.responseCode == 401)
                {
                    return LoginResult.Failure(LoginFailureKind.InvalidCredentials, "아이디 또는 비밀번호가 틀렸습니다.");
                }
                return LoginResult.Failure(LoginFailureKind.ServerError, $"서버 오류가 발생했습니다. ({request.responseCode})");

            default:
                return LoginResult.Failure(LoginFailureKind.ServerError, $"알 수 없는 오류가 발생했습니다. ({request.error})");
        }
    }

    private static LoginResult ParseSuccess(string json)
    {
        LoginResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<LoginResponse>(json);
        }
        catch (JsonException e)
        {
            Debug.LogError($"[HttpLoginService] 응답 파싱 실패: {e.Message}\n{json}");
            return LoginResult.Failure(LoginFailureKind.ServerError, "서버 응답을 읽을 수 없습니다.");
        }

        if (response == null || string.IsNullOrEmpty(response.accessToken))
        {
            Debug.LogError($"[HttpLoginService] 응답에 accessToken이 없음\n{json}");
            return LoginResult.Failure(LoginFailureKind.ServerError, "서버 응답을 읽을 수 없습니다.");
        }

        return LoginResult.Success(response.accessToken);
    }
}
