using System;
using DG.Tweening;
using UnityEngine;

// 화면 전체를 덮는 검은 막. 씬을 나갈 때 서서히 어두워지게 한다
// 어두워지는 동안 뒤의 버튼이 눌리지 않게 입력을 막는다
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Tween tween;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref canvasGroup);

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }

    public void FadeOut(float duration, Action onComplete)
    {
        tween?.Kill();

        canvasGroup.blocksRaycasts = true;
        tween = canvasGroup.DOFade(1f, duration).OnComplete(() => onComplete?.Invoke());
    }
}
