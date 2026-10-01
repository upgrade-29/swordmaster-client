using System;
using System.Collections;
using UnityEngine;

// 서버가 준 BattleResult를 시간 순서대로 재생한다. 계산은 하지 않고 받은 값만 보여준다
// 재생 중에 일어난 일은 이벤트로만 알린다. 로그 같은 부가 기능은 이 이벤트를 구독해서 만든다
public class BattleDirector : MonoBehaviour
{
    public const float BossWarningDuration = 1.8f;
    private const float StartDelay = 2f;

    public event Action CountdownStarted; // 서버가 전투를 받아들여 StartDelay 대기를 시작할 때. 처음 입장과 재도전 모두
    public event Action<BattleResult> BattleStarted;
    public event Action<BattleResult, BattleWave, int> WaveStarted; // int는 1부터 세는 웨이브 번호
    public event Action<BattleWave, BattleEvent> AttackApplied;
    public event Action<BattleWave> BossWarningStarted; // 보스전 시작 전 경고 연출. 끝날 때까지 공격은 시작하지 않는다
    public event Action<BattleWave> WaveEnded;
    public event Action<BattleResult> BattleEnded;
    public event Action<string> RequestRejected; // 쿨타임, 잠긴 스테이지 같은 거절 사유

    private BattleUnitView enemyView;
    private BattleHudView hud;

    private IBattleService battleService;
    private int stage;
    private bool isPlaying;
    private float resultReceivedTime;

    // 서버 응답을 받은 뒤 지난 시간. 쿨타임은 응답을 받은 시점부터 흐르므로 재생이 끝난 뒤 남은 시간을 구할 때 뺀다
    public float SecondsSinceResult => Time.unscaledTime - resultReceivedTime;

    private void Awake()
    {
        enemyView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Enemy"));
        hud = GameUtil.Bind<BattleHudView>(GameObject.Find("BattleCanvas"), "Hud");
    }

    // 들어오면 전투를 요청하고, 받아들여지면 StartDelay 뒤에 재생한다
    public void Init(IBattleService battleService, int stage)
    {
        this.battleService = battleService;
        this.stage = stage;

        RequestAndPlay();
    }

    // 결과 팝업의 재도전. 처음 입장과 같이 StartDelay를 기다린 뒤 재생한다
    // 대기 중이거나 전투 중이거나 서버가 거절하면 아무 일도 일어나지 않는다(거절 사유는 RequestRejected로 알린다)
    public void Retry()
    {
        RequestAndPlay();
    }

    private async void RequestAndPlay()
    {
        if (isPlaying || battleService == null)
            return;

        isPlaying = true;

        BattleResult result;
        try
        {
            result = await battleService.RequestBattleAsync(stage);
        }
        catch (BattleRequestException e)
        {
            RequestRejected?.Invoke(e.Message);
            isPlaying = false;
            return;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            isPlaying = false;
            return;
        }

        // 기다리는 동안 씬이 바뀌어서 이 오브젝트가 사라졌으면 재생하지 않는다
        if (this == null)
            return;

        resultReceivedTime = Time.unscaledTime;
        StartCoroutine(PlayBattle(result));
    }

    private IEnumerator PlayBattle(BattleResult result)
    {
        yield return CountDown(result);

        BattleStarted?.Invoke(result);

        for (var i = 0; i < result.waves.Count; i++)
        {
            BattleWave wave = result.waves[i];
            yield return PlayWave(result, wave, i + 1);

            if (wave.outcome != WaveOutcome.EnemyDead)
                break;

            enemyView.SetVisible(false);
        }

        BattleEnded?.Invoke(result);

        // 재도전은 이기든 지든 방금 한 스테이지를 다시 한다
        isPlaying = false;
    }

    // 첫 웨이브 직전 상태로 화면을 되돌리고, 남은 시간 자리에 시작 안내를 보여주며 StartDelay만큼 기다린다
    // 재도전이면 지난 전투에서 쓰러진 적과 줄어든 HP가 여기서 다시 세팅된다
    private IEnumerator CountDown(BattleResult result)
    {
        CountdownStarted?.Invoke();
        SetUpWave(result, result.waves[0], 1);

        for (float remaining = StartDelay; remaining > 0f; remaining -= Time.deltaTime)
        {
            hud.SetStartCountdown(remaining);
            yield return null;
        }
    }

    // 웨이브 시작 상태로 스테이지 표시, 적, HP바를 세팅한다
    private void SetUpWave(BattleResult result, BattleWave wave, int waveNumber)
    {
        hud.SetStage(result.stage, waveNumber, wave.isBoss);
        enemyView.SetVisible(true);
        hud.PlayerHpBar.SetHp(wave.playerStartHp, result.playerStat.maxHp);
        hud.EnemyHpBar.SetHp(wave.enemyStat.maxHp, wave.enemyStat.maxHp);
    }

    private IEnumerator PlayWave(BattleResult result, BattleWave wave, int waveNumber)
    {
        SetUpWave(result, wave, waveNumber);

        WaveStarted?.Invoke(result, wave, waveNumber);

        if (wave.isBoss)
        {
            BossWarningStarted?.Invoke(wave);
            yield return new WaitForSeconds(BossWarningDuration);
        }

        double elapsed = 0;
        var nextEventIndex = 0;
        while (true)
        {
            // 마지막 이벤트 시각이 duration이므로, duration에서 멈추면 모든 이벤트가 적용된다
            elapsed = Math.Min(elapsed + Time.deltaTime, wave.duration);

            // 한 프레임 동안 지나간 이벤트를 모두 적용한다
            while (nextEventIndex < wave.events.Count && wave.events[nextEventIndex].time <= elapsed)
            {
                ApplyEvent(result, wave, wave.events[nextEventIndex]);
                nextEventIndex++;
            }

            hud.SetRemainingTime(wave.timeLimit - elapsed);

            if (elapsed >= wave.duration)
                break;

            yield return null;
        }

        WaveEnded?.Invoke(wave);
    }

    private void ApplyEvent(BattleResult result, BattleWave wave, BattleEvent battleEvent)
    {
        hud.PlayerHpBar.SetHp(battleEvent.playerHp, result.playerStat.maxHp);
        hud.EnemyHpBar.SetHp(battleEvent.enemyHp, wave.enemyStat.maxHp);

        AttackApplied?.Invoke(wave, battleEvent);
    }
}
