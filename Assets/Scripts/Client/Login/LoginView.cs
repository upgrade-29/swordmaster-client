using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아이디/비밀번호 입력, 로그인/회원가입 버튼 입력 전달, 결과 문구 표시만 담당한다. 인증 로직은 갖지 않는다.
public class LoginView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private TMP_InputField idInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordInput;
    [ReadOnly] [SerializeField] private Button loginButton;
    [ReadOnly] [SerializeField] private Button signupButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI resultText;

    public event Action<string, string> OnClickLoginEvent = delegate { };
    public event Action OnClickSignupEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "LoginPanel").gameObject;
        idInput = GameUtil.Bind<TMP_InputField>(transform, "LoginPanel/IdInput");
        passwordInput = GameUtil.Bind<TMP_InputField>(transform, "LoginPanel/PasswordInput");
        loginButton = GameUtil.Bind<Button>(transform, "LoginPanel/LoginButton");
        signupButton = GameUtil.Bind<Button>(transform, "LoginPanel/SignupButton");
        resultText = GameUtil.Bind<TextMeshProUGUI>(transform, "LoginPanel/ResultText");

        loginButton.onClick.AddListener(OnClickLogin);
        signupButton.onClick.AddListener(OnClickSignup);
        resultText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnClickLogin);
        }

        if (signupButton != null)
        {
            signupButton.onClick.RemoveListener(OnClickSignup);
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
    }

    public void SetMessage(string message)
    {
        resultText.color = Color.white;
        resultText.text = message;
    }

    public void SetInteractable(bool interactable)
    {
        idInput.interactable = interactable;
        passwordInput.interactable = interactable;
        loginButton.interactable = interactable;
        signupButton.interactable = interactable;
    }

    private void OnClickLogin()
    {
        OnClickLoginEvent.Invoke(idInput.text, passwordInput.text);
    }

    private void OnClickSignup()
    {
        OnClickSignupEvent.Invoke();
    }
}
