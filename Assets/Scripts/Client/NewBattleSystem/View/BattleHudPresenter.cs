using System;

// BattleManager가 알려주는 진행 상황으로 상단 HUD(스테이지, 남은 시간, HP바, 모은 골드)를 갱신한다
public class BattleHudPresenter : IDisposable
{
    private readonly BattleManager manager;
    private readonly BattleHudView hud;

    public BattleHudPresenter(BattleManager manager, BattleHudView hud)
    {
        this.manager = manager;
        this.hud = hud;

        manager.CountdownStarted += OnCountdownStarted;
        manager.CountdownTicked += OnCountdownTicked;
        manager.SubStageStarted += SetUp;
        manager.SubStageAdvanced += OnSubStageAdvanced;
        manager.AttackApplied += OnAttackApplied;
        manager.SubStageVerified += OnSubStageVerified;
        manager.BattleEnded += OnBattleEnded;
    }

    public void Dispose()
    {
        manager.CountdownStarted -= OnCountdownStarted;
        manager.CountdownTicked -= OnCountdownTicked;
        manager.SubStageStarted -= SetUp;
        manager.SubStageAdvanced -= OnSubStageAdvanced;
        manager.AttackApplied -= OnAttackApplied;
        manager.SubStageVerified -= OnSubStageVerified;
        manager.BattleEnded -= OnBattleEnded;
    }

    // 골드는 서버 세션 값으로 시작한다. 새 전투면 0, 재접속이면 끊기기 전까지 모은 골드
    private void OnCountdownStarted(BattleSubStage subStage)
    {
        SetUp(subStage);
        hud.SetGold(manager.Session.accumulatedGold);
    }

    // 세부스테이지 시작 상태로 스테이지 표시, HP바, 남은 시간을 세팅한다
    // 재도전이면 지난 전투에서 줄어든 HP가 여기서 다시 세팅된다
    private void SetUp(BattleSubStage subStage)
    {
        hud.SetStage(manager.Session.stage, subStage.SubStageIndex + 1, subStage.IsBoss);
        SetHp(subStage.PlayerHp, subStage.EnemyHp, subStage);
        hud.SetRemainingTime(subStage.TimeLimit);
    }

    private void OnCountdownTicked(float remaining)
    {
        hud.SetStartCountdown(remaining);
    }

    private void OnSubStageAdvanced(BattleSubStage subStage)
    {
        hud.SetRemainingTime(subStage.TimeLimit - subStage.Elapsed);
    }

    private void OnAttackApplied(BattleSubStage subStage, BattleEvent battleEvent)
    {
        SetHp(battleEvent.playerHp, battleEvent.enemyHp, subStage);
    }

    // 처치 골드는 서버 검증을 통과한 뒤에 늘어난다
    private void OnSubStageVerified(BattleSubStage subStage, BattleResultVerifyResponse response)
    {
        hud.SetGold(response.accumulatedGold);
    }

    // 포기로 끝나면 검증 응답 없이 끝나므로 최종 보상으로 한 번 더 맞춘다
    private void OnBattleEnded(BattleEndResponse result)
    {
        hud.SetGold(result.rewards.gold);
    }

    private void SetHp(double playerHp, double enemyHp, BattleSubStage subStage)
    {
        hud.PlayerHpBar.SetHp(playerHp, manager.Session.playerStat.maxHp);
        hud.EnemyHpBar.SetHp(enemyHp, subStage.EnemyStat.maxHp);
    }
}
