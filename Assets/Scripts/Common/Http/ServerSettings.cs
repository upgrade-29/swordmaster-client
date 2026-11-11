using UnityEngine;

// 접속할 서버 주소와 응답을 기다리는 시간. 어느 씬에서 플레이를 시작하든 첫 씬을 읽기 전에 HttpConfig에 넣는다
public static class ServerSettings
{
    public const string BaseUrl = "https://api.upgrade-29.com";
    public const int TimeoutSeconds = 10;

    // HttpConfig가 SubsystemRegistration에서 설정을 초기화하므로, 그 뒤인 BeforeSceneLoad에서 넣는다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyToHttpConfig()
    {
        HttpConfig.SetBaseUrl(BaseUrl);
        HttpConfig.SetTimeoutSeconds(TimeoutSeconds);
    }
}
