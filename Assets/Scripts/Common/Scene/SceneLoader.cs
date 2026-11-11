using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬 전환을 맡는다. 처음 호출될 때 생겨서 씬이 바뀌어도 남는다
// 어두워짐 → 로딩 씬 → 밝아짐 → 대상 씬을 비동기로 읽음 → 어두워짐 → 대상 씬을 켬 → 밝아짐 순서로 바꾼다
// 페이드와 진행 바는 실제 시간으로 센다. 일시 정지(Time.timeScale 0)나 느린 화면(DOTween.timeScale) 중에 불려도 멈추지 않게 하려고 DOTween을 쓰지 않는다
public class SceneLoader : SingletonMonoBehaviour<SceneLoader>
{
    private const float SceneReadyProgress = 0.9f; // 켜기를 미룬 씬은 다 읽어도 진행률이 여기서 멈춘다

    private CanvasGroup canvasGroupFade;
    private Coroutine corLoad;
    private string targetSceneName;
    private string pendingSceneName; // 전환하는 도중에 요청된 다른 씬. 지금 전환이 끝나면 이어서 바꾼다

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);
        CreateFade();
    }

    /// <summary>
    /// 로딩 씬을 거쳐 sceneName으로 바꾼다.
    /// 전환하는 도중에 같은 씬이 다시 요청되면(연속 클릭 등) 무시하고, 다른 씬이 요청되면(새 씬의 진입 확인, 401 등) 지금 전환이 끝난 뒤 이어서 바꾼다.
    /// </summary>
    public void Load(string sceneName)
    {
        if (corLoad != null)
        {
            if (sceneName != targetSceneName)
            {
                pendingSceneName = sceneName;
            }

            return;
        }

        targetSceneName = sceneName;
        corLoad = StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        canvasGroupFade.blocksRaycasts = true;
        yield return Fade(1f, GameConstants.SceneTransition.FadeOutDuration);

        // 로딩 씬은 바로 바꾸고, 다음 프레임에 그 화면의 Awake와 Start가 끝난 뒤 찾는다
        SceneManager.LoadScene(GameConstants.Scenes.Loading);
        yield return null;
        LoadingView loadingView = FindFirstObjectByType<LoadingView>();
        loadingView.SetProgress(0f);
        yield return Fade(0f, GameConstants.SceneTransition.FadeInDuration);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // 읽기가 빨리 끝나도 최소 표시 시간이 지나기 전에는 100%에 닿지 않게 목표를 늦춘다
        float elapsed = 0f;
        float shownProgress = 0f;
        while (shownProgress < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float loadedProgress = Mathf.Clamp01(operation.progress / SceneReadyProgress);
            float targetProgress = Mathf.Min(loadedProgress, elapsed / GameConstants.SceneTransition.MinLoadingDuration);
            shownProgress = GetFollowedProgress(shownProgress, targetProgress);
            loadingView.SetProgress(shownProgress);
            yield return null;
        }

        yield return Fade(1f, GameConstants.SceneTransition.FadeOutDuration);
        operation.allowSceneActivation = true;
        yield return operation;
        yield return Fade(0f, GameConstants.SceneTransition.FadeInDuration);

        canvasGroupFade.blocksRaycasts = false;
        corLoad = null;
        targetSceneName = null;

        if (pendingSceneName != null)
        {
            string nextSceneName = pendingSceneName;
            pendingSceneName = null;
            Load(nextSceneName);
        }
    }

    // 보이는 진행률이 목표를 부드럽게 따라가고, 목표가 100%면 거의 닿았을 때 바로 채운다
    private float GetFollowedProgress(float shownProgress, float targetProgress)
    {
        float followRatio = 1f - Mathf.Exp(-GameConstants.SceneTransition.ProgressFollowSpeed * Time.unscaledDeltaTime);
        float followedProgress = shownProgress + (targetProgress - shownProgress) * followRatio;
        if (targetProgress >= 1f && followedProgress >= GameConstants.SceneTransition.ProgressSnapThreshold)
        {
            followedProgress = 1f;
        }

        return followedProgress;
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroupFade.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroupFade.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroupFade.alpha = targetAlpha;
    }

    // 모든 씬 위를 덮는 검은 막. 전환하는 동안 뒤의 버튼이 눌리지 않게 입력도 막는다
    private void CreateFade()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = GameConstants.SceneTransition.FadeSortingOrder;
        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroupFade = gameObject.AddComponent<CanvasGroup>();
        canvasGroupFade.alpha = 0f;
        canvasGroupFade.blocksRaycasts = false;

        var objFade = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        objFade.transform.SetParent(transform, false);

        var rectFade = (RectTransform)objFade.transform;
        rectFade.anchorMin = Vector2.zero;
        rectFade.anchorMax = Vector2.one;
        rectFade.sizeDelta = Vector2.zero;
        objFade.GetComponent<Image>().color = Color.black;
    }
}
