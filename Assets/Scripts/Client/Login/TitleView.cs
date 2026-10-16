using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인 씬에 처음 뜨는 타이틀 화면. 화면 전체를 덮고, 아무 곳이나 누르면(모바일은 터치) 입력을 전달만 한다.
// 누른 뒤 기존/신규 유저 판별과 화면 전환은 LoginController가 맡는다
public class TitleView : MonoBehaviour
{
    private const float BlinkSpeed = 2f; // "터치하여 시작하기" 문구가 깜빡이는 속도

    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private Button touchButton; // 화면 전체를 덮는 버튼이라 마우스 클릭과 터치를 같이 받는다
    [ReadOnly] [SerializeField] private TextMeshProUGUI touchText;
    [ReadOnly] [SerializeField] private GameObject objDimmed; // 로그인 화면 뒤에 깔릴 때 타이틀을 어둡게 덮는다

    public event Action OnClickTouchEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "TitlePanel").gameObject;
        touchButton = GameUtil.Bind<Button>(transform, "TitlePanel");
        touchText = GameUtil.Bind<TextMeshProUGUI>(transform, "TitlePanel/TouchText");
        objDimmed = GameUtil.Bind<RectTransform>(transform, "TitlePanel/Dimmed").gameObject;

        touchButton.onClick.AddListener(OnClickTouch);
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
        objDimmed.SetActive(false);
    }

    // 타이틀을 끄지 않고 로그인 화면의 배경으로 남긴다. 어둡게 덮고, 터치 문구를 숨기고, 더 눌리지 않게 막는다
    public void ShowDimmed()
    {
        objRoot.SetActive(true);
        touchButton.interactable = false;
        touchText.gameObject.SetActive(false);
        objDimmed.SetActive(true);
    }

    // 판별 요청을 기다리는 동안 다시 눌리지 않게 막는다
    public void SetInteractable(bool interactable)
    {
        touchButton.interactable = interactable;
    }

    private void OnClickTouch()
    {
        OnClickTouchEvent.Invoke();
    }
}
