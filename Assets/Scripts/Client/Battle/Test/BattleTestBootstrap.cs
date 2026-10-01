using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

// 서버가 없기 때문에 임시 코드. 테스트 데이터로 가짜 서버(LocalBattleService)를 만들어 BattleDirector에 넘긴다
// BattleDirector와 같은 오브젝트에 붙인다
// 결과 팝업의 나가기를 받아 스테이지 선택 씬으로 돌아가는 것도 임시로 여기서 한다. 로비와 합치면 이 부분만 바뀐다
public class BattleTestBootstrap : MonoBehaviour
{
    private const string StageSelectSceneName = "StageSelectScene";
    private const float ExitFadeDuration = 0.5f;

    [ReadOnly(true)] [SerializeField] private string userFile = "rich"; // rich, high_sword, full_artifacts, poor
    [ReadOnly(true)] [SerializeField] private int randomSeed = -1; // 0 이상이면 매번 같은 전투가 나온다
    [ReadOnly(true)] [SerializeField] private bool logToConsole = true; // 로그 창 문장을 Console에도 찍는다

    private BattleDirector director;
    private BattleLogView logView;
    private BattleUnitView playerView;
    private BattleUnitView enemyView;
    private BossWarningView bossWarningView;
    private BattleResultView resultView;
    private ScreenFader screenFader;
    private BattleLogger battleLogger;
    private BattleEffectPresenter effectPresenter;
    private GameDB gameDB;
    private User user;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref director);
        logView = GameUtil.Bind<BattleLogView>(GameObject.Find("BattleCanvas"), "Hud/BattleLogPanel");
        playerView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Player"));
        enemyView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Enemy"));
        bossWarningView = GameUtil.Bind<BossWarningView>(GameObject.Find("BattleCanvas"), "BossWarningPanel");
        resultView = GameUtil.Bind<BattleResultView>(GameObject.Find("BattleCanvas"), "ResultPopup");
        screenFader = GameUtil.Bind<ScreenFader>(GameObject.Find("BattleCanvas"), "FadePanel");
    }

    private void OnDestroy()
    {
        resultView.RetryClicked -= director.Retry;
        resultView.ExitClicked -= OnExitClicked;
        battleLogger?.Dispose();
        effectPresenter?.Dispose();
    }

    private void Start()
    {
        // 스테이지 선택 화면에서 넘어왔으면 받은 유저와 고른 스테이지를 쓰고, 아니면 테스트 파일과 다음에 도전할 스테이지를 쓴다
        if (BattleEntry.TryConsume(out gameDB, out user, out int stage))
        {
            Debug.Log($"[Battle] Entered from stage select. Start stage {stage} (next stage {user.StageProgress.NextStage})");
        }
        else
        {
            gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);
            user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);
            stage = user.StageProgress.NextStage;
            Debug.Log($"[Battle] Test user '{userFile}' loaded. Next stage {stage}");
        }

        var random = randomSeed >= 0 ? new System.Random(randomSeed) : new System.Random();
        var battleService = new LocalBattleService(gameDB, user, random);
        director.Init(battleService, stage);
        battleLogger = new BattleLogger(director, logView, user.Nickname, gameDB, logToConsole);
        effectPresenter = new BattleEffectPresenter(director, playerView, enemyView, bossWarningView, resultView, gameDB);

        resultView.RetryClicked += director.Retry;
        resultView.ExitClicked += OnExitClicked;
    }

    // 어두워진 뒤 전투 결과가 반영된 유저를 들고 스테이지 선택 씬으로 돌아간다
    private void OnExitClicked()
    {
        screenFader.FadeOut(ExitFadeDuration, () =>
        {
            StageSelectEntry.Set(gameDB, user);
            SceneManager.LoadScene(StageSelectSceneName);
        });
    }
}
