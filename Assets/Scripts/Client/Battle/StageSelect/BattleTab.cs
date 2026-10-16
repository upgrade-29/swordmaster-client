using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTab : LobbyTab
{
    [ReadOnly] [SerializeField] private StageSelectController stageSelectController;
    [ReadOnly] [SerializeField] private Button btnChat;
    [ReadOnly] [SerializeField] private LobbyController lobbyController;

    [ReadOnly] [SerializeField] private BattleResumePopup resumePopup;

    private bool isEntering; // 세션 요청을 기다리는 중. 입장 버튼을 여러 번 눌러도 한 번만 보낸다
    private BattleApi.BattleSessionInfoResponse activeSession; // 이어서 할지 묻는 동안 들고 있는 진행 중이던 세션
    private int enteredStage; // 새로 시작을 고르면 이 스테이지로 시작한다

    private GameDB GameDB => GameSession.Instance.GameDB;
    private User User => GameSession.Instance.User;
    private ConfirmPopup ConfirmPopup => LobbyPopupUI.Instance.Get<ConfirmPopup>();

    protected override void Awake()
    {
        base.Awake();

        stageSelectController = GameUtil.Bind<StageSelectController>(transform, "Viewport/Content/StageSelectView");
        stageSelectController.StageEntered += OnStageEntered;

        btnChat = GameUtil.Bind<Button>(transform, "Viewport/Content/StageSelectView/ChatUI/ChatButton");
        btnChat.onClick.AddListener(OnClickChat);

        lobbyController = GetComponentInParent<LobbyController>();
        lobbyController.OnChangeTabEvent += OnChangeTab;
    }

    // 팝업은 LobbyPopupUI가 Awake에서 모으므로 Start에서 꺼낸다
    private void Start()
    {
        stageSelectController.Init(GameDB.stages, User.StageProgress, CalculateCombatPower());

        resumePopup = LobbyPopupUI.Instance.Get<BattleResumePopup>();
        resumePopup.ContinueClicked += OnContinueClicked;
        resumePopup.RestartClicked += OnRestartClicked;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (stageSelectController != null)
        {
            stageSelectController.StageEntered -= OnStageEntered;
        }

        if (btnChat != null)
        {
            btnChat.onClick.RemoveListener(OnClickChat);
        }

        if (lobbyController != null)
        {
            lobbyController.OnChangeTabEvent -= OnChangeTab;
        }

        if (resumePopup != null)
        {
            resumePopup.ContinueClicked -= OnContinueClicked;
            resumePopup.RestartClicked -= OnRestartClicked;
        }
    }

    // 다른 탭에서 강화나 장착으로 능력치가 바뀌었을 수 있어 이 탭을 고를 때마다 다시 계산한다
    private void OnChangeTab(LobbyTabType tab)
    {
        if (tab != LobbyTabType.Battle)
        {
            return;
        }

        stageSelectController.SetMyPower(CalculateCombatPower());
    }

    private long CalculateCombatPower()
    {
        CombatStat stat = StatCalculator.CalculatePlayer(GameDB, User);
        return CombatPowerCalculator.Calculate(stat, GameDB);
    }

    // 전투 씬으로 넘어가기 전에 세션을 받는다. 진행 중인 세션이 있으면 이어서 할지 묻고, 없으면 새로 시작한다
    // 요청이 실패하면 전투 씬으로 넘어가지 않고 로비에서 알린다
    private void OnStageEntered(int stage)
    {
        if (isEntering)
        {
            return;
        }

        isEntering = true;
        enteredStage = stage;
        HttpRequest.Request(BattleApi.GetActiveSession(GameDB.version), (error, info) =>
        {
            if (this == null || error != null)
            {
                OnSessionResponse(error, info);
                return;
            }

            if (info != null)
            {
                ShowResumePopup(info);
                return;
            }

            StartBattle();
        });
    }

    // 진행 중이던 전투의 요약을 보여주고 이어서 할지 새로 시작할지 고르게 한다
    private void ShowResumePopup(BattleApi.BattleSessionInfoResponse info)
    {
        activeSession = info;

        StageData stageData = GameDB.stages.First(data => data.stage == info.stage);
        resumePopup.Show(info.stage, info.subStageIndex + 1, BattleEngine.GetSubStageCount(stageData),
            info.playerHp, info.playerStat.maxHp, info.accumulatedGold);
    }

    private void OnContinueClicked()
    {
        resumePopup.Close();
        OnSessionResponse(null, activeSession);
    }

    // 진행 중이던 전투를 포기(패배 처리, 모은 골드 지급)하고 고른 스테이지를 새로 시작한다
    private void OnRestartClicked()
    {
        resumePopup.Close();
        HttpRequest.Request(BattleApi.Abandon(activeSession.battleId), (error, result) =>
        {
            if (this == null || error != null)
            {
                OnSessionResponse(error, null);
                return;
            }

            result.ApplyTo(User);
            StartBattle();
        });
    }

    private void StartBattle()
    {
        HttpRequest.Request(BattleApi.Start(enteredStage, GameDB.version), OnSessionResponse);
    }

    private void OnSessionResponse(HttpError error, BattleApi.BattleSessionInfoResponse info)
    {
        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (error == null)
        {
            BattleEntry.Set(info);
            SceneManager.LoadScene(SceneNames.Battle);
            return;
        }

        isEntering = false;
        Debug.LogWarning($"[Battle] Enter failed: {error}");

        // 로그인 만료처럼 HttpErrorHandler가 이미 처리한 에러는 알림을 띄우지 않는다
        if (error.IsHandled)
        {
            return;
        }

        ConfirmPopup.Setup("전투 시작 실패", error.Message, OnCloseAlert, OnCloseAlert);
        ConfirmPopup.Open();
    }

    private void OnCloseAlert()
    {
        ConfirmPopup.Close();
    }

    private void OnClickChat()
    {
        LobbyPopupUI.Instance.Get<ChatOverlayView>().Open();
    }
}
