using System;

// BattleManager가 알려주는 진행 상황으로 상단 HUD(스테이지, 남은 시간, HP바, 모은 골드)를 갱신한다
// 적을 처치하면 적 자리에서 동전을 띄우고, 동전이 골드 HUD에 닿을 때마다 처치 골드를 나눠서 올린다
public class BattleHudPresenter : IDisposable
{
    private const int EnemyCoinCount = 6;
    private const int BossCoinCount = 12;

    private readonly BattleManager manager;
    private readonly BattleHudView hud;
    private readonly BattleUnitView enemyView;
    private readonly CoinFlyView coinFlyView;

    // 서버가 확정한 골드와 화면에 보이는 골드. 동전이 날아가는 동안에는 보이는 쪽이 뒤따라간다
    private long confirmedGold;
    private long displayedGold;
    private int flyingCoinCount;

    public BattleHudPresenter(BattleManager manager, BattleHudView hud, BattleUnitView enemyView, CoinFlyView coinFlyView)
    {
        this.manager = manager;
        this.hud = hud;
        this.enemyView = enemyView;
        this.coinFlyView = coinFlyView;

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
        confirmedGold = manager.Session.accumulatedGold;
        ShowGold(confirmedGold);
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

    // 처치 골드는 서버 검증을 통과한 뒤 동전으로 날아와서 늘어난다
    // 처치 골드를 동전 수로 나눈 몫만큼 동전마다 올리고, 나머지는 마지막 동전에 얹어 소수점이 생기지 않게 한다
    private void OnSubStageVerified(BattleSubStage subStage, BattleApi.BattleResultVerifyResponse response)
    {
        long gainedGold = response.accumulatedGold - confirmedGold;
        confirmedGold = response.accumulatedGold;

        if (response.resultEnum != BattleSubStageResultEnum.EnemyDead || gainedGold <= 0)
            return;

        // 처치 골드가 동전 수보다 적으면 0골드짜리 동전이 생기지 않게 동전을 줄인다
        var coinCount = (int)Math.Min(subStage.IsBoss ? BossCoinCount : EnemyCoinCount, gainedGold);
        long goldPerCoin = gainedGold / coinCount;
        long remainder = gainedGold % coinCount;
        var arrivedCount = 0;

        flyingCoinCount += coinCount;
        coinFlyView.Play(enemyView.CenterPosition, hud.GoldTarget, coinCount, () =>
        {
            arrivedCount++;
            OnCoinArrived(arrivedCount == coinCount ? goldPerCoin + remainder : goldPerCoin);
        });
    }

    // 날아가는 동전이 모두 닿으면 서버가 확정한 값과 정확히 맞춘다
    private void OnCoinArrived(long gold)
    {
        flyingCoinCount--;
        ShowGold(flyingCoinCount == 0 ? confirmedGold : Math.Min(displayedGold + gold, confirmedGold));
    }

    // 포기로 끝나면 검증 응답 없이 끝나므로 최종 보상으로 한 번 더 맞춘다
    // 아직 날아가는 동전이 있으면 마지막 동전이 닿을 때 맞춰진다
    private void OnBattleEnded(BattleApi.BattleEndResponse result)
    {
        confirmedGold = result.rewards.gold;
        if (flyingCoinCount == 0)
            ShowGold(confirmedGold);
    }

    private void ShowGold(long gold)
    {
        displayedGold = gold;
        hud.SetGold(gold);
    }

    private void SetHp(double playerHp, double enemyHp, BattleSubStage subStage)
    {
        hud.PlayerHpBar.SetHp(playerHp, manager.Session.playerStat.maxHp);
        hud.EnemyHpBar.SetHp(enemyHp, subStage.EnemyStat.maxHp);
    }
}
