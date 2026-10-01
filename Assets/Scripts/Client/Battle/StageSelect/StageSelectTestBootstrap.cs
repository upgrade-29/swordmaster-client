using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

// 서버가 없기 때문에 임시 코드. 테스트 데이터로 유저와 GameDB를 만들어 StageSelectController에 넘긴다
// 전투 씬에서 돌아오면 StageSelectEntry로 받은 유저를 쓴다
// 입장 이벤트를 받아 유저 정보와 함께 BattleScene을 여는 것도 임시로 여기서 한다. 로비와 합치면 이 구독처만 바뀐다
public class StageSelectTestBootstrap : MonoBehaviour
{
    private const string BattleSceneName = "BattleScene";

    [ReadOnly(true)] [SerializeField] private string userFile = "rich"; // rich, high_sword, full_artifacts, poor

    private StageSelectController controller;
    private GameDB gameDB;
    private User user;

    private void Awake()
    {
        controller = GameUtil.TryGetComponent<StageSelectController>(GameObject.Find("StageSelectView"));
    }

    private void Start()
    {
        // 전투 씬에서 돌아왔으면 전투 결과가 반영된 유저를 쓰고, 아니면 테스트 파일을 쓴다
        if (StageSelectEntry.TryConsume(out gameDB, out user) == false)
        {
            gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);
            user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);
        }

        controller.Init(gameDB.stages, user.StageProgress, CalculateMyPower(gameDB, user));
        controller.StageEntered += OnStageEntered;
    }

    private void OnDestroy()
    {
        if (controller != null)
            controller.StageEntered -= OnStageEntered;
    }

    private static long CalculateMyPower(GameDB gameDB, User user)
    {
        var battleDB = new BattleDB(gameDB);
        SwordData sword = battleDB.GetSword(user.Sword.Level);
        var equippedArtifacts = user.EquippedArtifacts
            .Select(artifact => (battleDB.GetArtifact(artifact.ArtifactCode), artifact.Level));
        CombatStat stat = StatCalculator.CalculatePlayer(sword, equippedArtifacts);

        string baseBattleTime = gameDB.config.First(data => data.key == "BASE_BATTLE_TIME").value;
        return CombatPowerCalculator.Calculate(stat, battleDB.critMultiplier,
            double.Parse(baseBattleTime, CultureInfo.InvariantCulture));
    }

    private void OnStageEntered(int stage)
    {
        Debug.Log($"[StageSelect] Enter stage {stage}");
        BattleEntry.Set(gameDB, user, stage);
        SceneManager.LoadScene(BattleSceneName);
    }
}
