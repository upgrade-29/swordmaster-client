using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// 서버의 배틀 세션을 받아 세부스테이지 전투를 직접 진행한다
// 세부스테이지가 끝나면 요약을 보내 검증받고, 통과하면 서버가 알려 준 체력으로 다음 세부스테이지를 시작한다
// 화면은 직접 건드리지 않고 이벤트로만 알린다. HP바, 로그, 이펙트는 이 이벤트를 구독해서 만든다
public class BattleManager : MonoBehaviour
{
    public const float BossWarningDuration = 1.8f;
    public const float MaxSpeed = 2f; // 서버 시간 검증과 같은 배속 상한
    private const float StartDelay = 2f;

    // 세션을 받아 StartDelay 대기를 시작할 때. 새 전투와 재접속 모두. 첫 세부스테이지를 미리 넘겨서 시작 전 화면을 세팅할 수 있게 한다
    public event Action<BattleSubStage> CountdownStarted;
    public event Action<float> CountdownTicked; // 매 프레임 남은 대기 시간(초)
    public event Action<BattleSessionInfo> BattleStarted;
    public event Action<BattleSubStage> SubStageStarted;
    public event Action<BattleSubStage> SubStageAdvanced; // 매 프레임 진행한 뒤. 남은 시간 표시용
    public event Action<BattleSubStage> BossWarningStarted; // 끝날 때까지 공격은 시작하지 않는다
    public event Action<BattleSubStage, BattleEvent> AttackApplied;
    public event Action<BattleSubStage, BattleResultVerifyResponse> SubStageVerified;
    public event Action<BattleEndResponse> BattleEnded;
    public event Action<string> RequestRejected; // 유저에게 보여 줄 거절 문구. 검증에 실패하면 서버가 이미 패배로 끝낸 상태다

    private IBattleSessionService battleService;
    private BattleDB battleDB;
    private BattleEngine engine;
    private BattleSessionInfo session;
    private Coroutine battleRoutine;
    private bool isBusy; // 요청을 기다리거나 전투를 진행하는 중

    public float Speed { get; private set; } = 1f;
    public BattleSessionInfo Session => session;
    public StageData Stage { get; private set; }
    public int SubStageCount => Stage == null ? 0 : BattleEngine.GetSubStageCount(Stage);
    public BattleSubStage CurrentSubStage { get; private set; } // 남은 시간, 체력 표시용
    public bool IsBusy => isBusy;

    public void Init(IBattleSessionService battleService, GameDB gameDB)
    {
        this.battleService = battleService;
        battleDB = new BattleDB(gameDB);
        engine = new BattleEngine(battleDB);
    }

    // 진행 중인 세션이 있으면 이어서 하고, 없으면 새로 시작한다
    public async void EnterBattle(int stage)
    {
        if (isBusy)
            return;

        isBusy = true;
        BattleSessionInfo info = await Request(async () =>
            await battleService.GetActiveSessionAsync() ?? await battleService.StartBattleAsync(stage));
        Begin(info);
    }

    // 결과 팝업의 재도전. 방금 한 스테이지를 새로 시작한다
    public async void Retry()
    {
        if (isBusy || session == null)
            return;

        isBusy = true;
        BattleSessionInfo info = await Request(() => battleService.StartBattleAsync(session.stage));
        Begin(info);
    }

    // 나가기. 서버가 패배로 처리하고 그때까지 모은 골드를 반영한다
    public async void Abandon()
    {
        if (isBusy == false || session == null)
            return;

        if (battleRoutine != null)
            StopCoroutine(battleRoutine);
        battleRoutine = null;

        BattleEndResponse result = await Request(() => battleService.AbandonAsync(session.battleId));
        if (result != null)
            Finish(result);
    }

    public void SetSpeed(float speed)
    {
        Speed = Mathf.Clamp(speed, 1f, MaxSpeed);
    }

    // 실패하면 거절 문구를 알리고 null을 돌려준다. 기다리는 동안 씬이 바뀌었어도 null
    private async Task<T> Request<T>(Func<Task<T>> request) where T : class
    {
        try
        {
            T response = await request();
            return this == null ? null : response;
        }
        catch (BattleRequestException e)
        {
            Debug.LogWarning($"[Battle] Request rejected: {e.Reason}");
            if (this != null)
            {
                isBusy = false;
                RequestRejected?.Invoke(e.Message);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (this != null)
                isBusy = false;
        }

        return null;
    }

    private void Begin(BattleSessionInfo info)
    {
        if (info == null)
            return;

        session = info;
        Stage = battleDB.GetStage(info.stage);
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

        while (true)
        {
            yield return RunSubStage(subStage);

            Task<BattleResultVerifyResponse> verifyTask = Request(() =>
                battleService.ReportSubStageAsync(BattleResultVerifyRequest.From(session.battleId, subStage)));
            yield return new WaitUntil(() => verifyTask.IsCompleted);

            BattleResultVerifyResponse verified = verifyTask.Result;
            if (verified == null)
                yield break; // 거절. 서버가 이미 패배로 끝냈다

            SubStageVerified?.Invoke(subStage, verified);

            if (verified.IsBattleEnded)
            {
                Finish(verified.battleEnd);
                yield break;
            }

            subStage = CreateSubStage(verified.nextSubStageIndex, verified.nextPlayerHp);
        }
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

    private void Finish(BattleEndResponse result)
    {
        battleRoutine = null;
        isBusy = false;
        BattleEnded?.Invoke(result);
    }
}
