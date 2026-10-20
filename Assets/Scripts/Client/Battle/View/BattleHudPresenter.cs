using System;
using DG.Tweening;
using UnityEngine;

// BattleManager가 알려주는 진행 상황으로 상단 HUD(스테이지, 남은 시간, HP바, 모은 골드)를 갱신한다
// 적을 처치하면 적 자리에서 동전을 띄우고, 동전이 골드 HUD에 닿을 때마다 처치 골드를 나눠서 올린다
public class BattleHudPresenter : IDisposable
{
    private const int EnemyCoinCount = 6;
    private const int BossCoinCount = 12;
    private const float EnemyUiFadeDuration = 0.2f; // 적 이름판과 체력바가 처치 때 사라지고 다음 적 도착 때 나타나는 시간

    private readonly BattleManager manager;
    private readonly BattleHudView hud;
    private readonly NamePlateView namePlateView;
    private readonly BattleUnitView enemyView;
    private readonly CoinFlyView coinFlyView;

    // 서버가 확정한 골드와 화면에 보이는 골드. 동전이 날아가는 동안에는 보이는 쪽이 뒤따라간다
    private long confirmedGold;
    private long displayedGold;
    private int flyingCoinCount;
    private double lastCountdownNumber;
    private double lastTimeWarningSecond; // 남은 시간 경고 효과음을 낸 마지막 정수 초. 세부스테이지마다 0으로 되돌린다

    // 처치 연출에서 적이 튕겨 날아가기 시작하는 시각(Time.time). 동전은 이때보다 먼저 나오지 않는다
    private float enemyDefeatTime;
    private Tween coinDelay;

    public BattleHudPresenter(BattleManager manager, BattleHudView hud, NamePlateView namePlateView, BattleUnitView enemyView,
        CoinFlyView coinFlyView, string playerNickname)
    {
        this.manager = manager;
        this.hud = hud;
        this.namePlateView = namePlateView;
        this.enemyView = enemyView;
        this.coinFlyView = coinFlyView;

        namePlateView.SetPlayerName(playerNickname);

        manager.CountdownStarted += OnCountdownStarted;
        manager.CountdownTicked += OnCountdownTicked;
        manager.BattleStarted += OnBattleStarted;
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
        manager.BattleStarted -= OnBattleStarted;
        manager.SubStageStarted -= SetUp;
        manager.SubStageAdvanced -= OnSubStageAdvanced;
        manager.AttackApplied -= OnAttackApplied;
        manager.SubStageVerified -= OnSubStageVerified;
        manager.BattleEnded -= OnBattleEnded;
        DOTween.Kill(this);
        coinDelay?.Kill();
    }

    /// <summary>
    /// 다음 적이 도착했을 때 적 이름과 체력을 바꾸고 이름판과 체력바를 보여 준다.
    /// </summary>
    public void ShowNextEnemy(BattleSubStage next)
    {
        namePlateView.SetEnemyName(next.Enemy.name);
        hud.EnemyHpBar.SetHp(next.EnemyHp, next.EnemyStat.maxHp);
        SetEnemyUiVisible(true, EnemyUiFadeDuration / manager.Speed);
    }

    // 골드는 서버 세션 값으로 시작한다. 새 전투면 0, 재접속이면 끊기기 전까지 모은 골드
    // 재도전이면 지난 전투에서 숨긴 적 이름판과 체력바를 바로 다시 보여 준다
    private void OnCountdownStarted(BattleSubStage subStage)
    {
        SetUp(subStage);
        SetEnemyUiVisible(true, 0f);
        confirmedGold = manager.Session.accumulatedGold;
        ShowGold(confirmedGold);
    }

    // 세부스테이지 시작 상태로 스테이지 표시, 적 이름, HP바, 남은 시간을 세팅한다
    // 재도전이면 지난 전투에서 줄어든 HP가 여기서 다시 세팅된다
    // 아직 나오지 않은 체력바 반응이 새로 세팅한 HP를 덮어쓰지 않게 먼저 버린다
    private void SetUp(BattleSubStage subStage)
    {
        DOTween.Kill(this);
        hud.SetStage(manager.Stage.name, manager.Session.stage, subStage.SubStageIndex + 1, subStage.IsBoss);
        namePlateView.SetEnemyName(subStage.Enemy.name);
        SetHp(subStage.PlayerHp, subStage.EnemyHp, subStage);
        hud.SetRemainingTime(subStage.TimeLimit);
        lastTimeWarningSecond = 0;
    }

    private void OnCountdownTicked(float remaining)
    {
        hud.SetStartCountdown(remaining);

        double number = Math.Ceiling(remaining);
        if (number != lastCountdownNumber)
        {
            lastCountdownNumber = number;
            SoundManager.Instance.PlaySfx(GameConstants.Sounds.Countdown);
        }
    }

    private void OnBattleStarted(BattleApi.BattleSessionInfoResponse session)
    {
        lastCountdownNumber = 0;
        hud.HideStartCountdown();
        SoundManager.Instance.PlaySfx(GameConstants.Sounds.BattleStart);
    }

    // 남은 시간 경고 중에는 정수 초가 바뀔 때마다 효과음을 낸다. 숫자 맥박과 같은 순간이다
    private void OnSubStageAdvanced(BattleSubStage subStage)
    {
        double remaining = subStage.TimeLimit - subStage.Elapsed;
        hud.SetRemainingTime(remaining);

        double second = Math.Ceiling(remaining);
        if (remaining > 0d && remaining <= BattleHudView.TimeWarningSeconds && second != lastTimeWarningSecond)
        {
            lastTimeWarningSecond = second;
            SoundManager.Instance.PlaySfx(GameConstants.Sounds.TimeWarning);
        }
    }

    // 맞은 쪽은 줄어드는 연출, 흡혈한 쪽은 차오르는 연출이 나온다
    // 검이 닿는 순간에 반응하도록 공격 모션만큼 늦춘다
    private void OnAttackApplied(BattleSubStage subStage, BattleEvent battleEvent)
    {
        DOVirtual.DelayedCall(BattleUnitView.AttackHitDelay / manager.Speed, () =>
        {
            hud.PlayerHpBar.ChangeHp(battleEvent.playerHp, manager.Session.playerStat.maxHp, manager.Speed);
            hud.EnemyHpBar.ChangeHp(battleEvent.enemyHp, subStage.EnemyStat.maxHp, manager.Speed);
        }, false).SetTarget(this);

        // 처치했으면 적이 튕겨 날아가기 시작할 때 이름판과 체력바를 숨긴다
        if (battleEvent.enemyHp <= 0)
        {
            float flyStartDelay = (BattleUnitView.AttackHitDelay + BattleUnitView.DefeatHitstopDuration) / manager.Speed;
            enemyDefeatTime = Time.time + flyStartDelay;
            DOVirtual.DelayedCall(flyStartDelay, () => SetEnemyUiVisible(false, EnemyUiFadeDuration / manager.Speed), false)
                .SetTarget(this);

            // 일반 적을 처치하면 적이 다 날아간 뒤 처치 회복만큼 체력바를 채운다. 다음 세부스테이지는 이 체력으로 시작한다
            if (subStage.IsBoss == false && subStage.HealOnKill > 0)
            {
                double healedHp = battleEvent.playerHp + subStage.HealOnKill;
                float knockoutEnd = (BattleUnitView.AttackHitDelay + BattleUnitView.DefeatDuration) / manager.Speed;
                DOVirtual.DelayedCall(knockoutEnd, () => hud.PlayerHpBar.ChangeHp(healedHp, manager.Session.playerStat.maxHp, manager.Speed), false)
                    .SetTarget(this);
            }
        }
    }

    private void SetEnemyUiVisible(bool visible, float duration)
    {
        namePlateView.SetEnemyVisible(visible, duration);
        hud.EnemyHpBar.SetVisible(visible, duration);
    }

    // 처치 골드는 서버 검증을 통과한 뒤 동전으로 날아와서 늘어난다
    // 한 방에 처치하면 검이 닿기도 전에 응답이 오므로, 적이 튕겨 날아가기 시작할 때까지 동전을 늦춘다
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

        // 다음 세부스테이지의 SetUp이 DOTween.Kill(this)로 지우지 않게 따로 들고 있는다
        float delay = Mathf.Max(0f, enemyDefeatTime - Time.time);
        coinDelay = DOVirtual.DelayedCall(delay, () =>
            coinFlyView.Play(enemyView.CenterPosition, hud.GoldIconPosition, coinCount, manager.Speed, () =>
            {
                arrivedCount++;
                OnCoinArrived(arrivedCount == coinCount ? goldPerCoin + remainder : goldPerCoin);
            }), false);
    }

    // 날아가는 동전이 모두 닿으면 서버가 확정한 값과 정확히 맞춘다
    private void OnCoinArrived(long gold)
    {
        flyingCoinCount--;
        ShowGold(flyingCoinCount == 0 ? confirmedGold : Math.Min(displayedGold + gold, confirmedGold));
        hud.PlayGoldArrived(manager.Speed);
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
