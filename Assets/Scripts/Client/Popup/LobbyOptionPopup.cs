using System;
using UnityEngine;
using UnityEngine.UI;

// 로비 상단 메뉴 버튼을 누르면 뜨는 팝업. 버튼 입력을 이벤트로 알리기만 하고, 로그아웃 처리는 여는 쪽이 한다
public class LobbyOptionPopup : BasePopup
{
    [ReadOnly] [SerializeField] private Button logoutButton;

    public event Action LogoutClicked;

    protected override void Awake()
    {
        base.Awake();

        logoutButton = GameUtil.Bind<Button>(transform, "Panel/Footer/LogoutButton");

        logoutButton.onClick.AddListener(OnClickLogout);
    }

    private void OnClickLogout() => LogoutClicked?.Invoke();
}
