using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleTab : LobbyTab
{
    private const string BattleSceneName = "BattleScene";

    [ReadOnly] [SerializeField] private StageSelectController stageSelectController;

    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

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
        SwordData sword = GameDB.swords.First(x => x.level == User.Sword.Level);
        CombatStat stat = StatCalculator.CalculatePlayer(sword, GetEquippedArtifactLevels());
        return CombatPowerCalculator.Calculate(stat, GetConfigValue("CRIT_MULTIPLIER"), GetConfigValue("BASE_BATTLE_TIME"));
    }

    private IEnumerable<(ArtifactData data, int level)> GetEquippedArtifactLevels()
    {
        return User.EquippedArtifacts.Select(x => (GameDB.artifacts.First(y => y.code == x.ArtifactCode), x.Level));
    }

    private double GetConfigValue(string key)
    {
        return double.Parse(GameDB.config.First(x => x.key == key).value, CultureInfo.InvariantCulture);
    }

    private void OnStageEntered(int stage)
    {
        BattleEntry.Set(GameDB, User, stage);
        SceneManager.LoadScene(BattleSceneName);
    }
}
