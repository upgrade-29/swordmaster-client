using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로비 상단 랭킹 버튼을 누르면 뜨는 팝업. 종류 탭을 누르면 이벤트로 알리고, 요청은 여는 쪽이 해서 SetRanking으로 넘긴다
public class RankingPopup : BasePopup
{
    private static readonly RankingType[] Types = (RankingType[])Enum.GetValues(typeof(RankingType));

    private const string LoadingMessage = "랭킹을 불러오는 중입니다.";
    private const string FailedMessage = "랭킹을 불러오지 못했습니다.";
    private const string EmptyMessage = "아직 랭킹이 없습니다.";
    private const string NoRecordText = "기록 없음";
    private const string NotAggregatedText = "집계 전";
    private const string MyRankingTitle = "내 순위";
    private const string MyRankingEstimatedTitle = "내 순위 · 예상 (다음 집계 때 반영)";
    private const string AggregatingText = "집계 중";
    private const double RefreshDelaySeconds = 3d; // 집계 시각이 지나고 이만큼 기다렸다가 한 번만 다시 받는다. 스냅샷 작업이 끝날 시간

    // 다음 집계까지 남은 시간 표시 상태
    private enum CountdownState
    {
        None,
        Counting,
        WaitingRefresh, // 0초가 지나 다시 받을 때를 기다린다
        Aggregating,    // 다시 받았거나 받는 중. 사용자가 탭을 다시 고르기 전까지 더 받지 않는다
    }

    [ReadOnly] [SerializeField] private ButtonUI btnClose;
    [ReadOnly] [SerializeField] private List<RankingTabButton> listTabButton = new List<RankingTabButton>();
    [ReadOnly] [SerializeField] private ScrollRect scrollRanking;
    [ReadOnly] [SerializeField] private RectTransform rectItemContent;
    [ReadOnly] [SerializeField] private List<RankingItem> listRankingItem = new List<RankingItem>();
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMessage;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMyRankingTitle;
    [ReadOnly] [SerializeField] private RankingItem myRankingItem;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtSummary;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCountdown;

    private RankingType selectedType;

    // 기기 시계는 틀릴 수 있어서, 서버 시각 기준으로 받은 남은 시간을 기기 시계와 상관없는 realtime으로 줄여 간다
    private CountdownState countdownState;
    private double countdownEndRealtime; // 남은 시간이 0이 되는 realtime
    private double refreshRealtime;      // 다시 받을 realtime
    private long shownSeconds = -1;      // 같은 초에는 글자를 다시 만들지 않는다
    private bool isRefreshing;           // 집계 시각이 지나 다시 받은 응답인지
    private DateTime? lastUpdatedAt;

    public event Action<RankingType> OnSelectTypeEvent = delegate { };

    protected override void Awake()
    {
        base.Awake();

        btnClose = GameUtil.Bind<ButtonUI>(transform, "Panel/Header/CloseButton");
        foreach (var type in Types)
        {
            var tabButton = GameUtil.Bind<RankingTabButton>(transform, $"Panel/Tabs/{type.ToString()}Tab");
            tabButton.Init(type);
            tabButton.OnClickTabEvent += OnClickTab;
            listTabButton.Add(tabButton);
        }

        scrollRanking = GameUtil.Bind<ScrollRect>(transform, "Panel/RankingList");
        rectItemContent = GameUtil.Bind<RectTransform>(transform, "Panel/RankingList/Viewport/Content");
        listRankingItem.Add(GameUtil.Bind<RankingItem>(rectItemContent, "Item"));
        txtMessage = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/RankingList/Message");
        txtMyRankingTitle = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/MyRankingTitle");
        myRankingItem = GameUtil.Bind<RankingItem>(transform, "Panel/MyRanking");
        txtSummary = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Footer/Summary");
        txtCountdown = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Footer/Countdown");

        btnClose.OnClickEvent += OnClickClose;
    }

    private void Update()
    {
        switch (countdownState)
        {
            case CountdownState.Counting:
                UpdateCountdown();
                break;
            case CountdownState.WaitingRefresh:
                if (Time.realtimeSinceStartupAsDouble >= refreshRealtime)
                {
                    countdownState = CountdownState.Aggregating;
                    isRefreshing = true;
                    OnSelectTypeEvent.Invoke(selectedType);
                }

                break;
        }
    }

    private void OnDestroy()
    {
        foreach (var tabButton in listTabButton)
        {
            if (tabButton == null)
            {
                continue;
            }

            tabButton.OnClickTabEvent -= OnClickTab;
        }
    }

    // 마지막으로 본 종류를 다시 알려서 여는 쪽이 요청하게 한다. 열 때마다 새로 받는다
    protected override void OnOpen()
    {
        base.OnOpen();

        SelectType(selectedType);
    }

    // 응답이 오는 사이에 다른 탭을 골랐으면 늦게 온 응답은 버린다
    public void SetRanking(RankingType type, RankingApi.RankingResponse response)
    {
        if (type != selectedType)
        {
            return;
        }

        // 상위 목록은 집계본이라 내 순위가 me와 다를 수 있으므로 닉네임으로 나를 찾는다
        string myNickname = response.me?.nickname;
        ShowItems(response.rankings.Count);
        for (int i = 0; i < response.rankings.Count; i++)
        {
            RankingApi.RankingEntryResponse entry = response.rankings[i];
            listRankingItem[i].Set(GetRankText(entry.rank), entry.nickname, GetScoreText(type, entry.score));
            listRankingItem[i].SetMine(myNickname != null && entry.nickname == myNickname);
        }

        SetMessage(response.rankings.Count == 0 ? EmptyMessage : null);
        SetMyRanking(type, response.me, response.rankChanged);
        txtSummary.text = GetSummaryText(response.total, response.updatedAt);
        SetCountdown(response);
    }

    public void SetFailed(RankingType type)
    {
        if (type != selectedType)
        {
            return;
        }

        // 집계 시각이 지나 다시 받다가 실패했으면 보던 목록은 두고 "집계 중"으로 남긴다
        if (isRefreshing)
        {
            isRefreshing = false;
            return;
        }

        SetMessage(FailedMessage);
    }

    // 다시 받았는데 집계 시각이 그대로면 스냅샷이 아직 끝나지 않은 것이다. 이때는 더 받지 않고 "집계 중"으로 둔다
    private void SetCountdown(RankingApi.RankingResponse response)
    {
        bool isStillAggregating = isRefreshing && response.updatedAt == lastUpdatedAt;
        isRefreshing = false;
        lastUpdatedAt = response.updatedAt;

        if (isStillAggregating)
        {
            countdownState = CountdownState.Aggregating;
            txtCountdown.text = AggregatingText;
            return;
        }

        countdownState = CountdownState.Counting;
        countdownEndRealtime = Time.realtimeSinceStartupAsDouble + response.GetTimeUntilNextUpdate().TotalSeconds;
        shownSeconds = -1;
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        double remainingSeconds = countdownEndRealtime - Time.realtimeSinceStartupAsDouble;
        if (remainingSeconds <= 0d)
        {
            countdownState = CountdownState.WaitingRefresh;
            refreshRealtime = Time.realtimeSinceStartupAsDouble + RefreshDelaySeconds;
            txtCountdown.text = AggregatingText;
            return;
        }

        // 남은 0.5초를 0초로 보이지 않게 올림한다
        long seconds = (long)Math.Ceiling(remainingSeconds);
        if (seconds == shownSeconds)
        {
            return;
        }

        shownSeconds = seconds;
        txtCountdown.text = $"다음 집계까지 {GetTimeText(seconds)}";
    }

    private void ResetCountdown()
    {
        countdownState = CountdownState.None;
        isRefreshing = false;
        lastUpdatedAt = null;
        shownSeconds = -1;
        txtCountdown.text = string.Empty;
    }

    private void SelectType(RankingType type)
    {
        selectedType = type;
        foreach (var tabButton in listTabButton)
        {
            tabButton.SetSelected(tabButton.RankingType == type);
        }

        // 이전 종류의 목록이 잠깐이라도 보이지 않게 비우고 맨 위로 올린다
        ShowItems(0);
        SetMessage(LoadingMessage);
        SetMyRanking(type, null, false);
        txtSummary.text = string.Empty;
        ResetCountdown();
        scrollRanking.verticalNormalizedPosition = 1f;

        OnSelectTypeEvent.Invoke(type);
    }

    // 내 기록은 실시간이라 집계본과 다를 수 있다. 다르면 다음 집계 때 바뀔 예상 순위라고 제목에 알린다
    private void SetMyRanking(RankingType type, RankingApi.RankingEntryResponse me, bool rankChanged)
    {
        // 기록이 없는 계정은 서버가 rank 0으로 보낸다
        bool hasRecord = me != null && me.rank > 0;
        txtMyRankingTitle.text = hasRecord && rankChanged
            ? MyRankingEstimatedTitle
            : MyRankingTitle;

        if (me == null)
        {
            myRankingItem.Set("-", NoRecordText, string.Empty);
            return;
        }

        if (hasRecord == false)
        {
            myRankingItem.Set("-", me.nickname, NoRecordText);
            return;
        }

        myRankingItem.Set(GetRankText(me.rank), me.nickname, GetScoreText(type, me.score));
    }

    private void SetMessage(string message)
    {
        txtMessage.gameObject.SetActive(message != null);
        txtMessage.text = message;
    }

    private void ShowItems(int count)
    {
        while (listRankingItem.Count < count)
        {
            listRankingItem.Add(Instantiate(listRankingItem[0], rectItemContent));
        }

        for (int i = 0; i < listRankingItem.Count; i++)
        {
            listRankingItem[i].gameObject.SetActive(i < count);
        }
    }

    private static string GetRankText(int rank)
    {
        return $"{rank.ToString()}위";
    }

    private static string GetScoreText(RankingType type, long score)
    {
        switch (type)
        {
            case RankingType.SwordLevel:
                return $"Lv. {score.ToString()}";
            case RankingType.StageCleared:
                return $"스테이지 {score.ToString()}";
            case RankingType.CombatPower:
                return score.ToString("N0", CultureInfo.InvariantCulture);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    // 상위 목록은 집계본이라 집계 시각을 기기 시간으로 함께 보여준다
    private static string GetSummaryText(long total, DateTime? updatedAt)
    {
        string updatedText = updatedAt.HasValue
            ? $"{updatedAt.Value.ToLocalTime():MM/dd HH:mm} 기준"
            : NotAggregatedText;
        return $"총 {total.ToString("N0", CultureInfo.InvariantCulture)}명 · {updatedText}";
    }

    private static string GetTimeText(long totalSeconds)
    {
        long hours = totalSeconds / 3600;
        long minutes = totalSeconds / 60 % 60;
        long seconds = totalSeconds % 60;
        return $"{hours.ToString("D2")}:{minutes.ToString("D2")}:{seconds.ToString("D2")}";
    }

    private void OnClickTab(RankingTabButton tabButton)
    {
        if (tabButton.RankingType == selectedType)
        {
            return;
        }

        SelectType(tabButton.RankingType);
    }

    private void OnClickClose()
    {
        Close();
    }
}
