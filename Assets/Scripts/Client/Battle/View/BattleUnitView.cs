using DG.Tweening;
using UnityEngine;

// 전투에 나오는 플레이어/적 한 명의 겉모습. 계산은 하지 않고 보여주기만 한다
public class BattleUnitView : MonoBehaviour
{
    private const float AttackDistance = 0.3f;
    private const float AttackDuration = 0.2f;
    private const float HitDuration = 0.2f;
    private const float HitScale = 0.1f; // 원래 크기에 대한 비율
    private const float CritHitScale = 0.25f;
    private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f);

    private SpriteRenderer spriteRenderer;

    // 공격은 위치, 피격은 크기/색만 움직여서 두 연출이 겹쳐도 서로 값을 덮어쓰지 않게 한다
    private Tween attackTween;
    private Tween hitScaleTween;
    private Tween hitColorTween;

    // 데미지 숫자를 띄울 머리 위 위치
    public Vector3 HeadPosition => new Vector3(transform.position.x, spriteRenderer.bounds.max.y, 0f);

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref spriteRenderer);
    }

    private void OnDestroy()
    {
        transform.DOKill();
        spriteRenderer.DOKill();
    }

    public void SetVisible(bool visible)
    {
        spriteRenderer.enabled = visible;
    }

    // 상대 쪽으로 살짝 튀어나갔다가 제자리로 돌아온다
    public void PlayAttack(Vector3 targetPosition)
    {
        // 공격 간격이 연출보다 짧으면 이전 연출을 끝낸 자리에서 다시 시작해야 위치가 밀리지 않는다
        CompleteIfActive(attackTween);

        Vector3 direction = (targetPosition - transform.position).normalized;
        attackTween = transform.DOPunchPosition(direction * AttackDistance, AttackDuration, 1, 0f);
    }

    // 잠깐 커졌다 돌아오고 붉게 번쩍인다. 치명타는 더 크게 튄다
    public void PlayHit(bool isCrit)
    {
        CompleteIfActive(hitScaleTween);
        CompleteIfActive(hitColorTween);

        var scale = isCrit ? CritHitScale : HitScale;
        hitScaleTween = transform.DOPunchScale(transform.localScale * scale, HitDuration, 1, 0f);
        hitColorTween = spriteRenderer.DOColor(HitColor, HitDuration * 0.5f).SetLoops(2, LoopType.Yoyo);
    }

    private static void CompleteIfActive(Tween tween)
    {
        if (tween != null && tween.IsActive())
            tween.Complete();
    }
}
