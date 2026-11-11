using DG.Tweening;
using UnityEngine;

// DOTween이 한 번에 돌릴 수 있는 트윈 수. 어느 씬에서 플레이를 시작하든 첫 씬을 읽기 전에 정한다
// 기본값(트윈 200, 시퀀스 50)을 넘으면 DOTween이 실행 중에 용량을 늘리며 경고를 남기고, 늘리는 순간 잠깐 끊길 수 있다
// 배틀에서 적이 한꺼번에 쓰러지면 동전마다 시퀀스가 하나씩 돌아서 시퀀스가 많이 필요하다
public static class TweenSettings
{
    public const int TweenersCapacity = 1000;
    public const int SequencesCapacity = 300;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyCapacity()
    {
        DOTween.SetTweensCapacity(TweenersCapacity, SequencesCapacity);
    }
}
