using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    private static readonly LobbyTabType[] Tabs = (LobbyTabType[])Enum.GetValues(typeof(LobbyTabType));

    [ReadOnly] [SerializeField] private LobbyPlayerInfo playerInfo;
    [ReadOnly] [SerializeField] private ScrollRect scrollRectTab;
    [ReadOnly] [SerializeField] private List<LobbyTab> listLobbyTab = new List<LobbyTab>();
    [ReadOnly] [SerializeField] private List<LobbyTabButton> listLobbyTabButton = new List<LobbyTabButton>();

    [SerializeField] private float trackAnimationDuration = 0.28f;

    private Tween tweenTrack;
    private bool isStarted;

    public float TrackPosition => scrollRectTab.horizontalNormalizedPosition;

    public event Action<LobbyTabType> OnClickTabButtonEvent = delegate { };
    public event Action OnSwipeEndEvent = delegate { };
    public event Action OnUpdateTabLayoutEvent = delegate { };
    public event Action OnClickMenuEvent = delegate { };
    public event Action OnClickRankingEvent = delegate { };

    private void Awake()
    {
        playerInfo = GameUtil.Bind<LobbyPlayerInfo>(transform, "TopUI/PlayerInfo");
        playerInfo.OnClickMenuEvent += OnClickMenu;
        playerInfo.OnClickRankingEvent += OnClickRanking;
        scrollRectTab = GameUtil.Bind<ScrollRect>(transform, "TabList");

        foreach (var tab in Tabs)
        {
            var lobbyTab = GameUtil.Bind<LobbyTab>(transform, $"TabList/Content/{tab.ToString()}Tab");
            lobbyTab.OnBeginDragTrackEvent += OnBeginDragTrack;
            lobbyTab.OnEndDragTrackEvent += OnEndDragTrack;
            listLobbyTab.Add(lobbyTab);

            var lobbyTabButton = GameUtil.Bind<LobbyTabButton>(transform, $"BottomMenu/{tab.ToString()}Button");
            lobbyTabButton.Init(tab);
            lobbyTabButton.OnClickTabButtonEvent += OnClickTabButton;
            listLobbyTabButton.Add(lobbyTabButton);
        }
    }

    private void Start()
    {
        isStarted = true;
        UpdateTabLayout();
    }

    private void OnDestroy()
    {
        tweenTrack?.Kill();

        if (playerInfo != null)
        {
            playerInfo.OnClickMenuEvent -= OnClickMenu;
            playerInfo.OnClickRankingEvent -= OnClickRanking;
        }

        foreach (var lobbyTab in listLobbyTab)
        {
            if (lobbyTab == null)
            {
                continue;
            }

            lobbyTab.OnBeginDragTrackEvent -= OnBeginDragTrack;
            lobbyTab.OnEndDragTrackEvent -= OnEndDragTrack;
        }

        foreach (var lobbyTabButton in listLobbyTabButton)
        {
            if (lobbyTabButton == null)
            {
                continue;
            }

            lobbyTabButton.OnClickTabButtonEvent -= OnClickTabButton;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isStarted == false)
        {
            return;
        }

        UpdateTabLayout();
    }

    public void SetNickname(string nickname)
    {
        playerInfo.SetNickname(nickname);
    }

    public void SetCurrencies(string gold, string diamond)
    {
        playerInfo.SetCurrencies(gold, diamond);
    }

    public Vector3 GetCurrencyIconPosition(CurrencyType type)
    {
        return playerInfo.GetCurrencyIconPosition(type);
    }

    public void SetSelectedTab(LobbyTabType selectedTab)
    {
        foreach (var lobbyTabButton in listLobbyTabButton)
        {
            lobbyTabButton.SetSelected(lobbyTabButton.TabType == selectedTab);
        }

        UpdateTabSeparators(selectedTab);
    }

    public void AnimateSelectedTab(LobbyTabType selectedTab)
    {
        foreach (var lobbyTabButton in listLobbyTabButton)
        {
            lobbyTabButton.AnimateSelected(lobbyTabButton.TabType == selectedTab);
        }

        UpdateTabSeparators(selectedTab);
    }

    public void SetTrackPosition(float normalizedPosition)
    {
        tweenTrack?.Kill();
        scrollRectTab.horizontalNormalizedPosition = normalizedPosition;
    }

    public void AnimateTrackPosition(float normalizedPosition)
    {
        tweenTrack?.Kill();
        tweenTrack = scrollRectTab.DOHorizontalNormalizedPos(normalizedPosition, trackAnimationDuration).SetEase(Ease.OutCubic);
    }

    // 스크롤 범위 갱신
    private void UpdateTabLayout()
    {
        float viewportWidth = scrollRectTab.viewport.rect.width;
        foreach (var lobbyTab in listLobbyTab)
        {
            lobbyTab.SetWidth(viewportWidth);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTab.content);
        OnUpdateTabLayoutEvent.Invoke();
    }

    // 구분선은 각 탭의 오른쪽 끝에 있고, 양옆 탭이 모두 선택되지 않았을 때만 보임
    private void UpdateTabSeparators(LobbyTabType selectedTab)
    {
        for (int i = 0; i < listLobbyTabButton.Count - 1; i++)
        {
            bool isBetweenUnselectedTabs = listLobbyTabButton[i].TabType != selectedTab
                && listLobbyTabButton[i + 1].TabType != selectedTab;
            listLobbyTabButton[i].SetSeparatorVisible(isBetweenUnselectedTabs);
        }
    }

    private void OnBeginDragTrack(LobbyTab lobbyTab)
    {
        tweenTrack?.Kill();
    }

    private void OnEndDragTrack(LobbyTab lobbyTab)
    {
        OnSwipeEndEvent.Invoke();
    }

    private void OnClickTabButton(LobbyTabButton lobbyTabButton)
    {
        OnClickTabButtonEvent.Invoke(lobbyTabButton.TabType);
    }

    private void OnClickMenu()
    {
        OnClickMenuEvent.Invoke();
    }

    private void OnClickRanking()
    {
        OnClickRankingEvent.Invoke();
    }
}
