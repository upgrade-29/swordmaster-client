using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

// BattleManager가 알려주는 공격마다 공격/피격 모션과 데미지 숫자를 보여준다. 적 처치/다음 적 등장, 화면 흔들림, 보스 경고, 결과 팝업도 띄운다
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

    private readonly BattleManager manager;
    private readonly BattleUnitView playerView;
    private readonly BattleUnitView enemyView;
    private readonly BossWarningView bossWarningView;
    private readonly BattleBackgroundView backgroundView;
    private readonly Transform trnsCamera;
    private readonly Vector3 vecCameraPosition;
    private readonly Action onRetry;
    private readonly Action onExit;
    private readonly Dictionary<string, string> artifactNamesByCode;
    private readonly DamageFloaterPool damageFloaters = new DamageFloaterPool();

    // 처치 공격부터 적이 다 날아갈 때까지. 다음 적 등장은 이게 끝난 뒤에 시작한다
    private bool isDefeatPlaying;

    private BattleResultPopup ResultPopup => Popup.Instance.Get<BattleResultPopup>();

    // 결과 팝업의 재도전과 나가기에서 할 일은 여는 쪽(BattleViewManager)이 정해서 넘긴다
    public BattleEffectPresenter(BattleManager manager, BattleUnitView playerView, BattleUnitView enemyView,
        BossWarningView bossWarningView, BattleBackgroundView backgroundView, Transform trnsCamera, GameDB gameDB,
        Action onRetry, Action onExit)
    {
        this.manager = manager;
        this.playerView = playerView;
        this.enemyView = enemyView;
        this.bossWarningView = bossWarningView;
        this.backgroundView = backgroundView;
        this.trnsCamera = trnsCamera;
        vecCameraPosition = trnsCamera.position;
        this.onRetry = onRetry;
        this.onExit = onExit;
        artifactNamesByCode = gameDB.artifacts.ToDictionary(data => data.code, data => data.name);

        manager.CountdownStarted += OnCountdownStarted;
        manager.SubStageStarted += OnSubStageStarted;
        manager.BossWarningStarted += OnBossWarningStarted;
        manager.AttackApplied += OnAttackApplied;
        manager.BattleEnded += OnBattleEnded;
        manager.RequestRejected += OnRequestRejected;
    }

    public void Dispose()
    {
        manager.CountdownStarted -= OnCountdownStarted;
        manager.SubStageStarted -= OnSubStageStarted;
        manager.BossWarningStarted -= OnBossWarningStarted;
        manager.AttackApplied -= OnAttackApplied;
        manager.BattleEnded -= OnBattleEnded;
        manager.RequestRejected -= OnRequestRejected;
        DOTween.Kill(this);
        trnsCamera.DOKill();
        damageFloaters.Dispose();
    }

    /// <summary>
    /// 처치한 적이 다 날아가기를 기다렸다가, 배경을 흘리며 다음 적이 다가오게 한다. 다음 적이 제자리에 도착하면 끝난다.
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
        yield return new WaitForSeconds(EntranceDuration / speed);
    }

    // 재도전이 받아들여져 시작 대기에 들어가면 팝업을 닫고, 지난 전투에서 쓰러진 적을 제자리에 다시 세운다
    // 지난 전투에서 아직 나오지 않은 피격 반응은 버린다
    private void OnCountdownStarted(BattleSubStage subStage)
    {
        DOTween.Kill(this);
        isDefeatPlaying = false;
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

    private void OnBossWarningStarted(BattleSubStage subStage)
    {
        bossWarningView.Play(subStage.Enemy.name, BattleManager.BossWarningDuration);
    }

    private void OnAttackApplied(BattleSubStage subStage, BattleEvent battleEvent)
    {
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        BattleUnitView attacker = isPlayerAttack ? playerView : enemyView;
        BattleUnitView target = isPlayerAttack ? enemyView : playerView;

        attacker.PlayAttack(target.transform.position, manager.Speed);

        // 처치는 공격하는 순간 정해지므로, 검증 응답이 먼저 와도 다음 적 등장이 처치 연출을 기다리게 바로 표시한다
        bool isDefeat = isPlayerAttack && battleEvent.enemyHp <= 0;
        if (isDefeat)
        {
            isDefeatPlaying = true;
            float defeatEnd = BattleUnitView.AttackHitDelay + BattleUnitView.DefeatDuration + NextEnemyWaitDuration;
            DOVirtual.DelayedCall(defeatEnd / manager.Speed, () => isDefeatPlaying = false, false).SetTarget(this);
        }

        // 검이 닿는 순간에 맞는 쪽이 반응하도록 공격 모션만큼 늦춘다. 전투 계산과 로그는 이미 끝난 상태다
        DOVirtual.DelayedCall(BattleUnitView.AttackHitDelay / manager.Speed, () => PlayHitReaction(subStage, battleEvent, attacker, target, isDefeat), false)
            .SetTarget(this);
    }

    private void PlayHitReaction(BattleSubStage subStage, BattleEvent battleEvent, BattleUnitView attacker, BattleUnitView target, bool isDefeat)
    {
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        double targetMaxHp = isPlayerAttack ? subStage.EnemyStat.maxHp : manager.Session.playerStat.maxHp;
        if (isDefeat)
        {
            attacker.Freeze(BattleUnitView.DefeatHitstopDuration / manager.Speed);
            target.PlayDefeat(Mathf.Sign(target.transform.position.x - attacker.transform.position.x), manager.Speed);
            ShakeCamera(DefeatShakeStrength, DefeatShakeDuration);
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
        damageFloaters.Show(target.HeadPosition, battleEvent.damage.ToString(GameConstants.NumberFormats.Decimal), color, sizeScale, battleEvent.isCrit);

        // 흡혈은 공격자 머리 위에 회복량을 띄운다. 체력이 가득 차서 회복하지 못했으면 보여주지 않는다
        // 바로 뒤에 맞으면 같은 자리에 데미지 숫자가 뜨므로, 상대 반대쪽 바깥으로 비켜서 띄운다
        if (battleEvent.lifesteal > 0)
        {
            attacker.PlayHeal(manager.Speed);
            var outward = Mathf.Sign(attacker.transform.position.x - target.transform.position.x);
            Vector3 position = attacker.HeadPosition + new Vector3(outward * LifestealOffset.x, LifestealOffset.y, 0f);
            damageFloaters.Show(position, $"+{battleEvent.lifesteal.ToString(GameConstants.NumberFormats.Decimal)}", GameConstants.Colors.Heal, 1f, false);
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

    private void OnBattleEnded(BattleApi.BattleEndResponse result)
    {
        var artifactNames = result.rewards.artifactCodes.Select(code => artifactNamesByCode[code]).ToList();
        ResultPopup.Setup(result, artifactNames, onRetry, onExit);
        ResultPopup.Open();
    }

    // 재도전이 거절되면 열려 있는 팝업에 문구만 바꾸고, 전투 중 검증에 실패했으면 실패 팝업을 띄운다
    private void OnRequestRejected(string message)
    {
        ResultPopup.SetupRejected(manager.Session?.stage ?? 0, message, onRetry, onExit);
        ResultPopup.Open();
    }
}
