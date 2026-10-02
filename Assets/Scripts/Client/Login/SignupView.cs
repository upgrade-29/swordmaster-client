using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 회원가입 입력(아이디/비밀번호/비밀번호 확인/닉네임), 가입/뒤로 버튼 입력 전달, 결과 문구 표시만 담당한다.
public class SignupView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private TMP_InputField idInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordConfirmInput;
    [ReadOnly] [SerializeField] private TMP_InputField nicknameInput;
    [ReadOnly] [SerializeField] private Button signupButton;
    [ReadOnly] [SerializeField] private Button backButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI resultText;

    // 아이디, 비밀번호, 비밀번호 확인, 닉네임
    public event Action<string, string, string, string> OnClickSignupEvent = delegate { };
    public event Action OnClickBackEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "SignupPanel").gameObject;
        idInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/IdInput");
        passwordInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/PasswordInput");
        passwordConfirmInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/PasswordConfirmInput");
        nicknameInput = GameUtil.Bind<TMP_InputField>(transform, "SignupPanel/NicknameInput");
        signupButton = GameUtil.Bind<Button>(transform, "SignupPanel/SignupButton");
        backButton = GameUtil.Bind<Button>(transform, "SignupPanel/BackButton");
        resultText = GameUtil.Bind<TextMeshProUGUI>(transform, "SignupPanel/ResultText");

        signupButton.onClick.AddListener(OnClickSignup);
        backButton.onClick.AddListener(OnClickBack);
        resultText.text = string.Empty;
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
    }

    // 열 때마다 이전 입력과 결과 문구를 지운다
    public void Show()
    {
        idInput.text = string.Empty;
        passwordInput.text = string.Empty;
        passwordConfirmInput.text = string.Empty;
        nicknameInput.text = string.Empty;
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
        nicknameInput.interactable = interactable;
        signupButton.interactable = interactable;
        backButton.interactable = interactable;
    }

    private void OnClickSignup()
    {
        OnClickSignupEvent.Invoke(idInput.text, passwordInput.text, passwordConfirmInput.text, nicknameInput.text);
    }

    private void OnClickBack()
    {
        OnClickBackEvent.Invoke();
    }
}
