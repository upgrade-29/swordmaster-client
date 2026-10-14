using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

// 서버의 배틀 세션을 받아 세부스테이지 전투를 직접 진행한다
// 세부스테이지가 끝나면 요약을 보내 검증받고, 통과하면 서버가 알려 준 체력으로 다음 세부스테이지를 시작한다
// 화면은 직접 건드리지 않고 이벤트로만 알린다. HP바, 로그, 이펙트는 이 이벤트를 구독해서 만든다
public class BattleManager : MonoBehaviour
{
    public const float BossWarningDuration = 1.8f;
    public const float MaxSpeed = 2f; // 서버 시간 검증과 같은 배속 상한
    private const float StartDelay = 2f;
    private const int ReportMaxAttempts = 3; // 통신 오류로 보고 응답을 못 받으면 다시 보낸다. 서버는 같은 보고에 같은 응답을 준다
    private const float RequestRetryDelay = 1f; // 다시 보내기 전에 기다리는 시간(초)

    // 전투 규칙에 쓰는 값. 서버 검증도 같은 값을 써야 결과가 같다
    public const double TimeEpsilon = 1e-9; // 부동소수점 오차 때문에 같은 시각의 공격이 미세하게 어긋나는 것을 같은 시각으로 본다
    public const string CritMultiplierConfigKey = "CRIT_MULTIPLIER"; // GameDB.config 키
    public const string HealRateConfigKey = "HEAL_RATE";
    public const string NormalTimeLimitConfigKey = "NORMAL_BATTLE_TIME_LIMIT";
    public const string BossTimeLimitConfigKey = "BOSS_BATTLE_TIME_LIMIT";

    // 세션을 받아 StartDelay 대기를 시작할 때. 새 전투와 재접속 모두. 첫 세부스테이지를 미리 넘겨서 시작 전 화면을 세팅할 수 있게 한다
    public event Action<BattleSubStage> CountdownStarted;
    public event Action<float> CountdownTicked; // 매 프레임 남은 대기 시간(초)
    public event Action<BattleApi.BattleSessionInfoResponse> BattleStarted;
    public event Action<BattleSubStage> SubStageStarted;
    public event Action<BattleSubStage> SubStageAdvanced; // 매 프레임 진행한 뒤. 남은 시간 표시용
    public event Action<BattleSubStage> BossWarningStarted; // 끝날 때까지 공격은 시작하지 않는다
    public event Action<BattleSubStage, BattleEvent> AttackApplied;
    public event Action<BattleSubStage, BattleApi.BattleResultVerifyResponse> SubStageVerified;
    public event Action<BattleApi.BattleEndResponse> BattleEnded;
    public event Action<string> RequestRejected; // 유저에게 보여 줄 거절 문구. 검증에 실패하면 서버가 이미 패배로 끝낸 상태다

    private GameDB gameDB;
    private BattleEngine engine;
    private string gameDataVersion; // 서버가 같은 데이터로 계산하는지 확인하도록 모든 요청에 담는다
    private BattleApi.BattleSessionInfoResponse session;
    private Coroutine battleRoutine;
    private bool isBusy; // 요청을 기다리거나 전투를 진행하는 중

    public float Speed { get; private set; } = 1f;
    public BattleApi.BattleSessionInfoResponse Session => session;
    public StageData Stage { get; private set; }
    public int SubStageCount => Stage == null ? 0 : BattleEngine.GetSubStageCount(Stage);
    public BattleSubStage CurrentSubStage { get; private set; } // 남은 시간, 체력 표시용
    public bool IsBusy => isBusy;

    public void Init(GameDB gameDB)
    {
        this.gameDB = gameDB;
        engine = new BattleEngine(gameDB);
        gameDataVersion = gameDB.version;
    }

    // 진행 중인 세션이 있으면 이어서 하고, 없으면 새로 시작한다
    public void EnterBattle(int stage)
    {
        if (isBusy)
            return;

        isBusy = true;
        Request(BattleApi.GetActiveSession(gameDataVersion), info =>
        {
            if (info != null)
                Begin(info);
            else
                Request(BattleApi.Start(stage, gameDataVersion), Begin);
        });
    }

    // 결과 팝업의 재도전. 방금 한 스테이지를 새로 시작한다
    public void Retry()
    {
        if (isBusy || session == null)
            return;

        isBusy = true;
        Request(BattleApi.Start(session.stage, gameDataVersion), Begin);
    }

    // 나가기. 서버가 패배로 처리하고 그때까지 모은 골드를 반영한다
    public void Abandon()
    {
        if (isBusy == false || session == null)
            return;

        if (battleRoutine != null)
            StopCoroutine(battleRoutine);
        battleRoutine = null; // 기다리던 보고 응답이 뒤늦게 와도 무시한다

        Request(BattleApi.Abandon(session.battleId), Finish);
    }

    public void SetSpeed(float speed)
    {
        Speed = Mathf.Clamp(speed, 1f, MaxSpeed);
    }

    // 서버 연결이 불안정할 경우 maxAttempts번까지 다시 보내고, 서버가 거절하면 바로 끝낸다. 401은 HttpErrorHandler가 로그인 씬으로 보낸다
    // 에러코드가 BattleRequestErrorEnum에 정의되어 있으면 RequestRejected 이벤트를 호출한다.
    // 성공적인 응답이 오면 onSuccess를 호출한다. onSuccess에서 다음 요청을 보내면 된다.
    // 전투 중 세부스테이지마다 보내므로 로딩 표시는 하지 않는다.
  
    private void Request<T>(HttpRequestInfo<T> requestInfo, Action<T> onSuccess, int maxAttempts = 1, int attempt = 1)
        where T : class
    {
        // 전투 중 세부스테이지마다 보내므로 로딩 표시는 하지 않는다
        HttpRequest.Request(requestInfo, (error, result) =>
        {
            if (this == null)
                return;

            if (error == null)
            {
                onSuccess(result);
                return;
            }

            if (TryParseErrorEnum(error.Code, out BattleRequestErrorEnum errorEnum))
            {
                Debug.LogWarning($"[Battle] Request rejected: {errorEnum} {error}");
                isBusy = false;
                RequestRejected?.Invoke(error.Message); // 서버가 보낸 문구
                return;
            }

            if (error.IsHandled == false && attempt < maxAttempts)
            {
                Debug.LogWarning($"[Battle] Request failed, retry {attempt}/{maxAttempts - 1}: {error}");
                StartCoroutine(RequestAfterDelay(requestInfo, onSuccess, maxAttempts, attempt + 1));
                return;
            }

            isBusy = false;
        }, showLoading: false);
    }

    // HttpRequestInfo는 보낼 때마다 요청을 새로 만들므로 같은 인스턴스를 다시 보낸다
    private IEnumerator RequestAfterDelay<T>(HttpRequestInfo<T> requestInfo, Action<T> onSuccess, int maxAttempts,
        int attempt) where T : class
    {
        yield return new WaitForSecondsRealtime(RequestRetryDelay);
        Request(requestInfo, onSuccess, maxAttempts, attempt);
    }

    private static bool TryParseErrorEnum(string code, out BattleRequestErrorEnum errorEnum)
    {
        errorEnum = BattleRequestErrorEnum.None;
        if (string.IsNullOrEmpty(code))
            return false;

        try
        {
            errorEnum = JsonConvert.DeserializeObject<BattleRequestErrorEnum>($"\"{code}\"");
        }
        catch (JsonException)
        {
            return false; // 클라가 모르는 코드
        }

        return errorEnum != BattleRequestErrorEnum.None;
    }

    private void Begin(BattleApi.BattleSessionInfoResponse info)
    {
        if (info == null)
            return;

        session = info;
        Stage = gameDB.stages.First(data => data.stage == info.stage);
        battleRoutine = StartCoroutine(RunBattle());
    }

    private IEnumerator RunBattle()
    {
        BattleSubStage subStage = CreateSubStage(session.subStageIndex, session.playerHp);
        CountdownStarted?.Invoke(subStage);

        for (float remaining = StartDelay; remaining > 0f; remaining -= Time.deltaTime)
        {
            CountdownTicked?.Invoke(remaining);
            yield return null;
        }

        BattleStarted?.Invoke(session);
        yield return RunSubStageAndReport(subStage);
    }

    private IEnumerator RunSubStageAndReport(BattleSubStage subStage)
    {
        yield return RunSubStage(subStage);

        var request = BattleApi.BattleResultVerifyRequest.From(session.battleId, subStage);
        Request(BattleApi.Report(request, gameDataVersion), verified => OnSubStageVerified(subStage, verified),
            ReportMaxAttempts);
    }

    // 거절되면 Request에서 끝난다. 검증 실패면 서버가 이미 패배로 끝냈고, 데이터 버전이 다르면 세션이 남아 있어 다시 접속하면 이어서 한다
    private void OnSubStageVerified(BattleSubStage subStage, BattleApi.BattleResultVerifyResponse verified)
    {
        if (battleRoutine == null) // 응답을 기다리는 동안 포기했다
            return;

        SubStageVerified?.Invoke(subStage, verified);

        if (verified.IsBattleEnded)
        {
            Finish(verified.battleEnd);
            return;
        }

        BattleSubStage next = CreateSubStage(verified.nextSubStageIndex, verified.nextPlayerHp);
        battleRoutine = StartCoroutine(RunSubStageAndReport(next));
    }

    private BattleSubStage CreateSubStage(int subStageIndex, double playerHp)
    {
        return new BattleSubStage(engine, Stage, subStageIndex, session.playerStat, playerHp, session.battleSeed);
    }

    private IEnumerator RunSubStage(BattleSubStage subStage)
    {
        CurrentSubStage = subStage;
        subStage.AttackApplied += battleEvent => AttackApplied?.Invoke(subStage, battleEvent);
        SubStageStarted?.Invoke(subStage);

        if (subStage.IsBoss)
        {
            BossWarningStarted?.Invoke(subStage);
            yield return new WaitForSeconds(BossWarningDuration);
        }

        while (subStage.IsFinished == false)
        {
            subStage.Advance(Time.deltaTime * Speed);
            SubStageAdvanced?.Invoke(subStage);
            yield return null;
        }
    }

    private void Finish(BattleApi.BattleEndResponse result)
    {
        battleRoutine = null;
        isBusy = false;
        BattleEnded?.Invoke(result);
    }
}
