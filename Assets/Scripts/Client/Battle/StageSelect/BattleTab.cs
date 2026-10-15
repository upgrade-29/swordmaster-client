using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTab : LobbyTab
{
    [ReadOnly] [SerializeField] private StageSelectController stageSelectController;
    [ReadOnly] [SerializeField] private Button btnChat;
    [ReadOnly] [SerializeField] private LobbyController lobbyController;

    private bool isEntering; // 세션 요청을 기다리는 중. 입장 버튼을 여러 번 눌러도 한 번만 보낸다

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

    private void Start()
    {
        stageSelectController.Init(GameDB.stages, User.StageProgress, CalculateCombatPower());
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

    // 전투 씬으로 넘어가기 전에 세션을 받는다. 진행 중인 세션이 있으면 그 세션으로, 없으면 새로 시작한다
    // 요청이 실패하면 전투 씬으로 넘어가지 않고 로비에서 알린다
    private void OnStageEntered(int stage)
    {
        if (isEntering)
        {
            return;
        }

        isEntering = true;
        HttpRequest.Request(BattleApi.GetActiveSession(GameDB.version), (error, info) =>
        {
            if (this == null || error != null || info != null)
            {
                OnSessionResponse(error, info);
                return;
            }

            HttpRequest.Request(BattleApi.Start(stage, GameDB.version), OnSessionResponse);
        });
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
