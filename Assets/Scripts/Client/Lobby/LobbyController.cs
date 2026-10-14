using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    private const string LoginSceneName = "LoginScene";
    private static readonly int LastTabIndex = Enum.GetValues(typeof(LobbyTabType)).Length - 1;

    [ReadOnly] [SerializeField] private LobbyView view;

    [ReadOnly(true)] [SerializeField] private LobbyTabType startTab = LobbyTabType.Battle;
    [ReadOnly(true)] [SerializeField] private float swipeThresholdRatio = 0.2f; // 이 값보다 많이 움직이면 탭 전환

    private User User => GameSession.Instance.User;

    private LobbyTabType currentTab;

    private void Awake()
    {
        // 로그인하지 않고 로비 씬을 바로 열었으면 로그인 씬으로 보낸다. 탭들이 Start에서 유저를 읽으므로 그 전에 로비 UI를 끈다
        if (User == null)
        {
            Debug.LogWarning("[Lobby] Not logged in. Back to login");
            gameObject.SetActive(false);
            SceneManager.LoadScene(LoginSceneName);
            return;
        }

        GameUtil.Bind(gameObject, ref view);
        currentTab = startTab;

        view.OnClickTabButtonEvent += OnClickTabButton;
        view.OnSwipeEndEvent += OnSwipeEnd;
        view.OnUpdateTabLayoutEvent += OnUpdateTabLayout;
    }

    private void Start()
    {
        view.SetSelectedTab(currentTab);
        view.SetTrackPosition(GetTrackPosition(currentTab));

        GameSession.Instance.OnChangeCurrenciesEvent += OnChangeCurrencies;
        RefreshCurrencies();
    }

    private void OnDestroy()
    {
        // 플레이를 끝낼 때 세션이 먼저 사라졌으면 Instance가 새 세션을 만들기 때문에 확인한다
        if (GameSession.IsInitialize())
        {
            GameSession.Instance.OnChangeCurrenciesEvent -= OnChangeCurrencies;
        }

        if (view == null)
        {
            return;
        }

        view.OnClickTabButtonEvent -= OnClickTabButton;
        view.OnSwipeEndEvent -= OnSwipeEnd;
        view.OnUpdateTabLayoutEvent -= OnUpdateTabLayout;
    }

    public void ChangeTab(LobbyTabType tab)
    {
        currentTab = tab;
        view.AnimateSelectedTab(tab);
        view.AnimateTrackPosition(GetTrackPosition(tab));
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
}
