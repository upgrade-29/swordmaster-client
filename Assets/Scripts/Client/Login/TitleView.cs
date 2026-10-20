using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인 씬에 처음 뜨는 타이틀 화면. 화면 전체를 덮고, 아무 곳이나 누르면(모바일은 터치) 입력을 전달만 한다.
// 누른 뒤 기존/신규 유저 판별과 화면 전환은 LoginController가 맡는다
public class TitleView : MonoBehaviour
{
    private const float BlinkSpeed = 2f; // "터치하여 시작하기" 문구가 깜빡이는 속도
    private const float LogoFloatHeight = 12f;    // 로고가 기본 위치에서 위아래로 움직이는 거리
    private const float LogoFloatDuration = 1.2f; // 로고가 아래에서 위까지 가는 시간(초). 왕복은 두 배
    private const float ShineMoveDuration = 0.8f; // 빛줄기가 로고를 가로지르는 시간(초)
    private const float ShineInterval = 2f;       // 빛줄기가 지나간 뒤 다음 빛줄기까지 기다리는 시간(초)

    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private Button touchButton; // 화면 전체를 덮는 버튼이라 마우스 클릭과 터치를 같이 받는다
    [ReadOnly] [SerializeField] private TextMeshProUGUI touchText;
    [ReadOnly] [SerializeField] private GameObject objDimmed; // 로그인 화면 뒤에 깔릴 때 타이틀을 어둡게 덮는다
    [ReadOnly] [SerializeField] private GameObject objLoading; // 리소스를 읽는 동안 터치 문구 자리에 보여 준다
    [ReadOnly] [SerializeField] private Image imgLoadingFill;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLoading;
    [ReadOnly] [SerializeField] private RectTransform rectLogo;
    [ReadOnly] [SerializeField] private RectTransform rectLogoShine; // 로고의 Mask 안에서만 보이는 흰 빛줄기

    public event Action OnClickTouchEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "TitlePanel").gameObject;
        touchButton = GameUtil.Bind<Button>(transform, "TitlePanel");
        touchText = GameUtil.Bind<TextMeshProUGUI>(transform, "TitlePanel/TouchText");
        objDimmed = GameUtil.Bind<RectTransform>(transform, "TitlePanel/Dimmed").gameObject;
        objLoading = GameUtil.Bind<RectTransform>(transform, "TitlePanel/Loading").gameObject;
        imgLoadingFill = GameUtil.Bind<Image>(transform, "TitlePanel/Loading/Bar/Fill");
        txtLoading = GameUtil.Bind<TextMeshProUGUI>(transform, "TitlePanel/Loading/Label");
        rectLogo = GameUtil.Bind<RectTransform>(transform, "TitlePanel/TitleLogo");
        rectLogoShine = GameUtil.Bind<RectTransform>(transform, "TitlePanel/TitleLogo/Shine");

        touchButton.onClick.AddListener(OnClickTouch);
    }

    private void Start()
    {
        PlayLogoAnimation();
    }

    private void Update()
    {
        if (objRoot.activeInHierarchy == false || objDimmed.activeSelf)
        {
            return;
        }

        Color color = touchText.color;
        color.a = Mathf.PingPong(Time.unscaledTime * BlinkSpeed, 1f);
        touchText.color = color;
    }

    private void OnDestroy()
    {
        if (touchButton != null)
        {
            touchButton.onClick.RemoveListener(OnClickTouch);
        }
    }

    public void Show()
    {
        objRoot.SetActive(true);
        touchButton.interactable = true;
        touchText.gameObject.SetActive(true);
        objLoading.SetActive(false);
        objDimmed.SetActive(false);
    }

    // 리소스를 다 읽을 때까지 터치 문구 대신 진행 바를 보여 주고, 누르지 못하게 막는다
    public void ShowLoading()
    {
        objRoot.SetActive(true);
        touchButton.interactable = false;
        touchText.gameObject.SetActive(false);
        objLoading.SetActive(true);
        objDimmed.SetActive(false);
        SetLoadingProgress(0f);
    }

    public void SetLoadingProgress(float progress)
    {
        imgLoadingFill.fillAmount = progress;
        txtLoading.text = $"불러오는 중 {Mathf.FloorToInt(progress * 100f).ToString()}%";
    }

    // 타이틀을 끄지 않고 로그인 화면의 배경으로 남긴다. 어둡게 덮고, 터치 문구를 숨기고, 더 눌리지 않게 막는다
    public void ShowDimmed()
    {
        objRoot.SetActive(true);
        touchButton.interactable = false;
        touchText.gameObject.SetActive(false);
        objLoading.SetActive(false);
        objDimmed.SetActive(true);
    }

    // 판별 요청을 기다리는 동안 다시 눌리지 않게 막는다
    public void SetInteractable(bool interactable)
    {
        touchButton.interactable = interactable;
    }

    // 로고는 사인 곡선처럼 위아래로 움직이고, 흰 빛줄기가 왼쪽에서 오른쪽으로 주기적으로 지나간다
    private void PlayLogoAnimation()
    {
        float baseY = rectLogo.anchoredPosition.y;
        rectLogo.anchoredPosition = new Vector2(rectLogo.anchoredPosition.x, baseY - LogoFloatHeight);
        rectLogo.DOAnchorPosY(baseY + LogoFloatHeight, LogoFloatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

        // 빛줄기가 기울어져 있어도 로고 밖에서 시작하고 끝나도록 빛줄기 높이의 절반만큼 더 간다
        float shineX = rectLogo.rect.width * 0.5f + rectLogoShine.rect.height * 0.5f;
        rectLogoShine.anchoredPosition = new Vector2(-shineX, 0f);
        DOTween.Sequence()
            .Append(rectLogoShine.DOAnchorPosX(shineX, ShineMoveDuration).SetEase(Ease.InOutQuad))
            .AppendInterval(ShineInterval)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject);
    }

    private void OnClickTouch()
    {
        SoundManager.Instance.PlaySfx(GameConstants.Sounds.TitleTouch);
        OnClickTouchEvent.Invoke();
    }
}
