using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투가 끝날 때(보스 처치, 패배) 화면을 어둡게 덮고, 시간 초과면 TIME OVER 문구를 띄운다. 보여주기만 하고 언제 보여줄지는 모른다
// 입력은 막지 않는다. 결과 팝업은 이 위에 뜬다
public class BattleDefeatView : MonoBehaviour
{
    public const float FadeDuration = 0.4f; // 1배속 기준
    private const float DimAlpha = 0.45f;

    private Image dimImage;
    private TMP_Text timeOverText;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref dimImage);
        timeOverText = GameUtil.Bind<TMP_Text>(gameObject, "TimeOverText");

        Hide();
    }

    private void OnDestroy()
    {
        dimImage.DOKill();
        timeOverText.DOKill();
    }

    /// <summary>
    /// 화면을 서서히 어둡게 한다. speed는 전투 배속이고, 배속만큼 빨리 끝난다.
    /// </summary>
    public void ShowDim(float speed)
    {
        dimImage.DOKill();
        Tween tween = dimImage.DOFade(DimAlpha, FadeDuration);
        tween.timeScale = speed;
    }

    /// <summary>
    /// 화면을 어둡게 하면서 TIME OVER 문구를 함께 서서히 띄운다.
    /// </summary>
    public void ShowTimeOver(float speed)
    {
        ShowDim(speed);

        timeOverText.DOKill();
        Tween tween = timeOverText.DOFade(1f, FadeDuration);
        tween.timeScale = speed;
    }

    /// <summary>
    /// 재도전처럼 다시 전투를 시작할 때 바로 걷어 낸다.
    /// </summary>
    public void Hide()
    {
        dimImage.DOKill();
        timeOverText.DOKill();
        dimImage.color = new Color(dimImage.color.r, dimImage.color.g, dimImage.color.b, 0f);
        timeOverText.alpha = 0f;
    }
}
