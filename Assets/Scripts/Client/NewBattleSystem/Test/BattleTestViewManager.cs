using UnityEngine;
using UnityEngine.SceneManagement;

// 서버가 없기 때문에 임시 코드. 로비에서 BattleEntry로 받은 GameDB와 유저로 가짜 서버(LocalBattleSessionService)를 만들어
// BattleManager에 넘기고, 전투 씬의 뷰들을 연결한다. 받은 값이 없으면(전투 씬을 바로 연 경우) 로비로 돌려보낸다
// BattleManager와 같은 오브젝트에 붙인다
// 결과 팝업의 나가기를 받아 로비 씬으로 돌아가는 것도 임시로 여기서 한다
public class BattleTestViewManager : MonoBehaviour
{
    private const string LobbySceneName = "LobbyScene";
    private const float ExitFadeDuration = 0.5f;

    // 가짜 서버는 씬이 바뀌어도 남아 있어야 세션(Redis 역할)이 유지된다. 유저가 바뀌면 새로 만든다
    private static LocalBattleSessionService battleService;
    private static User battleServiceUser;

    [ReadOnly(true)] [SerializeField] private int randomSeed = -1; // 0 이상이면 매번 같은 시드로 전투가 나온다
    [ReadOnly(true)] [SerializeField] private bool logToConsole = true; // 로그 창 문장을 Console에도 찍는다
    [ReadOnly(true)] [SerializeField] [Range(1f, BattleManager.MaxSpeed)] private float speed = 1f;

    private BattleManager manager;
    private BattleHudView hud;
    private BattleLogView logView;
    private BattleUnitView playerView;
    private BattleUnitView enemyView;
    private BossWarningView bossWarningView;
    private BattleResultView resultView;
    private ScreenFader screenFader;
    private BattleHudPresenter hudPresenter;
    private BattleLogger battleLogger;
    private BattleEffectPresenter effectPresenter;
    private GameDB gameDB;
    private User user;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref manager);
        hud = GameUtil.Bind<BattleHudView>(GameObject.Find("BattleCanvas"), "Hud");
        logView = GameUtil.Bind<BattleLogView>(GameObject.Find("BattleCanvas"), "Hud/BattleLogPanel");
        playerView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Player"));
        enemyView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Enemy"));
        bossWarningView = GameUtil.Bind<BossWarningView>(GameObject.Find("BattleCanvas"), "BossWarningPanel");
        resultView = GameUtil.Bind<BattleResultView>(GameObject.Find("BattleCanvas"), "ResultPopup");
        screenFader = GameUtil.Bind<ScreenFader>(GameObject.Find("BattleCanvas"), "FadePanel");
    }

    private void OnDestroy()
    {
        resultView.RetryClicked -= manager.Retry;
        resultView.ExitClicked -= OnExitClicked;
        hudPresenter?.Dispose();
        battleLogger?.Dispose();
        effectPresenter?.Dispose();
    }

    private void Start()
    {
        // 데이터는 로비에서만 읽는다. 로비를 거치지 않고 열었으면 로비로 돌아간다
        if (BattleEntry.TryConsume(out gameDB, out user, out int stage) == false)
        {
            Debug.LogWarning("[Battle] No entry data. Back to lobby");
            SceneManager.LoadScene(LobbySceneName);
            return;
        }

        Debug.Log($"[Battle] Entered from lobby. Start stage {stage} (next stage {user.StageProgress.NextStage})");

        if (battleService == null || battleServiceUser != user)
        {
            var random = randomSeed >= 0 ? new System.Random(randomSeed) : new System.Random();
            battleService = new LocalBattleSessionService(gameDB, user, random);
            battleServiceUser = user;
        }

        manager.Init(battleService, gameDB);
        manager.SetSpeed(speed);
        hudPresenter = new BattleHudPresenter(manager, hud);
        battleLogger = new BattleLogger(manager, logView, user.Nickname, logToConsole);
        effectPresenter = new BattleEffectPresenter(manager, playerView, enemyView, bossWarningView, resultView, gameDB);

        resultView.RetryClicked += manager.Retry;
        resultView.ExitClicked += OnExitClicked;

        manager.EnterBattle(stage);
    }

    // 어두워진 뒤 전투 결과가 반영된 유저를 들고 로비 씬으로 돌아간다. 로비는 전투 탭에서 시작한다
    private void OnExitClicked()
    {
        screenFader.FadeOut(ExitFadeDuration, () =>
        {
            StageSelectEntry.Set(gameDB, user);
            SceneManager.LoadScene(LobbySceneName);
        });
    }
}
