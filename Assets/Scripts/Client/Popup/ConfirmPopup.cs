using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 제목/본문 표시와 확인/취소 입력 전달만 담당한다. 구매 등 어떤 기능의 도메인 규칙도 갖지 않는다.
public class ConfirmPopup : BasePopup
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI titleText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI messageText;
    [ReadOnly] [SerializeField] private Button confirmButton;
    [ReadOnly] [SerializeField] private Button cancelButton;

    public event Action OnConfirmEvent = delegate { };
    public event Action OnCancelEvent = delegate { };

    protected override void Awake()
    {
        base.Awake();

        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/TitleBar/Title");
        messageText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Message");
        confirmButton = GameUtil.Bind<Button>(transform, "Panel/Buttons/ConfirmButton");
        cancelButton = GameUtil.Bind<Button>(transform, "Panel/Buttons/CancelButton");

        confirmButton.onClick.AddListener(OnClickConfirm);
        cancelButton.onClick.AddListener(OnClickCancel);
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }

    private void OnClickConfirm()
    {
        OnConfirmEvent.Invoke();
    }

    private void OnClickCancel()
    {
        OnCancelEvent.Invoke();
    }
}
