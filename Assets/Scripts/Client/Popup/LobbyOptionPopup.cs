using System;
using UnityEngine;

// 옵션 팝업의 입력을 이벤트로 전달하고 계정 연결 중 입력을 제한한다.
public class LobbyOptionPopup : BasePopup
{
    [ReadOnly] [SerializeField] private ButtonUI logoutButton;
    [ReadOnly] [SerializeField] private ButtonUI guestToGoogleButton;

    private bool isLinkInProgress;

    public event Action LogoutClicked;
    public event Action GuestToGoogleClicked;

    protected override void Awake()
    {
        base.Awake();

        logoutButton = GameUtil.Bind<ButtonUI>(transform, "Panel/Footer/LogoutButton");
        guestToGoogleButton = GameUtil.Bind<ButtonUI>(transform, "Panel/Footer/GuestToGoogleButton");

        logoutButton.OnClickEvent += OnClickLogout;
        guestToGoogleButton.OnClickEvent += OnClickGuestToGoogle;
    }

    private void OnDestroy()
    {
        if (logoutButton != null)
        {
            logoutButton.OnClickEvent -= OnClickLogout;
        }

        if (guestToGoogleButton != null)
        {
            guestToGoogleButton.OnClickEvent -= OnClickGuestToGoogle;
        }
    }

    public void SetLinkInProgress(bool inProgress)
    {
        isLinkInProgress = inProgress;
        logoutButton.Interactable = !inProgress;
        guestToGoogleButton.Interactable = !inProgress;
    }

    protected override void OnClickDimmed()
    {
        if (isLinkInProgress)
        {
            return;
        }

        base.OnClickDimmed();
    }

    private void OnClickLogout()
    {
        if (isLinkInProgress)
        {
            return;
        }

        LogoutClicked?.Invoke();
    }

    private void OnClickGuestToGoogle()
    {
        if (isLinkInProgress)
        {
            return;
        }

        GuestToGoogleClicked?.Invoke();
    }
}
