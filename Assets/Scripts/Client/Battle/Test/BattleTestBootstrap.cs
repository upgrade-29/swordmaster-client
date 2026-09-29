using Newtonsoft.Json;
using UnityEngine;

// 서버가 없기 때문에 임시 코드. 테스트 데이터로 가짜 서버(LocalBattleService)를 만들어 BattleDirector에 넘긴다
// BattleDirector와 같은 오브젝트에 붙인다
public class BattleTestBootstrap : MonoBehaviour
{
    [ReadOnly(true)] [SerializeField] private string userFile = "rich"; // rich, high_sword, full_artifacts, poor
    [ReadOnly(true)] [SerializeField] private int randomSeed = -1; // 0 이상이면 매번 같은 전투가 나온다
    [ReadOnly(true)] [SerializeField] private bool logToConsole = true; // 로그 창 문장을 Console에도 찍는다

    private BattleDirector director;
    private BattleLogView logView;
    private BattleUnitView playerView;
    private BattleUnitView enemyView;
    private BattleLogger battleLogger;
    private BattleEffectPresenter effectPresenter;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref director);
        logView = GameUtil.Bind<BattleLogView>(GameObject.Find("BattleCanvas"), "Hud/BattleLogPanel");
        playerView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Player"));
        enemyView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Enemy"));
    }

    private void OnDestroy()
    {
        battleLogger?.Dispose();
        effectPresenter?.Dispose();
    }

    private void Start()
    {
        GameDB gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);
        User user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);
        var random = randomSeed >= 0 ? new System.Random(randomSeed) : new System.Random();

        var battleService = new LocalBattleService(gameDB, user, random);
        director.Init(battleService, user.StageProgress.NextStage);
        battleLogger = new BattleLogger(director, logView, user.Nickname, gameDB, logToConsole);
        effectPresenter = new BattleEffectPresenter(director, playerView, enemyView);

        Debug.Log($"[Battle] Test user '{userFile}' loaded. Next stage {user.StageProgress.NextStage}");
    }
}
