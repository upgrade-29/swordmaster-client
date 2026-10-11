using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// IAuthService의 HTTP 구현. {baseUrl}/api/auth/... 로 JSON POST 요청을 보내고, 실패는 AuthFailureException으로 던진다
public class HttpAuthService : IAuthService
{
    private const string LoginPath = "api/auth/login";
    private const string SignupPath = "api/auth/signup";

    private readonly string baseUrl;
    private readonly int timeoutSeconds;

    public HttpAuthService(string baseUrl, int timeoutSeconds)
    {
        this.baseUrl = baseUrl.TrimEnd('/');
        this.timeoutSeconds = timeoutSeconds;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        string json = await PostAsync(LoginPath, new LoginRequest(email, password));

        LoginResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<LoginResponse>(json);
        }
        catch (JsonException e)
        {
            Debug.LogError($"[HttpAuthService] 로그인 응답 파싱 실패: {e.Message}\n{json}");
            throw new AuthFailureException(AuthFailureReason.ServerError, "서버 응답을 읽을 수 없습니다.");
        }

        if (response == null || string.IsNullOrEmpty(response.accessToken))
        {
            Debug.LogError($"[HttpAuthService] 로그인 응답에 accessToken이 없음\n{json}");
            throw new AuthFailureException(AuthFailureReason.ServerError, "서버 응답을 읽을 수 없습니다.");
        }

        return response;
    }

    public async Task SignupAsync(string email, string password)
    {
        // 성공 응답은 200만 오므로 본문은 읽지 않는다
        await PostAsync(SignupPath, new SignupRequest(email, password));
    }

    // 성공하면 응답 본문을 돌려주고, 실패하면 AuthFailureException을 던진다
    private async Task<string> PostAsync(string path, object requestBody)
    {
        string body = JsonConvert.SerializeObject(requestBody);

        using (var request = new UnityWebRequest($"{baseUrl}/{path}", UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;

            await SendAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw ToException(request);
            }

            return request.downloadHandler.text;
        }
    }

    private static Task SendAsync(UnityWebRequest request)
    {
        var tcs = new TaskCompletionSource<bool>();
        request.SendWebRequest().completed += _ => tcs.SetResult(true);
        return tcs.Task;
    }

    private static AuthFailureException ToException(UnityWebRequest request)
    {
        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                // 타임아웃도 ConnectionError로 오기 때문에 에러 문구로 구분한다
                if (request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new AuthFailureException(AuthFailureReason.Timeout, "서버 응답 시간이 초과되었습니다.");
                }
                return new AuthFailureException(AuthFailureReason.NetworkError, "서버에 연결할 수 없습니다.");

            case UnityWebRequest.Result.ProtocolError:
                if (request.responseCode == 401)
                {
                    return new AuthFailureException(AuthFailureReason.InvalidCredentials, "아이디 또는 비밀번호가 틀렸습니다.");
                }
                if (request.responseCode == 409)
                {
                    return new AuthFailureException(AuthFailureReason.DuplicateId, "이미 사용 중인 아이디입니다.");
                }
                return new AuthFailureException(AuthFailureReason.ServerError, $"서버 오류가 발생했습니다. ({request.responseCode})");

            default:
                return new AuthFailureException(AuthFailureReason.ServerError, $"알 수 없는 오류가 발생했습니다. ({request.error})");
        }
    }
}
