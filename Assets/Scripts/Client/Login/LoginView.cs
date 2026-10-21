using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 아이디/비밀번호 입력, 로그인/회원가입 버튼 입력 전달, 결과 문구 표시만 담당한다. 인증 로직은 갖지 않는다.
// Tab으로 입력칸을 오가고, 아이디에서 Enter(모바일 키보드 완료)를 누르면 비밀번호로, 비밀번호에서 누르면 로그인한다
public class LoginView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private TMP_InputField idInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordInput;
    [ReadOnly] [SerializeField] private Button loginButton;
    [ReadOnly] [SerializeField] private Button signupButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI resultText;
    [ReadOnly][SerializeField] private Button googleButton;

    private string googleToken = "108416504166-s6e69s8oo6fh27s58dko1t2aajj5fevt.apps.googleusercontent.com";

    // onSubmit 직후 입력칸이 스스로 비활성화되므로, 다음 칸 포커스는 다음 프레임에 준다
    private TMP_InputField pendingFocusInput;

    public event Action<string, string> OnClickLoginEvent = delegate { };
    public event Action OnClickSignupEvent = delegate { };
    public event Action OnClickGoogleEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "LoginPanel").gameObject;
        idInput = GameUtil.Bind<TMP_InputField>(transform, "LoginPanel/IdInput");
        passwordInput = GameUtil.Bind<TMP_InputField>(transform, "LoginPanel/PasswordInput");
        loginButton = GameUtil.Bind<Button>(transform, "LoginPanel/LoginButton");
        signupButton = GameUtil.Bind<Button>(transform, "LoginPanel/SignupButton");
        googleButton = GameUtil.Bind<Button>(transform, "LoginPanel/GoogleButton");
        resultText = GameUtil.Bind<TextMeshProUGUI>(transform, "LoginPanel/ResultText");

        loginButton.onClick.AddListener(OnClickLogin);
        signupButton.onClick.AddListener(OnClickSignup);
        googleButton.onClick.AddListener(OnClickGoogle);
        idInput.onSubmit.AddListener(OnSubmitId);
        passwordInput.onSubmit.AddListener(OnSubmitPassword);
        resultText.text = string.Empty;
    }

    // 예약된 포커스를 주고, Tab을 누르면 아이디와 비밀번호 입력칸을 오간다
    private void Update()
    {
        if (pendingFocusInput != null)
        {
            Focus(pendingFocusInput);
            pendingFocusInput = null;
        }

        if (objRoot.activeInHierarchy == false || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Focus(idInput.isFocused ? passwordInput : idInput);
        }
    }

    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnClickLogin);
        }

        if (googleButton != null)
        {
            googleButton.onClick.RemoveListener(OnClickGoogle);
        }

        if (idInput != null)
        {
            idInput.onSubmit.RemoveListener(OnSubmitId);
        }

        if (passwordInput != null)
        {
            passwordInput.onSubmit.RemoveListener(OnSubmitPassword);
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
        idInput.interactable = interactable;
        passwordInput.interactable = interactable;
        loginButton.interactable = interactable;
        signupButton.interactable = interactable;
        googleButton.interactable = interactable;
    }

    private void OnClickLogin()
    {
        OnClickLoginEvent.Invoke(idInput.text, passwordInput.text);
    }

    private void Focus(TMP_InputField input)
    {
        input.Select();
        input.ActivateInputField();
    }

    // 아이디에서 Enter(모바일 키보드 완료)를 누르면 비밀번호 입력칸으로 넘어간다
    private void OnSubmitId(string _)
    {
        if (passwordInput.interactable)
        {
            pendingFocusInput = passwordInput;
        }
    }

    // 비밀번호에서 Enter(모바일 키보드 완료)를 누르면 로그인 버튼을 누른 것과 같다
    private void OnSubmitPassword(string _)
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

    private void OnClickGoogle()
    {
        OnClickGoogleEvent.Invoke();
    }
}
