using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BattleManager가 알려주는 공격마다 공격/피격 모션과 데미지 숫자를 보여준다. 적 등장/퇴장, 보스 경고, 결과 팝업도 띄운다
// 진행 타이밍이나 HP는 모른다
public class BattleEffectPresenter : IDisposable
{
    private static readonly Vector2 LifestealOffset = new Vector2(0.6f, 0.3f); // x는 상대 반대쪽으로

    private readonly BattleManager manager;
    private readonly BattleUnitView playerView;
    private readonly BattleUnitView enemyView;
    private readonly BossWarningView bossWarningView;
    private readonly Action onRetry;
    private readonly Action onExit;
    private readonly Dictionary<string, string> artifactNamesByCode;
    private readonly DamageFloaterPool damageFloaters = new DamageFloaterPool();

    private BattleResultPopup ResultPopup => Popup.Instance.Get<BattleResultPopup>();

    // 결과 팝업의 재도전과 나가기에서 할 일은 여는 쪽(BattleViewManager)이 정해서 넘긴다
    public BattleEffectPresenter(BattleManager manager, BattleUnitView playerView, BattleUnitView enemyView,
        BossWarningView bossWarningView, GameDB gameDB, Action onRetry, Action onExit)
    {
        this.manager = manager;
        this.playerView = playerView;
        this.enemyView = enemyView;
        this.bossWarningView = bossWarningView;
        this.onRetry = onRetry;
        this.onExit = onExit;
        artifactNamesByCode = gameDB.artifacts.ToDictionary(data => data.code, data => data.name);

        manager.CountdownStarted += OnCountdownStarted;
        manager.SubStageStarted += OnSubStageStarted;
        manager.BossWarningStarted += OnBossWarningStarted;
        manager.AttackApplied += OnAttackApplied;
        manager.SubStageVerified += OnSubStageVerified;
        manager.BattleEnded += OnBattleEnded;
        manager.RequestRejected += OnRequestRejected;
    }

    public void Dispose()
    {
        manager.CountdownStarted -= OnCountdownStarted;
        manager.SubStageStarted -= OnSubStageStarted;
        manager.BossWarningStarted -= OnBossWarningStarted;
        manager.AttackApplied -= OnAttackApplied;
        manager.SubStageVerified -= OnSubStageVerified;
        manager.BattleEnded -= OnBattleEnded;
        manager.RequestRejected -= OnRequestRejected;
        damageFloaters.Dispose();
    }

    // 재도전이 받아들여져 시작 대기에 들어가면 팝업을 닫고, 지난 전투에서 쓰러진 적을 다시 보여준다
    private void OnCountdownStarted(BattleSubStage subStage)
    {
        ResultPopup.Close();
        enemyView.SetVisible(true);
    }

    private void OnSubStageStarted(BattleSubStage subStage)
    {
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

        attacker.PlayAttack(target.transform.position);
        target.PlayHit(battleEvent.isCrit);

        Color color = battleEvent.isCrit ? GameConstants.Colors.CritDamage : isPlayerAttack ? GameConstants.Colors.EnemyDamage : GameConstants.Colors.PlayerDamage;
        double targetMaxHp = isPlayerAttack ? subStage.EnemyStat.maxHp : manager.Session.playerStat.maxHp;
        float sizeScale = DamageFloaterPool.GetSizeScale(battleEvent.damage, targetMaxHp);
        damageFloaters.Show(target.HeadPosition, battleEvent.damage.ToString(GameConstants.NumberFormats.Decimal), color, sizeScale, battleEvent.isCrit);

        // 흡혈은 공격자 머리 위에 회복량을 띄운다. 체력이 가득 차서 회복하지 못했으면 보여주지 않는다
        // 바로 뒤에 맞으면 같은 자리에 데미지 숫자가 뜨므로, 상대 반대쪽 바깥으로 비켜서 띄운다
        if (battleEvent.lifesteal > 0)
        {
            attacker.PlayHeal();
            var outward = Mathf.Sign(attacker.transform.position.x - target.transform.position.x);
            Vector3 position = attacker.HeadPosition + new Vector3(outward * LifestealOffset.x, LifestealOffset.y, 0f);
            damageFloaters.Show(position, $"+{battleEvent.lifesteal.ToString(GameConstants.NumberFormats.Decimal)}", GameConstants.Colors.Heal, 1f, false);
        }
    }

    // 처치한 적은 서버 검증을 통과한 뒤 사라진다. 처치 골드 동전은 BattleHudPresenter가 띄운다
    private void OnSubStageVerified(BattleSubStage subStage, BattleApi.BattleResultVerifyResponse response)
    {
        if (response.resultEnum == BattleSubStageResultEnum.EnemyDead)
            enemyView.SetVisible(false);
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
