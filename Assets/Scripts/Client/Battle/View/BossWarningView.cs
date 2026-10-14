using DG.Tweening;
using TMPro;
using UnityEngine;

// 보스전 시작 전에 화면을 덮는 경고 연출. 보여주기만 하고 언제 보여줄지는 모른다
public class BossWarningView : MonoBehaviour
{
    private const float FadeDuration = 0.25f;
    private const float BlinkInterval = 0.3f;

    private CanvasGroup canvasGroup;
    private TMP_Text warningText;
    private TMP_Text bossNameText;
    private Sequence sequence;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref canvasGroup);
        warningText = GameUtil.Bind<TMP_Text>(gameObject, "WarningText");
        bossNameText = GameUtil.Bind<TMP_Text>(gameObject, "BossNameText");

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }

    // 나타나서 깜빡이다가 duration이 지나면 사라진다
    public void Play(string bossName, float duration)
    {
        sequence?.Kill();

        bossNameText.text = bossName;
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        warningText.alpha = 1f;

        sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(1f, FadeDuration));
        sequence.Join(warningText.DOFade(0.2f, BlinkInterval).SetLoops(Mathf.Max(2, Mathf.FloorToInt(duration / BlinkInterval)), LoopType.Yoyo));
        sequence.Insert(duration - FadeDuration, canvasGroup.DOFade(0f, FadeDuration));
        sequence.OnComplete(() => gameObject.SetActive(false));
    }
}
