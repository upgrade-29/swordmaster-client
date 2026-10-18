using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 제목/본문 표시와 버튼 입력 전달만 담당한다. 구매 등 어떤 기능의 도메인 규칙도 갖지 않는다.
// 버튼은 Setup으로 받은 정보 수만큼 Footer의 템플릿 버튼을 복제해 보여 준다. 팝업을 닫는 일은 버튼 콜백이 맡는다
public class DialogPopup : BasePopup
{
    // 버튼 하나에 표시할 문구와 눌렀을 때 할 일
    public class DialogPopupButtonInfo
    {
        public readonly string label;
        public readonly Action onClick;

        public DialogPopupButtonInfo(string label, Action onClick)
        {
            this.label = label;
            this.onClick = onClick;
        }
    }

    [ReadOnly] [SerializeField] private TextMeshProUGUI titleText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI messageText;
    [ReadOnly] [SerializeField] private RectTransform rectFooter;
    [ReadOnly] [SerializeField] private List<ButtonUI> listButton = new List<ButtonUI>();
    [ReadOnly] [SerializeField] private List<TextMeshProUGUI> listButtonLabel = new List<TextMeshProUGUI>();

    private string title;
    private string message;
    private DialogPopupButtonInfo[] buttonInfos = Array.Empty<DialogPopupButtonInfo>();

    protected override void Awake()
    {
        base.Awake();

        titleText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Header/Title");
        messageText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Message");
        rectFooter = GameUtil.Bind<RectTransform>(transform, "Panel/Footer");
        AddButton(GameUtil.Bind<ButtonUI>(rectFooter, "Button"));
    }

    // 여는 쪽마다 버튼 수와 할 일이 다르므로, 열 때마다 버튼 정보를 바꿔 끼운다
    public void Setup(string title, string message, params DialogPopupButtonInfo[] buttonInfos)
    {
        this.title = title;
        this.message = message;
        this.buttonInfos = buttonInfos;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        titleText.text = title;
        messageText.text = message;

        ShowButtons(buttonInfos.Length);
        for (int i = 0; i < buttonInfos.Length; i++)
        {
            listButtonLabel[i].text = buttonInfos[i].label;
        }
    }

    // 모자라는 버튼만 템플릿에서 복제하고, 남는 버튼은 끈다
    private void ShowButtons(int count)
    {
        while (listButton.Count < count)
        {
            AddButton(Instantiate(listButton[0], rectFooter));
        }

        for (int i = 0; i < listButton.Count; i++)
        {
            listButton[i].gameObject.SetActive(i < count);
        }
    }

    private void AddButton(ButtonUI button)
    {
        button.OnClickEvent += () => OnClickButton(button);
        listButton.Add(button);
        listButtonLabel.Add(GameUtil.Bind<TextMeshProUGUI>(button.transform, "Label"));
    }

    private void OnClickButton(ButtonUI button)
    {
        buttonInfos[listButton.IndexOf(button)].onClick?.Invoke();
    }
}
