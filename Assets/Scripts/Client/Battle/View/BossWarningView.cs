using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스전 시작 전에 화면을 덮는 경고 연출. 보여주기만 하고 언제 보여줄지는 모른다
// 경고 띠가 열리며 무늬가 옆으로 흐르고, 경고 문구와 보스 이름이 차례로 나타났다가 띠가 닫히며 사라진다
// 시각은 모두 1배속 기준이고, 배속만큼 빨리 지나간다
public class BossWarningView : MonoBehaviour
{
    private const float FadeInDuration = 0.25f; // 비네트가 나타나는 시간

    private const float BandOpenStart = 0.05f;
    private const float BandOpenDuration = 0.2f;
    private const Ease BandOpenEase = Ease.OutCubic;
    private const float BandScrollSpeed = 300f; // 띠 무늬가 왼쪽으로 흐르는 속도(초당 캔버스 단위)

    private const float WarningFadeDuration = 0.6f; // 띠가 다 열린 뒤 경고 문구가 나타나는 시간
    private const Ease WarningFadeEase = Ease.OutExpo;
    private const float WarningBlinkInterval = 0.3f; // 다 나타난 뒤 흐려졌다 돌아오는 각각의 시간
    private const float WarningBlinkMinAlpha = 0.2f;

    private const float NameStart = 0.35f;
    private const float NameDuration = 0.25f;
    private const float NameStartScale = 2.2f; // 이 크기에서 1배로 내려찍듯 나타난다
    private const Ease NameEase = Ease.OutCubic;

    private const float CloseDuration = 0.25f; // 끝나기 전 이 시간 동안 띠가 닫히며 모두 흐려진다

    private CanvasGroup canvasGroup;
    private RectTransform rectBand;
    private Image imgBandPattern;
    private TMP_Text warningText;
    private TMP_Text bossNameText;
    private float bandPatternBaseX;
    private Tween tween;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref canvasGroup);
        rectBand = GameUtil.Bind<RectTransform>(gameObject, "Band");
        imgBandPattern = GameUtil.Bind<Image>(gameObject, "Band/BandPattern");
        warningText = GameUtil.Bind<TMP_Text>(gameObject, "WarningText");
        bossNameText = GameUtil.Bind<TMP_Text>(gameObject, "BossNameText");
        bandPatternBaseX = imgBandPattern.rectTransform.anchoredPosition.x;

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }

    // duration(1배속 기준)이 지나면 사라진다. speed는 전투 배속이고, 배속만큼 빨리 끝난다
    public void Play(string bossName, float duration, float speed)
    {
        tween?.Kill();

        bossNameText.text = bossName;
        gameObject.SetActive(true);
        SoundManager.Instance.PlaySfx(GameConstants.Sounds.BossWarning);

        float tileWidth = GetBandTileWidth();
        ApplyTime(0f, duration, tileWidth);
        tween = DOVirtual.Float(0f, duration, duration, time => ApplyTime(time, duration, tileWidth))
            .SetEase(Ease.Linear)
            .OnComplete(() => gameObject.SetActive(false));
        tween.timeScale = speed;
    }

    // Tiled 이미지는 테두리를 뺀 가운데 부분을 반복하므로, 그 너비만큼 옮기면 무늬가 제자리로 돌아온다
    private float GetBandTileWidth()
    {
        Sprite sprite = imgBandPattern.sprite;
        float innerWidth = sprite.rect.width - sprite.border.x - sprite.border.z;
        return innerWidth / imgBandPattern.pixelsPerUnit;
    }

    // 시안과 같은 식으로 time 시점의 모습을 바로 계산한다
    private void ApplyTime(float time, float duration, float tileWidth)
    {
        float closeStart = duration - CloseDuration;
        float close = 0f;
        if (time >= closeStart)
        {
            close = DOVirtual.EasedValue(0f, 1f, (time - closeStart) / CloseDuration, Ease.InQuad);
        }

        canvasGroup.alpha = Mathf.Clamp01(time / FadeInDuration) * (1f - close);

        // 띠
        float open = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01((time - BandOpenStart) / BandOpenDuration), BandOpenEase);
        rectBand.localScale = new Vector3(1f, open * (1f - close), 1f);
        RectTransform rectPattern = imgBandPattern.rectTransform;
        rectPattern.anchoredPosition = new Vector2(bandPatternBaseX - BandScrollSpeed * time % tileWidth, rectPattern.anchoredPosition.y);

        // 경고 문구
        warningText.alpha = GetWarningAlpha(time - (BandOpenStart + BandOpenDuration)) * (1f - close);
        warningText.rectTransform.localScale = new Vector3(1f, 1f - close, 1f);

        // 보스 이름
        float nameTime = time - NameStart;
        float nameProgress = Mathf.Clamp01(nameTime / NameDuration);
        float nameScale = DOVirtual.EasedValue(NameStartScale, 1f, nameProgress, NameEase);
        bossNameText.rectTransform.localScale = Vector3.one * nameScale;
        bossNameText.alpha = nameTime < 0f ? 0f : Mathf.Clamp01(nameTime / NameDuration * 1.5f) * (1f - close);
    }

    // 띠가 다 열린 시점부터 흐린 상태에서 나타나고, 그 뒤로는 흐려졌다 돌아오기를 반복한다
    private float GetWarningAlpha(float time)
    {
        if (time < 0f)
            return 0f;

        if (time < WarningFadeDuration)
            return DOVirtual.EasedValue(0f, 1f, time / WarningFadeDuration, WarningFadeEase);

        float phase = (time - WarningFadeDuration) / WarningBlinkInterval % 2f;
        return phase < 1f
            ? DOVirtual.EasedValue(1f, WarningBlinkMinAlpha, phase, Ease.InOutSine)
            : DOVirtual.EasedValue(WarningBlinkMinAlpha, 1f, phase - 1f, Ease.InOutSine);
    }
}
