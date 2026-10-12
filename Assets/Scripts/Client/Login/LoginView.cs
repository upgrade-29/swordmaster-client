using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 아이디/비밀번호 입력, 로그인/회원가입 버튼 입력 전달, 결과 문구 표시만 담당한다. 인증 로직은 갖지 않는다.
// Tab으로 입력칸을 오가고, Enter로 로그인한다
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
        idInput.onSubmit.AddListener(OnSubmitInput);
        passwordInput.onSubmit.AddListener(OnSubmitInput);
        resultText.text = string.Empty;
    }

    // Tab을 누르면 아이디와 비밀번호 입력칸을 오간다
    private void Update()
    {
        if (objRoot.activeInHierarchy == false || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TMP_InputField next = idInput.isFocused ? passwordInput : idInput;
            next.Select();
            next.ActivateInputField();
        }
    }

    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnClickLogin);
        }

        if (idInput != null)
        {
            idInput.onSubmit.RemoveListener(OnSubmitInput);
        }

        if (passwordInput != null)
        {
            passwordInput.onSubmit.RemoveListener(OnSubmitInput);
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

    // 아이디를 채우고 비밀번호는 비운다
    public void SetLoginId(string loginId)
    {
        idInput.text = loginId;
        passwordInput.text = string.Empty;
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

    // 입력칸에서 Enter를 누르면 로그인 버튼을 누른 것과 같다
    private void OnSubmitInput(string _)
    {
        if (loginButton.interactable)
        {
            OnClickLogin();
        }
    }

    private void OnClickSignup()
    {
        OnClickSignupEvent.Invoke();
    }
}
