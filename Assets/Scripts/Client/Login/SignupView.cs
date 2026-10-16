using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 회원가입 입력(아이디/비밀번호/비밀번호 확인), 가입/뒤로 버튼 입력 전달, 결과 문구 표시만 담당한다.
// Tab(Shift+Tab은 역순)으로 입력칸을 오가고, Enter(모바일 키보드 완료)를 누르면 다음 칸으로, 마지막 칸에서 누르면 가입한다
public class SignupView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private TMP_InputField idInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordConfirmInput;
    [ReadOnly] [SerializeField] private Button signupButton;
    [ReadOnly] [SerializeField] private Button backButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI resultText;

    // Tab으로 이동하는 순서
    private TMP_InputField[] tabOrderInputs;

    // onSubmit 직후 입력칸이 스스로 비활성화되므로, 다음 칸 포커스는 다음 프레임에 준다
    private TMP_InputField pendingFocusInput;

    // 아이디, 비밀번호, 비밀번호 확인
    public event Action<string, string, string> OnClickSignupEvent = delegate { };
    public event Action OnClickBackEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "SignupPanel").gameObject;
        idInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/IdInput");
        passwordInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/PasswordInput");
        passwordConfirmInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/PasswordConfirmInput");
        signupButton = GameUtil.Bind<Button>(transform, "SignupPanel/SignupButton");
        backButton = GameUtil.Bind<Button>(transform, "SignupPanel/BackButton");
        resultText = GameUtil.Bind<TextMeshProUGUI>(transform, "SignupPanel/ResultText");

        tabOrderInputs = new[] { idInput, passwordInput, passwordConfirmInput };

        signupButton.onClick.AddListener(OnClickSignup);
        backButton.onClick.AddListener(OnClickBack);
        idInput.onSubmit.AddListener(OnSubmitId);
        passwordInput.onSubmit.AddListener(OnSubmitPassword);
        passwordConfirmInput.onSubmit.AddListener(OnSubmitPasswordConfirm);
        resultText.text = string.Empty;
    }

    // 예약된 포커스를 주고, Tab을 누르면 입력칸을 순서대로 오간다
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
            int step = Keyboard.current.shiftKey.isPressed ? -1 : 1;
            int focusedIndex = Array.FindIndex(tabOrderInputs, input => input.isFocused);
            // 아무 칸도 포커스가 없으면 첫 칸부터 시작한다
            int nextIndex = focusedIndex < 0 ? 0 : (focusedIndex + step + tabOrderInputs.Length) % tabOrderInputs.Length;
            Focus(tabOrderInputs[nextIndex]);
        }
    }

    private void OnDestroy()
    {
        if (signupButton != null)
        {
            signupButton.onClick.RemoveListener(OnClickSignup);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnClickBack);
        }

        if (idInput != null)
        {
            idInput.onSubmit.RemoveListener(OnSubmitId);
        }

        if (passwordInput != null)
        {
            passwordInput.onSubmit.RemoveListener(OnSubmitPassword);
        }

        if (passwordConfirmInput != null)
        {
            passwordConfirmInput.onSubmit.RemoveListener(OnSubmitPasswordConfirm);
        }
    }

    // 열 때마다 이전 입력과 결과 문구를 지운다
    public void Show()
    {
        idInput.text = string.Empty;
        passwordInput.text = string.Empty;
        passwordConfirmInput.text = string.Empty;
        resultText.text = string.Empty;
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
        passwordConfirmInput.interactable = interactable;
        signupButton.interactable = interactable;
        backButton.interactable = interactable;
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

    // 비밀번호에서 Enter(모바일 키보드 완료)를 누르면 비밀번호 확인 입력칸으로 넘어간다
    private void OnSubmitPassword(string _)
    {
        if (passwordConfirmInput.interactable)
        {
            pendingFocusInput = passwordConfirmInput;
        }
    }

    // 비밀번호 확인에서 Enter(모바일 키보드 완료)를 누르면 가입 버튼을 누른 것과 같다
    private void OnSubmitPasswordConfirm(string _)
    {
        if (signupButton.interactable)
        {
            OnClickSignup();
        }
    }

    private void OnClickSignup()
    {
        OnClickSignupEvent.Invoke(idInput.text, passwordInput.text, passwordConfirmInput.text);
    }

    private void OnClickBack()
    {
        OnClickBackEvent.Invoke();
    }
}
