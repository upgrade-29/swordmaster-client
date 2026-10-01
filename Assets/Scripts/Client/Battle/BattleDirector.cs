using System;
using System.Collections;
using UnityEngine;

// 서버가 준 BattleResult를 시간 순서대로 재생한다. 계산은 하지 않고 받은 값만 보여준다
// 재생 중에 일어난 일은 이벤트로만 알린다. 로그 같은 부가 기능은 이 이벤트를 구독해서 만든다
public class BattleDirector : MonoBehaviour
{
    public const float BossWarningDuration = 1.8f;
    private const float StartDelay = 2f;

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

    private void Awake()
    {
        enemyView = GameUtil.TryGetComponent<BattleUnitView>(GameObject.Find("Enemy"));
        hud = GameUtil.Bind<BattleHudView>(GameObject.Find("BattleCanvas"), "Hud");
    }

    // 들어오면 StartDelay 뒤에 전투를 시작한다
    public void Init(IBattleService battleService, int stage)
    {
        this.battleService = battleService;
        this.stage = stage;
        hud.SetStage(stage, 1, false);

        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        for (float remaining = StartDelay; remaining > 0f; remaining -= Time.deltaTime)
        {
            hud.SetStartCountdown(remaining);
            yield return null;
        }

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

        StartCoroutine(PlayBattle(result));
    }

    private IEnumerator PlayBattle(BattleResult result)
    {
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

        // 다음에 도전할 스테이지도 서버가 준 값을 따른다. 이기면 다음 스테이지, 지면 같은 스테이지다
        stage = result.stageProgress.NextStage;
        isPlaying = false;
    }

    private IEnumerator PlayWave(BattleResult result, BattleWave wave, int waveNumber)
    {
        hud.SetStage(result.stage, waveNumber, wave.isBoss);
        enemyView.SetVisible(true);
        hud.PlayerHpBar.SetHp(wave.playerStartHp, result.playerStat.maxHp);
        hud.EnemyHpBar.SetHp(wave.enemyStat.maxHp, wave.enemyStat.maxHp);

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
