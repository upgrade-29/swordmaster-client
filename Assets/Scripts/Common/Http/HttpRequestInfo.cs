using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// 응답 타입이 T인 요청 정보입니다. HttpRequestFactory로 만듭니다.
/// 생성한 뒤에는 바뀌지 않으며, 전송할 때마다 UnityWebRequest를 새로 만들기 때문에 같은 인스턴스를 다시 전송할 수 있습니다.
/// BaseUrl과 공통 헤더는 전송하는 시점의 값을 사용합니다.
/// </summary>
public sealed class HttpRequestInfo<T> where T : class
{
    private readonly string method;
    public string Method => method;

    private readonly string path;
    public string Path => path;

    private readonly byte[] body;

    private readonly Dictionary<string, string> dicHeader;

    // 실패해도 로그를 남기지 않을 HTTP 상태 코드입니다. 예상된 결과로 쓰는 에러(플레이어 없음 404 등)에 지정합니다.
    private readonly List<int> listSilentStatusCode;

    internal HttpRequestInfo(string method, string path, byte[] body, IReadOnlyDictionary<string, string> headers, IReadOnlyList<int> silentStatusCodes)
    {
        this.method = method;
        this.path = path;
        this.body = body;
        listSilentStatusCode = silentStatusCodes?.ToList() ?? new List<int>();

        // 호출부가 넘긴 딕셔너리를 나중에 바꿔도 영향을 받지 않도록 복사합니다.
        dicHeader = headers?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>();

        var nullValueHeader = dicHeader.FirstOrDefault(x => x.Value == null);
        if (nullValueHeader.Key != null)
        {
            throw new ArgumentNullException(nameof(headers), $"헤더 {nullValueHeader.Key}의 값이 null입니다.");
        }
    }

    public UnityWebRequest CreateUnityWebRequest()
    {
        var request = new UnityWebRequest(HttpConfig.GetResolvedUrl(path), method)
        {
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = HttpConfig.TimeoutSeconds,
        };

        // SetRequestHeader는 Unity가 막아 둔 헤더 이름을 받으면 예외를 던지므로, 실패하면 생성한 요청을 해제합니다.
        try
        {
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(body) { contentType = HttpConstants.JsonContentType };
            }

            // 요청별 헤더를 나중에 적용해서 전역 헤더를 덮어쓸 수 있게 합니다.
            foreach (var header in HttpConfig.Headers.Concat(dicHeader))
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
        }
        catch
        {
            request.Dispose();
            throw;
        }

        return request;
    }

    public bool IsSilentError(HttpError error)
    {
        return error.Type == HttpErrorType.Server && listSilentStatusCode.Contains(error.StatusCode);
    }

    public string GetBodyText()
    {
        return body == null
            ? string.Empty
            : Encoding.UTF8.GetString(body);
    }
}
