using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀을 터치하면 먼저 뜨는 로그인 방법 선택 팝업. Google/이메일 버튼 입력 전달과 결과 문구 표시만 담당한다
// Google 로그인은 이 팝업에서 바로 진행하므로 진행 문구와 실패 이유도 여기에 보여 준다
public class LoginSelectView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private Button googleButton;
    [ReadOnly] [SerializeField] private Button emailButton;
    [ReadOnly][SerializeField] private Button guestButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI resultText;

    public event Action OnClickGoogleEvent = delegate { };
    public event Action OnClickEmailEvent = delegate { };
    public event Action OnClickGuestEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "LoginSelectPanel").gameObject;
        googleButton = GameUtil.Bind<Button>(transform, "LoginSelectPanel/GoogleButton");
        emailButton = GameUtil.Bind<Button>(transform, "LoginSelectPanel/EmailButton");
        guestButton = GameUtil.Bind<Button>(transform, "LoginSelectPanel/GuestButton");
        resultText = GameUtil.Bind<TextMeshProUGUI>(transform, "LoginSelectPanel/ResultText");

        googleButton.onClick.AddListener(OnClickGoogle);
        emailButton.onClick.AddListener(OnClickEmail);
        guestButton.onClick.AddListener(OnClickGuest);
        resultText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (googleButton != null)
        {
            googleButton.onClick.RemoveListener(OnClickGoogle);
        }

        if (emailButton != null)
        {
            emailButton.onClick.RemoveListener(OnClickEmail);
        }
        if (guestButton != null)
        {
            guestButton.onClick.RemoveListener(OnClickGuest);
        }
    }

    public void Show()
    {
        objRoot.SetActive(true);
    }

    public void Hide()
    {
        objRoot.SetActive(false);
    }

    public void SetResult(bool isSuccess, string message)
    {
        resultText.color = isSuccess ? Color.green : Color.red;
        resultText.text = message;

        if (isSuccess == false)
        {
            SoundManager.Instance.PlaySfx(GameConstants.Sounds.UiError);
        }
    }

    public void SetMessage(string message)
    {
        resultText.color = Color.white;
        resultText.text = message;
    }

    public void SetInteractable(bool interactable)
    {
        googleButton.interactable = interactable;
        emailButton.interactable = interactable;
        guestButton.interactable = interactable;
    }

    private void OnClickGoogle()
    {
        OnClickGoogleEvent.Invoke();
    }

    private void OnClickEmail()
    {
        OnClickEmailEvent.Invoke();
    }

    private void OnClickGuest()
    {
        OnClickGuestEvent.Invoke();
    }
}
