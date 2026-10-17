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

    private string title;
    private string message;
    private Action onConfirm;
    private Action onCancel;

    protected override void Awake()
    {
        base.Awake();

        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Header/Title");
        messageText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Message");
        confirmButton = GameUtil.Bind<Button>(transform, "Panel/Footer/ConfirmButton");
        cancelButton = GameUtil.Bind<Button>(transform, "Panel/Footer/CancelButton");

        confirmButton.onClick.AddListener(OnClickConfirm);
        cancelButton.onClick.AddListener(OnClickCancel);
    }

    // 여는 쪽마다 확인/취소에서 할 일이 다르므로, 열 때마다 콜백을 바꿔 끼운다
    public void Setup(string title, string message, Action onConfirm, Action onCancel)
    {
        this.title = title;
        this.message = message;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        titleText.text = title;
        messageText.text = message;
    }

    private void OnClickConfirm()
    {
        onConfirm?.Invoke();
    }

    private void OnClickCancel()
    {
        onCancel?.Invoke();
    }
}
