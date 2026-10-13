using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 플레이어가 없는 계정이 처음 로그인했을 때 닉네임을 입력받는다. 입력 전달과 에러 문구 표시만 담당한다.
// 플레이어를 만들어야 로비에 들어갈 수 있으므로 밖을 눌러도 닫히지 않게 프리팹에서 closeOnOutsideTouch를 끈다
public class NicknamePopup : BasePopup
{
    public const int NicknameMaxLength = 10;

    // 공백 없이 1~10자. 입력 중인 한 글자 검사와 확인 버튼을 눌렀을 때 전체 검사에 같이 쓴다
    private static readonly Regex NicknameRegex = new Regex($@"^\S{{1,{NicknameMaxLength}}}$");

    [ReadOnly] [SerializeField] private TMP_InputField nicknameInput;
    [ReadOnly] [SerializeField] private Button confirmButton;
    [ReadOnly] [SerializeField] private TextMeshProUGUI errorText;

    public event Action<string> OnClickConfirmEvent = delegate { };

    protected override void Awake()
    {
        base.Awake();

        nicknameInput = GameUtil.Bind<TMP_InputField>(transform, "Panel/NicknameInput");
        confirmButton = GameUtil.Bind<Button>(transform, "Panel/ConfirmButton");
        errorText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/ErrorText");

        nicknameInput.characterLimit = NicknameMaxLength;
        nicknameInput.onValidateInput = OnValidateInput;

        confirmButton.onClick.AddListener(OnClickConfirm);
        nicknameInput.onSubmit.AddListener(OnSubmitInput);
        errorText.color = Color.red;
        errorText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnClickConfirm);
        }

        if (nicknameInput != null)
        {
            nicknameInput.onSubmit.RemoveListener(OnSubmitInput);
        }
    }

    // 입력칸에 바로 입력할 수 있게 포커스를 준다
    public void Focus()
    {
        nicknameInput.Select();
        nicknameInput.ActivateInputField();
    }

    public void SetError(string message)
    {
        errorText.text = message;
    }

    public void SetInteractable(bool interactable)
    {
        nicknameInput.interactable = interactable;
        confirmButton.interactable = interactable;
    }

    private void OnClickConfirm()
    {
        string nickname = nicknameInput.text;
        if (NicknameRegex.IsMatch(nickname) == false)
        {
            SetError($"한글·영문·숫자 {NicknameMaxLength}자 이내로 입력하세요.");
            return;
        }

        errorText.text = string.Empty;
        OnClickConfirmEvent.Invoke(nickname);
    }

    // 허용하지 않는 글자는 입력되지 않게 버린다. 길이 초과는 characterLimit이 막는다
    private char OnValidateInput(string text, int charIndex, char addedChar)
    {
        return NicknameRegex.IsMatch(addedChar.ToString()) ? addedChar : '\0';
    }

    // 입력칸에서 Enter를 누르면 확인 버튼을 누른 것과 같다
    private void OnSubmitInput(string _)
    {
        if (confirmButton.interactable)
        {
            OnClickConfirm();
        }
    }
}
