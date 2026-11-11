using UnityEngine;

// 화면을 그리는 목표 프레임. 어느 씬에서 플레이를 시작하든 첫 씬을 읽기 전에 정한다
// 모바일은 vSyncCount를 무시하고, targetFrameRate를 지정하지 않으면 30fps로 돈다
public static class FrameRateSettings
{
    public const int TargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyTargetFrameRate()
    {
        Application.targetFrameRate = TargetFrameRate;
    }
}
