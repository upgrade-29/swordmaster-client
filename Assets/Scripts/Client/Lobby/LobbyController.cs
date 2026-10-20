using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    private static readonly int LastTabIndex = Enum.GetValues(typeof(LobbyTabType)).Length - 1;

    private const string LogoutTitle = "로그아웃";
    private const string LogoutMessage = "로그아웃할까요?\n다음 실행부터 자동 로그인하지 않습니다.";

    [ReadOnly] [SerializeField] private LobbyView view;

    [ReadOnly(true)] [SerializeField] private LobbyTabType startTab = LobbyTabType.Battle;
    [ReadOnly(true)] [SerializeField] private float swipeThresholdRatio = 0.2f; // 이 값보다 많이 움직이면 탭 전환

    private User User => GameSession.Instance.User;
    private LobbyOptionPopup OptionPopup => Popup.Instance.Get<LobbyOptionPopup>();
    private DialogPopup DialogPopup => Popup.Instance.Get<DialogPopup>();

    private LobbyTabType currentTab;

    public event Action<LobbyTabType> OnChangeTabEvent = delegate { };

    private void Awake()
    {
        // 로그인하지 않고 로비 씬을 바로 열었으면 로그인 씬으로 보낸다. 탭들이 Start에서 유저를 읽으므로 그 전에 로비 UI를 끈다
        if (User == null)
        {
            Debug.LogWarning("[Lobby] Not logged in. Back to login");
            gameObject.SetActive(false);
            SceneManager.LoadScene(GameConstants.Scenes.Title);
            return;
        }

        GameUtil.Bind(gameObject, ref view);
        currentTab = startTab;

        view.OnClickTabButtonEvent += OnClickTabButton;
        view.OnSwipeEndEvent += OnSwipeEnd;
        view.OnUpdateTabLayoutEvent += OnUpdateTabLayout;
        view.OnClickMenuEvent += OnClickMenu;
    }

    private void Start()
    {
        SoundManager.Instance.PlayBgm(GameConstants.Sounds.BgmLobby);
        view.SetSelectedTab(currentTab);
        view.SetTrackPosition(GetTrackPosition(currentTab));
        view.SetNickname(User.Nickname);

        GameSession.Instance.OnChangeCurrenciesEvent += OnChangeCurrencies;
        RefreshCurrencies();

        OptionPopup.LogoutClicked += OnLogoutClicked;
    }

    private void OnDestroy()
    {
        // 플레이를 끝낼 때 세션이 먼저 사라졌으면 Instance가 새 세션을 만들기 때문에 확인한다
        if (GameSession.IsInitialize())
        {
            GameSession.Instance.OnChangeCurrenciesEvent -= OnChangeCurrencies;
        }

        // 씬을 닫을 때 Popup이 먼저 파괴됐으면 Instance로 새로 만들지 않는다. 팝업도 함께 파괴되므로 구독을 풀 필요가 없다
        if (Popup.IsInitialize())
        {
            OptionPopup.LogoutClicked -= OnLogoutClicked;
        }

        if (view == null)
        {
            return;
        }

        view.OnClickTabButtonEvent -= OnClickTabButton;
        view.OnSwipeEndEvent -= OnSwipeEnd;
        view.OnUpdateTabLayoutEvent -= OnUpdateTabLayout;
        view.OnClickMenuEvent -= OnClickMenu;
    }

    public void ChangeTab(LobbyTabType tab)
    {
        currentTab = tab;
        view.AnimateSelectedTab(tab);
        view.AnimateTrackPosition(GetTrackPosition(tab));
        OnChangeTabEvent.Invoke(tab);
    }

    private void RefreshCurrencies()
    {
        view.SetCurrencies(
            User.Currencies.Gold.ToString("N0", CultureInfo.InvariantCulture),
            User.Currencies.Diamond.ToString("N0", CultureInfo.InvariantCulture));
    }

    private LobbyTabType GetSwipeTargetTab(float trackPosition)
    {
        float movedTabCount = (trackPosition - GetTrackPosition(currentTab)) * LastTabIndex;
        if (Mathf.Abs(movedTabCount) <= swipeThresholdRatio)
        {
            return currentTab;
        }

        int nextTab = (int)currentTab + (movedTabCount > 0f ? 1 : -1);
        return (LobbyTabType)Mathf.Clamp(nextTab, 0, LastTabIndex);
    }

    // 첫 탭은 0, 마지막 탭은 1인 가로 스크롤 위치
    private float GetTrackPosition(LobbyTabType tab)
    {
        return (float)(int)tab / LastTabIndex;
    }

    private void OnClickTabButton(LobbyTabType tab)
    {
        ChangeTab(tab);
    }

    private void OnSwipeEnd()
    {
        ChangeTab(GetSwipeTargetTab(view.TrackPosition));
    }

    private void OnUpdateTabLayout()
    {
        view.SetTrackPosition(GetTrackPosition(currentTab));
    }

    private void OnChangeCurrencies()
    {
        RefreshCurrencies();
    }

    private void OnClickMenu()
    {
        OptionPopup.Open();
    }

    private void OnLogoutClicked()
    {
        OptionPopup.Close();
        DialogPopup.Setup(LogoutTitle, LogoutMessage,
            new DialogPopup.DialogPopupButtonInfo("확인", OnConfirmLogout),
            new DialogPopup.DialogPopupButtonInfo("취소", DialogPopup.Close));
        DialogPopup.Open();
    }

    // 서버 응답을 기다리지 않고 기기에서 먼저 로그아웃한다. 요청은 보내는 순간 헤더를 복사하므로 세션을 지워도 액세스 토큰이 붙어 간다
    // 늦게 온 401은 이미 로그아웃한 상태라 HttpErrorHandler가 처리하지 않고, 서버 요청이 실패해도 다음 실행에서 자동 로그인하지 않는다
    private void OnConfirmLogout()
    {
        DialogPopup.Close();
        HttpRequest.Request(AuthApi.Logout(), null, false);
        GameSession.Instance.Logout();
        SceneManager.LoadScene(GameConstants.Scenes.Title);
    }
}
