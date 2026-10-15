using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleTab : LobbyTab
{
    [ReadOnly] [SerializeField] private StageSelectController stageSelectController;
    [ReadOnly] [SerializeField] private Button btnChat;

    private GameDB GameDB => GameSession.Instance.GameDB;
    private User User => GameSession.Instance.User;

    protected override void Awake()
    {
        base.Awake();

        stageSelectController = GameUtil.Bind<StageSelectController>(transform, "Viewport/Content/StageSelectView");
        stageSelectController.StageEntered += OnStageEntered;

        btnChat = GameUtil.Bind<Button>(transform, "Viewport/Content/StageSelectView/ChatUI/ChatButton");
        btnChat.onClick.AddListener(OnClickChat);
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
    }

    private long CalculateCombatPower()
    {
        CombatStat stat = StatCalculator.CalculatePlayer(GameDB, User);
        return CombatPowerCalculator.Calculate(stat, GameDB);
    }

    private void OnStageEntered(int stage)
    {
        BattleEntry.Set(stage);
        SceneManager.LoadScene(SceneNames.Battle);
    }

    private void OnClickChat()
    {
        LobbyPopupUI.Instance.Get<ChatOverlayView>().Open();
    }
}
