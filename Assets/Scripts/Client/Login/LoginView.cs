using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아이디/비밀번호 입력, 로그인 버튼 입력 전달, 결과 문구 표시만 담당한다. 인증 로직은 갖지 않는다.
public class LoginView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TMP_InputField idInput;
    [ReadOnly] [SerializeField] private TMP_InputField passwordInput;
    [ReadOnly] [SerializeField] private Button loginButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI resultText;

    public event Action<string, string> OnClickLoginEvent = delegate { };

    private void Awake()
    {
        idInput = GameUtil.Bind<TMP_InputField>(transform, "Panel/IdInput");
        passwordInput = GameUtil.Bind<TMP_InputField>(transform, "Panel/PasswordInput");
        loginButton = GameUtil.Bind<Button>(transform, "Panel/LoginButton");
        resultText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/ResultText");

        loginButton.onClick.AddListener(OnClickLogin);
        resultText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnClickLogin);
        }
    }

    public void SetResult(bool isSuccess, string message)
    {
        resultText.color = isSuccess ? Color.green : Color.red;
        resultText.text = message;
    }

    public void SetInteractable(bool interactable)
    {
        idInput.interactable = interactable;
        passwordInput.interactable = interactable;
        loginButton.interactable = interactable;
    }

    private void OnClickLogin()
    {
        OnClickLoginEvent.Invoke(idInput.text, passwordInput.text);
    }
}
