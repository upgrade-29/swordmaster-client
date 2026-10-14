using UnityEngine;
using UnityEngine.SceneManagement;

// 전투 씬의 진입점. 로비에서 BattleEntry로 받은 스테이지와 GameSession의 GameDB로 BattleManager를 초기화하고, 전투 씬의 뷰들을 연결한다
// 받은 값이 없으면(전투 씬을 바로 연 경우) 로비로, 로그인하지 않았으면 로그인 씬으로 돌려보낸다
// BattleManager와 같은 오브젝트에 붙인다
// 결과 팝업의 나가기를 받아 로비 씬으로 돌아간다. 전투 결과는 GameSession의 유저에 이미 반영되어 있다
public class BattleViewManager : MonoBehaviour
{
    private const float ExitFadeDuration = 0.5f;

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
        manager.BattleEnded -= OnBattleEnded;
        resultView.ExitClicked -= OnExitClicked;
        hudPresenter?.Dispose();
        battleLogger?.Dispose();
        effectPresenter?.Dispose();
    }

    private void Start()
    {
        // 데이터는 로비에서만 읽는다. 로비를 거치지 않고 열었으면 로비로 돌아간다
        if (BattleEntry.TryConsume(out int stage) == false || GameSession.Instance.User == null)
        {
            Debug.LogWarning("[Battle] No entry data. Back to lobby");
            SceneManager.LoadScene(SceneNames.Lobby);
            return;
        }

        gameDB = GameSession.Instance.GameDB;
        user = GameSession.Instance.User;

        // 전투는 서버가 세션을 들고 검증하므로 로그인해야 한다
        if (GameSession.Instance.IsLoggedIn == false)
        {
            Debug.LogWarning("[Battle] Not logged in. Back to login");
            SceneManager.LoadScene(SceneNames.Login);
            return;
        }

        Debug.Log($"[Battle] Entered from lobby. Start stage {stage} (next stage {user.StageProgress.NextStage}), " +
                  $"server {ServerSettings.BaseUrl}");

        manager.Init(gameDB);
        manager.SetSpeed(speed);
        hudPresenter = new BattleHudPresenter(manager, hud);
        battleLogger = new BattleLogger(manager, logView, user.Nickname, logToConsole);
        effectPresenter = new BattleEffectPresenter(manager, playerView, enemyView, bossWarningView, resultView, gameDB);

        resultView.RetryClicked += manager.Retry;
        manager.BattleEnded += OnBattleEnded;
        resultView.ExitClicked += OnExitClicked;

        manager.EnterBattle(stage);
    }

    // 서버가 확정한 재화, 진행도, 아티팩트를 유저에 반영한다. 로비로 돌아가면 이 유저로 화면을 그린다
    private void OnBattleEnded(BattleApi.BattleEndResponse result)
    {
        result.ApplyTo(user);
    }

    // 어두워진 뒤 로비 씬으로 돌아간다. 로비는 전투 탭에서 시작한다
    private void OnExitClicked()
    {
        screenFader.FadeOut(ExitFadeDuration, () =>
        {
            SceneManager.LoadScene(SceneNames.Lobby);
        });
    }
}
