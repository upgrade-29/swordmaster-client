using System;
using UnityEngine;
using UnityEngine.UI;

// 전투 중 옵션 버튼을 누르면 뜨는 팝업. 버튼 입력을 이벤트로 알리기만 하고, 포기 처리는 여는 쪽이 한다
public class BattleOptionPopup : BasePopup
{
    [ReadOnly] [SerializeField] private Button abandonButton;
    [ReadOnly] [SerializeField] private Button closeButton;

    public event Action AbandonClicked;

    protected override void Awake()
    {
        base.Awake();

        abandonButton = GameUtil.Bind<Button>(transform, "Panel/Footer/AbandonButton");
        closeButton = GameUtil.Bind<Button>(transform, "Panel/Footer/CloseButton");

        abandonButton.onClick.AddListener(OnClickAbandon);
        closeButton.onClick.AddListener(Close);
    }

    private void OnClickAbandon() => AbandonClicked?.Invoke();
}
