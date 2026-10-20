using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

// BattleManager가 알려주는 공격마다 공격/피격 모션과 데미지 숫자를 보여준다. 적 처치/다음 적 등장, 화면 흔들림, 보스 경고, 결과 팝업도 띄운다
// 승리·패배 연출이 있으면 결과 팝업은 서버 결과가 오고 연출도 끝난 뒤에 띄운다
// 진행 타이밍이나 HP는 모른다
public class BattleEffectPresenter : IDisposable
{
    private static readonly Vector2 LifestealOffset = new Vector2(0.6f, 0.3f); // x는 상대 반대쪽으로

    // 다음 적 등장: 적이 다 날아가고 잠깐 쉰 뒤, 배경이 흐르면서 다음 적이 땅에 붙어 다가온다
    private const float NextEnemyWaitDuration = 0.15f;
    private const float EntranceDistance = 6f;
    private const float EntranceDuration = 1.2f;
    private const Ease EntranceEase = Ease.InOutQuad;

    // 화면 흔들림: 공격이 닿을 때 카메라를 흔든다. 타격과 피격은 맞은 쪽이 잃은 체력 비율이 클수록 세게 흔든다
    private const float HitShakeStrength = 0.025f; // 플레이어의 공격이 적에게 닿을 때
    private const float HitShakeDuration = 0.2f;
    private const float DamagedShakeStrength = 0.025f; // 적의 공격이 플레이어에게 닿을 때
    private const float DamagedShakeDuration = 0.2f;
    private const float DefeatShakeStrength = 0.25f; // 처치 공격이 닿을 때. 항상 같은 세기
    private const float DefeatShakeDuration = 0.25f;
    private const float StrongHitShakeScale = 1.5f; // 최대 체력의 StrongHitDamageRatio 이상을 잃었을 때의 배율
    private const int ShakeVibrato = 20;
    private const float ShakeRandomness = 90f;

    // 전투 종료: 보스가 다 날아가거나, 플레이어가 다 날아가거나, TIME OVER가 다 나타난 뒤 결과 팝업까지 기다린다
    private const float ResultPopupDelay = 0.9f;

    // 플레이어 사망: 맞는 순간부터 연출 전체(DOTween)를 잠깐 느리게 돌린다. 전투는 이미 끝나서 결과는 같다
    private const float DeathSlowMotionScale = 0.3f;
    private const float DeathSlowMotionDuration = 0.5f; // 느려진 동안의 게임 시간(1배속 기준)

    private readonly BattleManager manager;
    private readonly BattleUnitView playerView;
    private readonly BattleUnitView enemyView;
    private readonly BossWarningView bossWarningView;
    private readonly BattleBackgroundView backgroundView;
    private readonly BattleDefeatView defeatView;
    private readonly Transform trnsCamera;
    private readonly Vector3 vecCameraPosition;
    private readonly Action onRetry;
    private readonly Action onExit;
    private readonly Dictionary<string, string> artifactNamesByCode;
    private readonly DamageFloaterPool damageFloaters = new DamageFloaterPool();

    // 처치 공격부터 적이 다 날아갈 때까지. 다음 적 등장은 이게 끝난 뒤에 시작한다
    private bool isDefeatPlaying;
    private float bossWarningEndTime; // 다음 적 등장 중에 띄운 보스 경고가 끝나는 시각(Time.time)

    // 승리·패배 연출부터 결과 팝업 대기까지. 이 동안 온 결과는 보관했다가 끝나면 띄운다
    private bool isEndPlaying;
    private BattleApi.BattleEndResponse pendingResult;

    private BattleResultPopup ResultPopup => Popup.Instance.Get<BattleResultPopup>();

    // 결과 팝업의 재도전과 나가기에서 할 일은 여는 쪽(BattleViewManager)이 정해서 넘긴다
    public BattleEffectPresenter(BattleManager manager, BattleUnitView playerView, BattleUnitView enemyView,
        BossWarningView bossWarningView, BattleBackgroundView backgroundView, BattleDefeatView defeatView, Transform trnsCamera,
        GameDB gameDB, Action onRetry, Action onExit)
    {
        this.manager = manager;
        this.playerView = playerView;
        this.enemyView = enemyView;
        this.bossWarningView = bossWarningView;
        this.backgroundView = backgroundView;
        this.defeatView = defeatView;
        this.trnsCamera = trnsCamera;
        vecCameraPosition = trnsCamera.position;
        this.onRetry = onRetry;
        this.onExit = onExit;
        artifactNamesByCode = gameDB.artifacts.ToDictionary(data => data.code, data => data.name);

        manager.CountdownStarted += OnCountdownStarted;
        manager.SubStageStarted += OnSubStageStarted;
        manager.SubStageAdvanced += OnSubStageAdvanced;
        manager.BossWarningStarted += OnBossWarningStarted;
        manager.AttackApplied += OnAttackApplied;
        manager.BattleEnded += OnBattleEnded;
        manager.RequestRejected += OnRequestRejected;
    }

    public void Dispose()
    {
        manager.CountdownStarted -= OnCountdownStarted;
        manager.SubStageStarted -= OnSubStageStarted;
        manager.SubStageAdvanced -= OnSubStageAdvanced;
        manager.BossWarningStarted -= OnBossWarningStarted;
        manager.AttackApplied -= OnAttackApplied;
        manager.BattleEnded -= OnBattleEnded;
        manager.RequestRejected -= OnRequestRejected;
        DOTween.Kill(this);
        DOTween.timeScale = 1f; // 느린 화면 도중에 씬을 떠나도 다음 씬의 연출은 정상 속도로 돈다
        trnsCamera.DOKill();
        damageFloaters.Dispose();
    }

    /// <summary>
    /// 처치한 적이 다 날아가기를 기다렸다가, 배경을 흘리며 다음 적이 다가오게 한다. 다음 적이 제자리에 도착하면 끝난다.
    /// 다음 적이 보스면 다가오기 시작할 때 보스 경고도 띄운다.
    /// </summary>
    public IEnumerator PlayNextEnemyEntrance(BattleSubStage next)
    {
        while (isDefeatPlaying)
        {
            yield return null;
        }

        float speed = manager.Speed;
        enemyView.SetSprite(next.Enemy.code);
        enemyView.PlayEntrance(EntranceDistance, EntranceDuration, EntranceEase, speed);
        playerView.PlayWalk(EntranceDuration, speed);
        backgroundView.Scroll(EntranceDistance, EntranceDuration, EntranceEase, speed);
        if (next.IsBoss)
        {
            bossWarningView.Play(next.Enemy.name, BattleManager.BossWarningDuration, speed);
            bossWarningEndTime = Time.time + BattleManager.BossWarningDuration / speed;
        }

        yield return new WaitForSeconds(EntranceDuration / speed);
    }

    /// <summary>
    /// 다음 적 등장 중에 띄운 보스 경고가 끝날 때까지 기다린다. 경고가 끝나기 전에는 공격을 시작하지 않는다.
    /// </summary>
    public IEnumerator WaitBossWarning()
    {
        while (Time.time < bossWarningEndTime)
        {
            yield return null;
        }
    }

    // 재도전이 받아들여져 시작 대기에 들어가면 팝업을 닫고, 지난 전투에서 쓰러진 적을 제자리에 다시 세운다
    // 지난 전투에서 아직 나오지 않은 피격 반응은 버린다
    private void OnCountdownStarted(BattleSubStage subStage)
    {
        DOTween.Kill(this);
        DOTween.timeScale = 1f;
        isDefeatPlaying = false;
        isEndPlaying = false;
        pendingResult = null;
        defeatView.Hide();
        ResultPopup.Close();
        playerView.ResetPose();
        enemyView.ResetPose();
        enemyView.SetSprite(subStage.Enemy.code);
    }

    // 다음 적의 검 이미지로 바꿔서 보여준다. 보스 경고는 이 뒤에 뜨므로 경고 중에도 보스 이미지가 보인다
    private void OnSubStageStarted(BattleSubStage subStage)
    {
        enemyView.SetSprite(subStage.Enemy.code);
        enemyView.SetVisible(true);
    }

    // 시간 초과는 공격이 아니라 진행 중에 정해지므로, 매 프레임 진행 결과에서 감지한다. 세부스테이지마다 한 번만 끝난다
    private void OnSubStageAdvanced(BattleSubStage subStage)
    {
        if (subStage.ResultEnum != BattleSubStageResultEnum.TimeOver)
            return;

        defeatView.ShowTimeOver(manager.Speed);
        WaitEndEffect(BattleDefeatView.FadeDuration + ResultPopupDelay);
    }

    private void OnBossWarningStarted(BattleSubStage subStage)
    {
        bossWarningView.Play(subStage.Enemy.name, BattleManager.BossWarningDuration, manager.Speed);
    }

    private void OnAttackApplied(BattleSubStage subStage, BattleEvent battleEvent)
    {
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        BattleUnitView attacker = isPlayerAttack ? playerView : enemyView;
        BattleUnitView target = isPlayerAttack ? enemyView : playerView;

        attacker.PlayAttack(target.transform.position, manager.Speed);

        // 처치는 공격하는 순간 정해지므로, 검증 응답이 먼저 와도 다음 적 등장이 처치 연출을 기다리게 바로 표시한다
        bool isDefeat = isPlayerAttack && battleEvent.enemyHp <= 0;
        bool isPlayerDead = isPlayerAttack == false && battleEvent.playerHp <= 0;
        float knockoutEnd = BattleUnitView.AttackHitDelay + BattleUnitView.DefeatDuration; // 맞은 쪽이 다 날아갈 때까지
        if (isDefeat)
        {
            isDefeatPlaying = true;
            DOVirtual.DelayedCall((knockoutEnd + NextEnemyWaitDuration) / manager.Speed, () => isDefeatPlaying = false, false).SetTarget(this);
        }

        // 보스는 마지막 세부스테이지라서 보스를 처치하면 승리다
        if ((isDefeat && subStage.IsBoss) || isPlayerDead)
        {
            WaitEndEffect(knockoutEnd + ResultPopupDelay);
        }

        // 검이 닿는 순간에 맞는 쪽이 반응하도록 공격 모션만큼 늦춘다. 전투 계산과 로그는 이미 끝난 상태다
        DOVirtual.DelayedCall(BattleUnitView.AttackHitDelay / manager.Speed,
                () => PlayHitReaction(subStage, battleEvent, attacker, target, isDefeat || isPlayerDead), false)
            .SetTarget(this);
    }

    // isKnockout이면 맞은 쪽이 튕겨 날아간다. 적을 처치했거나 플레이어가 쓰러졌을 때다
    private void PlayHitReaction(BattleSubStage subStage, BattleEvent battleEvent, BattleUnitView attacker, BattleUnitView target, bool isKnockout)
    {
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        double targetMaxHp = isPlayerAttack ? subStage.EnemyStat.maxHp : manager.Session.playerStat.maxHp;
        if (isKnockout)
        {
            attacker.Freeze(BattleUnitView.DefeatHitstopDuration / manager.Speed);
            target.PlayDefeat(Mathf.Sign(target.transform.position.x - attacker.transform.position.x), manager.Speed);
            ShakeCamera(DefeatShakeStrength, DefeatShakeDuration);

            // 플레이어가 쓰러지면 맞는 순간부터 느려지고, 날아가기 시작할 때 화면이 어두워진다
            if (isPlayerAttack == false)
            {
                PlayDeathSlowMotion();
                DOVirtual.DelayedCall(BattleUnitView.DefeatHitstopDuration / manager.Speed, () => defeatView.ShowDim(manager.Speed), false)
                    .SetTarget(this);
            }
        }
        else
        {
            target.PlayHit(battleEvent.isCrit, manager.Speed);
            float hitStrength = Mathf.Clamp01((float)(battleEvent.damage / targetMaxHp) / GameConstants.BattleEffect.StrongHitDamageRatio);
            float strengthScale = Mathf.Lerp(1f, StrongHitShakeScale, hitStrength);
            if (isPlayerAttack)
            {
                ShakeCamera(HitShakeStrength * strengthScale, HitShakeDuration);
            }
            else
            {
                ShakeCamera(DamagedShakeStrength * strengthScale, DamagedShakeDuration);
            }
        }

        Color color = battleEvent.isCrit ? GameConstants.Colors.CritDamage : isPlayerAttack ? GameConstants.Colors.EnemyDamage : GameConstants.Colors.PlayerDamage;
        float sizeScale = DamageFloaterPool.GetSizeScale(battleEvent.damage, targetMaxHp);
        damageFloaters.Show(target.HeadPosition, battleEvent.damage.ToString(GameConstants.NumberFormats.Decimal), color, sizeScale, battleEvent.isCrit,
            manager.Speed);

        // 흡혈은 공격자 머리 위에 회복량을 띄운다. 체력이 가득 차서 회복하지 못했으면 보여주지 않는다
        // 바로 뒤에 맞으면 같은 자리에 데미지 숫자가 뜨므로, 상대 반대쪽 바깥으로 비켜서 띄운다
        if (battleEvent.lifesteal > 0)
        {
            attacker.PlayHeal(manager.Speed);
            var outward = Mathf.Sign(attacker.transform.position.x - target.transform.position.x);
            Vector3 position = attacker.HeadPosition + new Vector3(outward * LifestealOffset.x, LifestealOffset.y, 0f);
            damageFloaters.Show(position, $"+{battleEvent.lifesteal.ToString(GameConstants.NumberFormats.Decimal)}", GameConstants.Colors.Heal, 1f, false, manager.Speed);
        }
    }

    // 새 흔들림이 오면 이전 흔들림을 끝내고 제자리에서 다시 흔든다. 같은 카메라에 흔들림을 겹쳐 걸 수 없기 때문이다
    private void ShakeCamera(float strength, float duration)
    {
        trnsCamera.DOKill();
        trnsCamera.position = vecCameraPosition;
        Tween tween = trnsCamera.DOShakePosition(duration, new Vector3(strength, strength, 0f), ShakeVibrato, ShakeRandomness);
        tween.timeScale = manager.Speed;
    }

    // DOTween 전체 배율만 바꾼다. Time.timeScale은 옵션 팝업의 일시 정지가 쓰므로 건드리지 않는다
    // 되돌리는 대기도 함께 느려지므로, 그만큼 빨리 돌려서 게임 시간으로 DeathSlowMotionDuration 뒤에 되돌린다
    private void PlayDeathSlowMotion()
    {
        DOTween.timeScale = DeathSlowMotionScale;
        Tween tween = DOVirtual.DelayedCall(DeathSlowMotionDuration / manager.Speed, () => DOTween.timeScale = 1f, false).SetTarget(this);
        tween.timeScale = 1f / DeathSlowMotionScale;
    }

    // duration(1배속 기준) 동안 결과 팝업을 미룬다. 대기도 DOTween이라서 느린 화면 동안 함께 느려져 연출과 어긋나지 않는다
    private void WaitEndEffect(float duration)
    {
        isEndPlaying = true;
        DOVirtual.DelayedCall(duration / manager.Speed, OnEndEffectFinished, false).SetTarget(this);
    }

    private void OnEndEffectFinished()
    {
        isEndPlaying = false;
        TryOpenResult();
    }

    // 서버 결과가 연출보다 먼저 오면 연출이 끝날 때까지 보관한다. 포기처럼 연출이 없으면 바로 띄운다
    private void OnBattleEnded(BattleApi.BattleEndResponse result)
    {
        pendingResult = result;
        TryOpenResult();
    }

    private void TryOpenResult()
    {
        if (pendingResult == null || isEndPlaying)
            return;

        var artifactNames = pendingResult.rewards.artifactCodes.Select(code => artifactNamesByCode[code]).ToList();
        ResultPopup.Setup(pendingResult, artifactNames, onRetry, onExit);
        ResultPopup.Open();
        pendingResult = null;
    }

    // 재도전이 거절되면 열려 있는 팝업에 문구만 바꾸고, 전투 중 검증에 실패했으면 실패 팝업을 띄운다
    private void OnRequestRejected(string message)
    {
        ResultPopup.SetupRejected(manager.Session?.stage ?? 0, message, onRetry, onExit);
        ResultPopup.Open();
    }
}
