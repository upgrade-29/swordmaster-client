using System;
using TMPro;
using UnityEngine;

// 제목/본문 표시와 확인/취소 입력 전달만 담당한다. 구매 등 어떤 기능의 도메인 규칙도 갖지 않는다.
public class ConfirmPopupView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objRoot;
    [ReadOnly] [SerializeField] private TextMeshProUGUI titleText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI messageText;
    [ReadOnly] [SerializeField] private ShopButtonView confirmButton;
    [ReadOnly] [SerializeField] private ShopButtonView cancelButton;

    public event Action OnConfirmEvent = delegate { };
    public event Action OnCancelEvent = delegate { };

    private void Awake()
    {
        objRoot = GameUtil.Bind<RectTransform>(transform, "Root").gameObject;
        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Root/Dimmer/Title");
        messageText = GameUtil.Bind<TextMeshProUGUI>(transform, "Root/Message");
        confirmButton = GameUtil.Bind<ShopButtonView>(transform, "Root/Buttons/ConfirmButton");
        cancelButton = GameUtil.Bind<ShopButtonView>(transform, "Root/Buttons/CancelButton");

        confirmButton.OnClickEvent += OnClickConfirm;
        cancelButton.OnClickEvent += OnClickCancel;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.OnClickEvent -= OnClickConfirm;
        }

        if (cancelButton != null)
        {
            cancelButton.OnClickEvent -= OnClickCancel;
        }
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }

    public void Show()
    {
        objRoot.SetActive(true);
    }

    public void Hide()
    {
        objRoot.SetActive(false);
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
