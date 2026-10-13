using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HttpRequestFactory로 만든 HttpRequestInfo를 전송하고 응답을 DTO로 변환합니다. 에러는 콜백보다 먼저 HttpErrorHandler가 처리합니다.
/// 처음 요청할 때 싱글턴 오브젝트를 만들고 씬이 바뀌어도 유지합니다.
/// 요청을 시작하면 콜백은 성공과 실패에 관계없이 한 번 호출됩니다. 단, 싱글턴 오브젝트가 파괴되어 코루틴이 멈추면 호출되지 않습니다.
/// </summary>
public class HttpRequest : SingletonMonoBehaviour<HttpRequest>
{
    private const string ConnectionFailedMessage = "서버에 연결할 수 없습니다.";
    private const string TimeoutMessage = "서버 응답 시간이 초과되었습니다.";
    private const string ParseFailedMessage = "서버 응답을 읽을 수 없습니다.";

    // showLoading이 true인 요청 중에서 아직 끝나지 않은 요청의 개수입니다.
    private static int loadingRequestCount;

    // 로딩 인디케이터 표시용입니다. 진행 중인 요청이 0개에서 1개가 될 때 Start, 다시 0개가 될 때 End가 호출됩니다.
    public static event Action OnLoadingStartEvent = delegate { };
    public static event Action OnLoadingEndEvent = delegate { };

    // 응답 헤더 확인처럼 모든 응답에 공통으로 적용할 처리를 등록합니다. 응답마다 새로 만든 헤더 딕셔너리를 전달합니다.
    public static event Action<IReadOnlyDictionary<string, string>> OnResponseHeadersReceivedEvent = delegate { };

    // 도메인 리로드를 끈 에디터에서 이전 플레이의 상태가 남지 않도록 플레이를 시작할 때 초기화합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        loadingRequestCount = 0;
        OnLoadingStartEvent = delegate { };
        OnLoadingEndEvent = delegate { };
        OnResponseHeadersReceivedEvent = delegate { };
    }

    // 씬이 바뀌어도 진행 중인 요청의 코루틴이 멈추지 않도록 유지합니다.
    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);
    }

    public static Coroutine Request<T>(HttpRequestInfo<T> requestInfo, Action<HttpError, T> callback, bool showLoading = true) where T : class
    {
        return Instance.StartCoroutine(RequestCoroutine(requestInfo, callback, showLoading));
    }

    private static IEnumerator RequestCoroutine<T>(HttpRequestInfo<T> requestInfo, Action<HttpError, T> callback, bool showLoading) where T : class
    {
        StartLoading(showLoading);

        var request = CreateRequest(requestInfo, out HttpError createError);
        if (request == null)
        {
            CompleteRequest(requestInfo, null, createError, null, callback, showLoading);
            yield break;
        }

        using (request)
        {
            yield return request.SendWebRequest();

            var responseHeaders = request.GetResponseHeaders() ?? new Dictionary<string, string>();
            InvokeEventSafely(() => OnResponseHeadersReceivedEvent.Invoke(responseHeaders));

            var (error, result) = ParseResponse<T>(request);
            CompleteRequest(requestInfo, request, error, result, callback, showLoading);
        }
    }

    // BaseUrl 미설정, 허용되지 않는 헤더처럼 요청을 만들 수 없는 경우 Client 에러로 바꿉니다.
    private static UnityWebRequest CreateRequest<T>(HttpRequestInfo<T> requestInfo, out HttpError error) where T : class
    {
        error = null;
        try
        {
            return requestInfo.CreateUnityWebRequest();
        }
        catch (Exception e)
        {
            error = new HttpError(HttpErrorType.Client, 0, null, e.Message);
            return null;
        }
    }

    // 요청이 어떤 경로로 끝나든 이 메서드를 한 번만 거치게 해서, 로딩 종료와 콜백 호출을 보장합니다.
    private static void CompleteRequest<T>(HttpRequestInfo<T> requestInfo, UnityWebRequest request, HttpError error, T result, Action<HttpError, T> callback, bool showLoading) where T : class
    {
        EndLoading(showLoading);

        if (error != null)
        {
            if (requestInfo.IsSilentError(error) == false)
            {
                LogError(requestInfo, request, error);
            }

            error.SetHandled(InvokeErrorHandler(error));
        }

        callback?.Invoke(error, result);
    }

    private static void StartLoading(bool showLoading)
    {
        if (showLoading == false)
        {
            return;
        }

        loadingRequestCount++;
        if (loadingRequestCount == 1)
        {
            InvokeEventSafely(OnLoadingStartEvent);
        }
    }

    private static void EndLoading(bool showLoading)
    {
        if (showLoading == false)
        {
            return;
        }

        loadingRequestCount--;
        if (loadingRequestCount == 0)
        {
            InvokeEventSafely(OnLoadingEndEvent);
        }
    }

    // 구독자에서 발생한 예외가 요청 처리를 중단시키지 않도록 로그만 남깁니다.
    private static void InvokeEventSafely(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // 핸들러에서 발생한 예외가 콜백 호출을 막지 않도록, 예외가 나면 처리하지 않은 것으로 봅니다.
    private static bool InvokeErrorHandler(HttpError error)
    {
        try
        {
            return HttpErrorHandler.OnError(error);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    private static (HttpError error, T result) ParseResponse<T>(UnityWebRequest request) where T : class
    {
        HttpError error = null;
        T result = null;
        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
                error = new HttpError(HttpErrorType.Network, 0, null, GetNetworkErrorMessage(request));
                break;
            case UnityWebRequest.Result.ProtocolError:
                error = ParseServerError(request);
                break;
            case UnityWebRequest.Result.Success:
                (error, result) = ParseSuccessResponse<T>(request);
                break;
        }

        return (error, result);
    }

    // 본문이 없는 성공 응답(회원가입 201, 진행 중인 세션이 없을 때의 200)은 결과 없이 성공으로 처리합니다.
    private static (HttpError error, T result) ParseSuccessResponse<T>(UnityWebRequest request) where T : class
    {
        var text = request.downloadHandler.text;
        if (string.IsNullOrEmpty(text))
        {
            return (null, null);
        }

        var result = Deserialize<T>(text);
        if (result == null)
        {
            return (new HttpError(HttpErrorType.Parse, (int)request.responseCode, null, ParseFailedMessage), null);
        }

        return (null, result);
    }

    // 서버 에러 본문은 { message, code }이며 code는 없을 수 있습니다. 토큰 필터의 401처럼 이 형식이 아닌 본문도 있습니다.
    private static HttpError ParseServerError(UnityWebRequest request)
    {
        var errorResponse = Deserialize<ErrorResponse>(request.downloadHandler.text);
        var message = string.IsNullOrEmpty(errorResponse?.message)
            ? $"서버 오류가 발생했습니다. ({request.responseCode.ToString()})"
            : errorResponse.message;
        return new HttpError(HttpErrorType.Server, (int)request.responseCode, errorResponse?.code, message);
    }

    // 타임아웃도 ConnectionError로 오기 때문에 에러 문구로 구분합니다.
    private static string GetNetworkErrorMessage(UnityWebRequest request)
    {
        var isTimeout = request.error != null
            && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
        return isTimeout
            ? TimeoutMessage
            : ConnectionFailedMessage;
    }

    private static TResult Deserialize<TResult>(string text) where TResult : class
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // 커스텀 컨버터나 DTO 생성자가 던지는 예외도 파싱 실패로 처리하기 위해 모든 예외를 받습니다.
        try
        {
            return HttpConfig.DeserializeJson<TResult>(text);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void LogError<T>(HttpRequestInfo<T> requestInfo, UnityWebRequest request, HttpError error) where T : class
    {
        // 요청을 만들지 못한 경우에는 BaseUrl과 합친 URL이 없으므로 경로를 출력합니다.
        var url = request?.url ?? requestInfo.Path;
        var requestLog = $"[HttpRequest] {requestInfo.Method} {url} failed. {error} ({request?.error})";

        // 본문에는 토큰 같은 민감한 정보가 있을 수 있으므로 개발 빌드에서만 출력합니다.
        if (Debug.isDebugBuild == false)
        {
            Debug.LogWarning(requestLog);
            return;
        }

        var requestBodyLog = $"RequestBody: {requestInfo.GetBodyText()}";
        var responseLog = $"ResponseCode: {(request?.responseCode ?? 0).ToString()}, ResponseBody: {request?.downloadHandler?.text}";
        Debug.LogWarning($"{requestLog}\n{requestBodyLog}\n{responseLog}");
    }

    // 서버 에러 응답 본문입니다.
    private class ErrorResponse
    {
        public string message;
        public string code;
    }
}
