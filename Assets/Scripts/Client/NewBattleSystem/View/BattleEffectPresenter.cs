using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BattleManager가 알려주는 공격마다 공격/피격 모션과 데미지 숫자를 보여준다. 적 등장/퇴장, 보스 경고, 결과 팝업도 띄운다
// 진행 타이밍이나 HP는 모른다
public class BattleEffectPresenter : IDisposable
{
    private static readonly Color EnemyDamageColor = Color.white;
    private static readonly Color PlayerDamageColor = new Color(1f, 0.4f, 0.4f);
    private static readonly Color CritDamageColor = new Color(1f, 0.85f, 0.2f);

    private readonly BattleManager manager;
    private readonly BattleUnitView playerView;
    private readonly BattleUnitView enemyView;
    private readonly BossWarningView bossWarningView;
    private readonly BattleResultView resultView;
    private readonly Dictionary<string, string> artifactNamesByCode;
    private readonly DamageFloaterPool damageFloaters = new DamageFloaterPool();

    public BattleEffectPresenter(BattleManager manager, BattleUnitView playerView, BattleUnitView enemyView,
        BossWarningView bossWarningView, BattleResultView resultView, GameDB gameDB)
    {
        this.manager = manager;
        this.playerView = playerView;
        this.enemyView = enemyView;
        this.bossWarningView = bossWarningView;
        this.resultView = resultView;
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
        resultView.Hide();
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

        Color color = battleEvent.isCrit ? CritDamageColor : isPlayerAttack ? EnemyDamageColor : PlayerDamageColor;
        damageFloaters.Show(target.HeadPosition, battleEvent.damage.ToString("#,0.##"), color, battleEvent.isCrit);
    }

    // 처치한 적은 서버 검증을 통과한 뒤 사라진다
    private void OnSubStageVerified(BattleSubStage subStage, BattleResultVerifyResponse response)
    {
        if (response.resultEnum == BattleSubStageResultEnum.EnemyDead)
            enemyView.SetVisible(false);
    }

    private void OnBattleEnded(BattleEndResponse result)
    {
        var artifactNames = result.rewards.artifactCodes.Select(code => artifactNamesByCode[code]).ToList();
        resultView.Show(result, artifactNames);
    }

    // 재도전이 거절되면 열려 있는 팝업에 문구만 바꾸고, 전투 중 검증에 실패했으면 실패 팝업을 띄운다
    private void OnRequestRejected(string message)
    {
        resultView.ShowRejected(manager.Session?.stage ?? 0, message);
    }
}
