using System;
using UnityEngine;

// 로비 상단 메뉴 버튼을 누르면 뜨는 팝업. 버튼 입력을 이벤트로 알리기만 하고, 로그아웃 처리는 여는 쪽이 한다
public class LobbyOptionPopup : BasePopup
{
    [ReadOnly] [SerializeField] private ButtonUI logoutButton;

    public event Action LogoutClicked;

    protected override void Awake()
    {
        base.Awake();

        logoutButton = GameUtil.Bind<ButtonUI>(transform, "Panel/Footer/LogoutButton");

        logoutButton.OnClickEvent += OnClickLogout;
    }

    private void OnClickLogout() => LogoutClicked?.Invoke();
}
