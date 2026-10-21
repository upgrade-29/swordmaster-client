using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 전투 씬의 진입점. 로비에서 BattleEntry로 받은 배틀 세션과 GameSession의 GameDB로 BattleManager를 초기화하고, 전투 씬의 뷰들을 연결한다
// 받은 값이 없으면(전투 씬을 바로 연 경우) 로비로, 로그인하지 않았으면 로그인 씬으로 돌려보낸다
// BattleManager와 같은 오브젝트에 붙인다
// 결과 팝업의 나가기를 받아 로비 씬으로 돌아간다. 전투 결과는 GameSession의 유저에 이미 반영되어 있다
// 옵션 팝업이나 포기 확인 팝업이 떠 있는 동안은 전투를 멈춘다
public class BattleViewManager : MonoBehaviour
{
    private const float NextEnemyResumeDelay = 0.3f; // 다음 적이 도착하고 전투를 다시 시작하기까지(1배속 기준)
    private const string AbandonTitle = "전투 포기";
    private const string AbandonMessage = "전투를 포기하시겠습니까?\n지금까지 모은 골드만 받고 패배 처리됩니다.";

    [ReadOnly(true)] [SerializeField] private bool logToConsole = true; // 로그 창 문장을 Console에도 찍는다
    [ReadOnly(true)] [SerializeField] [Range(1f, GameConstants.Rules.MaxBattleSpeed)] private float speed = 1f;

    private BattleManager manager;
    private BattleHudView hud;
    private NamePlateView namePlateView;
    private BattleLogView logView;
    private BattleUnitView playerView;
    private BattleUnitView enemyView;
    private BossWarningView bossWarningView;
    private BattleBackgroundView backgroundView;
    private BattleDefeatView defeatView;
    private BattleVfxView vfxView;
    private CoinFlyView coinFlyView;
    private Button optionButton;
    private BattleHudPresenter hudPresenter;
    private BattleLogger battleLogger;
    private BattleEffectPresenter effectPresenter;
    private GameDB gameDB;
    private User user;

    private BattleResultPopup ResultPopup => Popup.Instance.Get<BattleResultPopup>();
    private BattleOptionPopup OptionPopup => Popup.Instance.Get<BattleOptionPopup>();
    private DialogPopup DialogPopup => Popup.Instance.Get<DialogPopup>();

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref manager);
        hud = GameUtil.Bind<BattleHudView>(GameObject.Find("BattleCanvas"), "Hud");
        logView = GameUtil.Bind<BattleLogView>(GameObject.Find("BattleCanvas"), "Hud/BattleLogPanel");
        playerView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Player"));
        enemyView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Enemy"));
        namePlateView = GameUtil.TryGetComponent<NamePlateView>(GameObject.Find("NamePlateCanvas"));
        backgroundView = GameUtil.TryGetComponent<BattleBackgroundView>(GameObject.Find("Background"));
        vfxView = GameUtil.TryGetComponent<BattleVfxView>(GameObject.Find("Vfx"));
        bossWarningView = GameUtil.Bind<BossWarningView>(GameObject.Find("BattleCanvas"), "BossWarningPanel");
        coinFlyView = GameUtil.Bind<CoinFlyView>(GameObject.Find("BattleCanvas"), "CoinLayer");
        defeatView = GameUtil.Bind<BattleDefeatView>(GameObject.Find("BattleCanvas"), "DefeatOverlay");
        optionButton = GameUtil.Bind<Button>(GameObject.Find("BattleCanvas"), "Hud/OptionButton");
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // 멈춘 채로 씬을 떠나도 다음 씬은 정상 속도로 돈다
        optionButton.onClick.RemoveListener(OnClickOption);

        // 씬을 닫을 때 Popup이 먼저 파괴됐으면 Instance로 새로 만들지 않는다. 팝업도 함께 파괴되므로 구독을 풀 필요가 없다
        if (Popup.IsInitialize())
        {
            OptionPopup.AbandonClicked -= OnAbandonClicked;
            OptionPopup.OnOpenEvent -= OnPausePopupChanged;
            OptionPopup.OnCloseEvent -= OnPausePopupChanged;
            DialogPopup.OnOpenEvent -= OnPausePopupChanged;
            DialogPopup.OnCloseEvent -= OnPausePopupChanged;
        }

        manager.BusyChanged -= OnBusyChanged;
        manager.BattleEnded -= OnBattleEnded;
        manager.NextSubStageTransition = null;
        hudPresenter?.Dispose();
        battleLogger?.Dispose();
        effectPresenter?.Dispose();
    }

    private void Start()
    {
        // 데이터는 로비에서만 읽는다. 로비를 거치지 않고 열었으면 로비로 돌아간다
        if (BattleEntry.TryConsume(out BattleApi.BattleSessionInfoResponse session) == false ||
            GameSession.Instance.User == null)
        {
            Debug.LogWarning("[Battle] No entry data. Back to lobby");
            SceneLoader.Instance.Load(GameConstants.Scenes.Lobby);
            return;
        }

        gameDB = GameSession.Instance.GameDB;
        user = GameSession.Instance.User;

        // 전투는 서버가 세션을 들고 검증하므로 로그인해야 한다
        if (GameSession.Instance.IsLoggedIn == false)
        {
            Debug.LogWarning("[Battle] Not logged in. Back to login");
            SceneLoader.Instance.Load(GameConstants.Scenes.Title);
            return;
        }

        Debug.Log($"[Battle] Entered from lobby. Start stage {session.stage} (next stage {user.StageProgress.NextStage}), " +
                  $"server {ServerSettings.BaseUrl}");

        // 플레이어는 지금 강화 단계의 검 이미지로 보여준다. 전투 중에는 강화하지 않으므로 한 번만 바꾼다
        SwordData sword = gameDB.swords.First(x => x.level == user.Sword.Level);
        playerView.SetSprite(sword.code);

        // 배경은 이 스테이지의 이미지로 바꾼다. 재도전도 같은 스테이지라서 한 번만 바꾼다
        StageData stage = gameDB.stages.First(x => x.stage == session.stage);
        backgroundView.SetStage(stage.imageCode);

        manager.Init(gameDB);
        manager.SetSpeed(speed);
        hudPresenter = new BattleHudPresenter(manager, hud, namePlateView, enemyView, coinFlyView, user.Nickname);
        battleLogger = new BattleLogger(manager, logView, user.Nickname, logToConsole);
        effectPresenter = new BattleEffectPresenter(manager, playerView, enemyView, bossWarningView, backgroundView, defeatView, vfxView,
            Camera.main.transform, gameDB, manager.Retry, OnExitClicked);

        manager.BusyChanged += OnBusyChanged;
        manager.BattleEnded += OnBattleEnded;
        manager.NextSubStageTransition = PlayNextSubStageTransition;

        optionButton.interactable = false; // 전투가 시작되면 OnBusyChanged에서 켠다
        optionButton.onClick.AddListener(OnClickOption);
        OptionPopup.AbandonClicked += OnAbandonClicked;
        OptionPopup.OnOpenEvent += OnPausePopupChanged;
        OptionPopup.OnCloseEvent += OnPausePopupChanged;
        DialogPopup.OnOpenEvent += OnPausePopupChanged;
        DialogPopup.OnCloseEvent += OnPausePopupChanged;

        SoundManager.Instance.PlayBgm(GameConstants.Sounds.BgmBattle);
        manager.EnterBattle(session);
    }

    // 처치한 적이 날아가고 다음 적이 도착하면, 다음 적의 이름판과 체력바를 보여 주고 잠깐 뒤에 전투를 다시 시작한다
    // 다음 적이 보스면 다가오는 동안 띄운 보스 경고가 끝날 때까지 더 기다린다
    private IEnumerator PlayNextSubStageTransition(BattleSubStage next)
    {
        yield return effectPresenter.PlayNextEnemyEntrance(next);
        hudPresenter.ShowNextEnemy(next);
        yield return new WaitForSeconds(NextEnemyResumeDelay / manager.Speed);
        yield return effectPresenter.WaitBossWarning();
    }

    // 서버가 확정한 재화, 진행도, 아티팩트를 유저에 반영한다. 로비로 돌아가면 이 유저로 화면을 그린다
    private void OnBattleEnded(BattleApi.BattleEndResponse result)
    {
        result.ApplyTo(user);
    }

    // 요청을 기다리거나 전투 중이면 재도전을 막는다. 결과나 거절·실패 응답을 받으면 다시 누를 수 있다
    // 옵션(포기)은 반대로 전투 중에만 쓸 수 있다. 전투가 끝나면 열려 있던 옵션 팝업도 닫는다
    private void OnBusyChanged(bool isBusy)
    {
        ResultPopup.SetRetryInteractable(isBusy == false);
        optionButton.interactable = isBusy;

        if (isBusy == false)
        {
            OptionPopup.Close();
            DialogPopup.Close();
        }
    }

    private void OnClickOption()
    {
        if (manager.IsBusy == false)
        {
            return;
        }

        OptionPopup.Open();
    }

    private void OnAbandonClicked()
    {
        OptionPopup.Close();
        DialogPopup.Setup(AbandonTitle, AbandonMessage,
            new DialogPopup.DialogPopupButtonInfo("확인", OnConfirmAbandon),
            new DialogPopup.DialogPopupButtonInfo("취소", DialogPopup.Close));
        DialogPopup.Open();
    }

    // 포기 응답이 오면 BattleEnded로 패배 결과 팝업이 뜬다
    private void OnConfirmAbandon()
    {
        DialogPopup.Close();
        manager.Abandon();
    }

    // 둘 중 하나라도 떠 있으면 카운트다운, 보스 경고, 전투 진행, 연출을 모두 멈춘다
    private void OnPausePopupChanged(BasePopup popup)
    {
        bool isPaused = OptionPopup.IsOpened || DialogPopup.IsOpened;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // 로딩 씬을 거쳐 로비 씬으로 돌아간다. 로비는 전투 탭에서 시작한다
    private void OnExitClicked()
    {
        SceneLoader.Instance.Load(GameConstants.Scenes.Lobby);
    }
}
