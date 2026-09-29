using System;
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
    private readonly DamageFloaterPool damageFloaters = new DamageFloaterPool();

    public BattleEffectPresenter(BattleDirector director, BattleUnitView playerView, BattleUnitView enemyView)
    {
        this.director = director;
        this.playerView = playerView;
        this.enemyView = enemyView;

        director.AttackApplied += OnAttackApplied;
    }

    public void Dispose()
    {
        director.AttackApplied -= OnAttackApplied;
        damageFloaters.Dispose();
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
