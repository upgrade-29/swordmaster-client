using System;

// BattleManager가 알려주는 진행 상황으로 상단 HUD(스테이지, 남은 시간, HP바)를 갱신한다
public class BattleHudPresenter : IDisposable
{
    private readonly BattleManager manager;
    private readonly BattleHudView hud;

    public BattleHudPresenter(BattleManager manager, BattleHudView hud)
    {
        this.manager = manager;
        this.hud = hud;

        manager.CountdownStarted += SetUp;
        manager.CountdownTicked += OnCountdownTicked;
        manager.SubStageStarted += SetUp;
        manager.SubStageAdvanced += OnSubStageAdvanced;
        manager.AttackApplied += OnAttackApplied;
    }

    public void Dispose()
    {
        manager.CountdownStarted -= SetUp;
        manager.CountdownTicked -= OnCountdownTicked;
        manager.SubStageStarted -= SetUp;
        manager.SubStageAdvanced -= OnSubStageAdvanced;
        manager.AttackApplied -= OnAttackApplied;
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

    private void SetHp(double playerHp, double enemyHp, BattleSubStage subStage)
    {
        hud.PlayerHpBar.SetHp(playerHp, manager.Session.playerStat.maxHp);
        hud.EnemyHpBar.SetHp(enemyHp, subStage.EnemyStat.maxHp);
    }
}
