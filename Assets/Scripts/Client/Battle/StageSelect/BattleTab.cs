using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleTab : LobbyTab
{
    [ReadOnly] [SerializeField] private StageSelectController stageSelectController;

    private GameDB GameDB => GameSession.Instance.GameDB;
    private User User => GameSession.Instance.User;

    protected override void Awake()
    {
        base.Awake();

        stageSelectController = GameUtil.Bind<StageSelectController>(transform, "Viewport/Content/StageSelectView");
        stageSelectController.StageEntered += OnStageEntered;
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
}
