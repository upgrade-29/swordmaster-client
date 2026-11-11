using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// HttpRequestInfo&lt;T&gt;를 만드는 메서드 모음입니다. API 메서드에서 응답 타입을 지정해서 호출하고, 만든 요청은 HttpRequest.Request로 전송합니다.
/// </summary>
public static class HttpRequestFactory
{
    /// <param name="query">값이 null인 항목은 제외합니다. bool은 소문자로 보내고, enum과 DateTime은 서버가 기대하는 형식으로 직접 변환해서 넣습니다.</param>
    /// <param name="silentStatusCodes">서버가 이 상태 코드로 응답하면 실패 로그를 남기지 않습니다. 콜백에는 그대로 에러로 전달합니다.</param>
    public static HttpRequestInfo<T> Get<T>(string path, IReadOnlyDictionary<string, object> query = null, IReadOnlyDictionary<string, string> headers = null, IReadOnlyList<int> silentStatusCodes = null) where T : class
    {
        var pathAndQuery = $"{path}{GetQueryString(path, query)}";
        return new HttpRequestInfo<T>(UnityWebRequest.kHttpVerbGET, pathAndQuery, null, headers, silentStatusCodes);
    }

    public static HttpRequestInfo<T> Post<T>(string path, object body = null, IReadOnlyDictionary<string, string> headers = null, IReadOnlyList<int> silentStatusCodes = null) where T : class
    {
        return new HttpRequestInfo<T>(UnityWebRequest.kHttpVerbPOST, path, GetJsonBytes(body), headers, silentStatusCodes);
    }

    public static HttpRequestInfo<T> Put<T>(string path, object body = null, IReadOnlyDictionary<string, string> headers = null, IReadOnlyList<int> silentStatusCodes = null) where T : class
    {
        return new HttpRequestInfo<T>(UnityWebRequest.kHttpVerbPUT, path, GetJsonBytes(body), headers, silentStatusCodes);
    }

    public static HttpRequestInfo<T> Delete<T>(string path, IReadOnlyDictionary<string, string> headers = null, IReadOnlyList<int> silentStatusCodes = null) where T : class
    {
        return new HttpRequestInfo<T>(UnityWebRequest.kHttpVerbDELETE, path, null, headers, silentStatusCodes);
    }

    private static byte[] GetJsonBytes(object body)
    {
        if (body == null)
        {
            return null;
        }

        return Encoding.UTF8.GetBytes(HttpConfig.SerializeJson(body));
    }

    private static string GetQueryString(string path, IReadOnlyDictionary<string, object> query)
    {
        if (query == null || query.Count == 0)
        {
            return string.Empty;
        }

        var listQueryPair = query
            .Where(x => x.Value != null)
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(GetQueryValue(x.Value))}")
            .ToList();
        if (listQueryPair.Count == 0)
        {
            return string.Empty;
        }

        // 경로에 이미 쿼리가 있으면 이어서 붙입니다.
        var separator = path.Contains("?")
            ? "&"
            : "?";
        return $"{separator}{string.Join("&", listQueryPair)}";
    }

    // bool.ToString()은 "True"를 반환하므로, 대부분의 서버가 기대하는 소문자로 바꿉니다.
    private static string GetQueryValue(object value)
    {
        if (value is bool boolValue)
        {
            return boolValue
                ? "true"
                : "false";
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
