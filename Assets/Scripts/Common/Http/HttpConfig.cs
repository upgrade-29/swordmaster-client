using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class HttpConstants
{
    public const int DefaultTimeoutSeconds = 30; // 응답을 기다리는 최대 시간(초)
    public const string JsonContentType = "application/json";
}

/// <summary>
/// 프로젝트 전역 HTTP 설정입니다. 지정하지 않은 값은 HttpConstants의 기본값을 사용합니다.
/// </summary>
public static class HttpConfig
{
    private static string baseUrl = string.Empty;

    private static int timeoutSeconds = HttpConstants.DefaultTimeoutSeconds;
    public static int TimeoutSeconds => timeoutSeconds;

    private static readonly Dictionary<string, string> dicHeader = new Dictionary<string, string>();
    public static IReadOnlyDictionary<string, string> Headers => dicHeader;

    // 외부에서 직렬화 설정을 바꾸지 못하도록 숨기고, SerializeJson과 DeserializeJson으로만 사용합니다.
    private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    // 도메인 리로드를 끈 에디터에서 이전 플레이의 설정이 남지 않도록 플레이를 시작할 때 초기화합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        baseUrl = string.Empty;
        timeoutSeconds = HttpConstants.DefaultTimeoutSeconds;
        dicHeader.Clear();
    }

    public static void SetBaseUrl(string baseUrl)
    {
        HttpConfig.baseUrl = baseUrl;
    }

    public static void SetTimeoutSeconds(int timeoutSeconds)
    {
        HttpConfig.timeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// 모든 요청에 붙일 헤더를 지정합니다. 헤더를 지우려면 RemoveHeader를 사용합니다.
    /// </summary>
    public static void SetHeader(string key, string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), $"헤더 {key}의 값이 null입니다.");
        }

        dicHeader[key] = value;
    }

    public static void RemoveHeader(string key)
    {
        dicHeader.Remove(key);
    }

    public static string GetResolvedUrl(string pathOrUrl)
    {
        if (IsAbsoluteHttpUrl(pathOrUrl))
        {
            return pathOrUrl;
        }

        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException($"상대 경로 {pathOrUrl}를 요청하려면 {nameof(HttpConfig)}.{nameof(SetBaseUrl)}을 먼저 호출해야 합니다.");
        }

        return $"{baseUrl.TrimEnd('/')}/{pathOrUrl.TrimStart('/')}";
    }

    public static string SerializeJson(object value)
    {
        return JsonConvert.SerializeObject(value, jsonSettings);
    }

    public static T DeserializeJson<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text, jsonSettings);
    }

    // Uri.IsWellFormedUriString은 공백처럼 인코딩되지 않은 문자가 있으면 false를 반환하므로 스킴으로 판별합니다.
    private static bool IsAbsoluteHttpUrl(string url)
    {
        return url.StartsWith($"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith($"{Uri.UriSchemeHttps}{Uri.SchemeDelimiter}", StringComparison.OrdinalIgnoreCase);
    }
}
