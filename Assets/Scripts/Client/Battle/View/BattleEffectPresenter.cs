using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BattleDirector가 알려주는 공격마다 공격/피격 모션과 데미지 숫자를 보여준다. 재생 타이밍이나 HP는 모른다
public class BattleEffectPresenter : IDisposable
{
    private static readonly Color EnemyDamageColor = Color.white;
    private static readonly Color PlayerDamageColor = new Color(1f, 0.4f, 0.4f);
    private static readonly Color CritDamageColor = new Color(1f, 0.85f, 0.2f);

    private readonly BattleDirector director;
    private readonly BattleUnitView playerView;
    private readonly BattleUnitView enemyView;
    private readonly BossWarningView bossWarningView;
    private readonly BattleResultView resultView;
    private readonly Dictionary<string, string> enemyNamesByCode;
    private readonly Dictionary<string, string> artifactNamesByCode;
    private readonly DamageFloaterPool damageFloaters = new DamageFloaterPool();

    public BattleEffectPresenter(BattleDirector director, BattleUnitView playerView, BattleUnitView enemyView,
        BossWarningView bossWarningView, BattleResultView resultView, GameDB gameDB)
    {
        this.director = director;
        this.playerView = playerView;
        this.enemyView = enemyView;
        this.bossWarningView = bossWarningView;
        this.resultView = resultView;
        enemyNamesByCode = gameDB.enemies.ToDictionary(data => data.enemyCode, data => data.name);
        artifactNamesByCode = gameDB.artifacts.ToDictionary(data => data.artifactCode, data => data.name);

        director.AttackApplied += OnAttackApplied;
        director.BattleEnded += OnBattleEnded;
        director.CountdownStarted += OnCountdownStarted;
        director.RequestRejected += OnRequestRejected;
        director.BossWarningStarted += OnBossWarningStarted;
    }

    public void Dispose()
    {
        director.AttackApplied -= OnAttackApplied;
        director.BattleEnded -= OnBattleEnded;
        director.CountdownStarted -= OnCountdownStarted;
        director.RequestRejected -= OnRequestRejected;
        director.BossWarningStarted -= OnBossWarningStarted;
        damageFloaters.Dispose();
    }

    // 재도전 요청이 받아들여져 시작 대기에 들어가면 팝업을 닫는다. 거절되면 팝업에 사유를 보여준다
    private void OnCountdownStarted()
    {
        resultView.Hide();
    }

    private void OnRequestRejected(string reason)
    {
        resultView.ShowMessage(reason);
    }

    private void OnBattleEnded(BattleResult result)
    {
        var artifactNames = result.rewards.artifactCodes.Select(code => artifactNamesByCode[code]).ToList();
        // 서버가 정한 쿨타임은 응답을 받은 시점부터 흐르므로, 재생하는 동안 지난 시간은 뺀다
        double cooldown = (result.stageProgress.NextBattleAvailableAt - result.serverTime).TotalSeconds - director.SecondsSinceResult;
        resultView.Show(result, artifactNames, Math.Max(0, cooldown));
    }

    private void OnBossWarningStarted(BattleWave wave)
    {
        bossWarningView.Play(enemyNamesByCode[wave.enemyCode], BattleDirector.BossWarningDuration);
    }

    private void OnAttackApplied(BattleWave wave, BattleEvent battleEvent)
    {
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        BattleUnitView attacker = isPlayerAttack ? playerView : enemyView;
        BattleUnitView target = isPlayerAttack ? enemyView : playerView;

        attacker.PlayAttack(target.transform.position);
        target.PlayHit(battleEvent.isCrit);

        Color color = battleEvent.isCrit ? CritDamageColor : isPlayerAttack ? EnemyDamageColor : PlayerDamageColor;
        damageFloaters.Show(target.HeadPosition, battleEvent.damage.ToString("#,0.##"), color, battleEvent.isCrit);
    }
}
