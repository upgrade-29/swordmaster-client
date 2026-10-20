using DG.Tweening;
using UnityEngine;

// 전투에 나오는 플레이어/적 한 명의 겉모습. 계산은 하지 않고 보여주기만 한다
public class BattleUnitView : MonoBehaviour
{
    // 공격: 뒤로 젖혔다가 상대 쪽으로 베면서 나아가고, 끝 자세에서 잠깐 멈춘 뒤 제자리로 돌아온다
    private const float WindupAngle = 50f;
    private const float WindupDistance = 0.05f; // 뒤로 물러나는 거리
    private const float WindupDuration = 0.12f;
    private const float SwingAngle = 75f;
    private const float SwingDistance = 0.75f; // 앞으로 나가는 거리
    private const float SwingDuration = 0.1f;
    private const float SwingHoldDuration = 0.04f;
    private const float RecoverDuration = 0.2f;
    private const float AttackDuration = WindupDuration + SwingDuration + SwingHoldDuration + RecoverDuration;
    private const float AttackPivotHeightRatio = 0.38f; // 회전 기준점(가드). 스프라이트 아래 끝에서 높이의 이 비율만큼 올라간 자리. 검 그림 47장의 가드 위치 중앙값

    /// <summary>
    /// 공격을 시작해서 검이 상대에게 닿을 때까지 걸리는 시간(1배속 기준). 맞는 쪽의 반응은 이만큼 늦게 시작한다.
    /// </summary>
    public const float AttackHitDelay = WindupDuration + SwingDuration;

    private const float HitDuration = 0.2f;
    private const float HitScale = 0.1f; // 원래 크기에 대한 비율
    private const float CritHitScale = 0.25f;
    private const float HealDuration = 0.3f;

    // 처치: 하얗게 번쩍이고 히트스톱만큼 멈춘 뒤, 회전하며 포물선으로 튕겨 날아가면서 흐려진다
    private const float DefeatFlashDuration = 0.12f;
    private const float DefeatFlyAngle = 40f; // 수평에서 위로 기울어진 각도
    private const float DefeatFlySpeed = 10f; // 처음 속도(초당 월드 단위)
    private const float DefeatGravity = 10f;
    private const float DefeatSpinSpeed = 720f; // 초당 회전 각도
    private const float DefeatFlyDuration = 0.35f;
    private const float DefeatFadeStartRatio = 0.6f; // 날아가는 시간의 이 지점부터 흐려지기 시작한다

    /// <summary>
    /// 처치 공격이 닿은 순간 공격자와 적이 함께 멈춰 있는 시간(1배속 기준).
    /// </summary>
    public const float DefeatHitstopDuration = 0.08f;

    /// <summary>
    /// 처치 공격이 닿은 순간부터 적이 다 날아갈 때까지 걸리는 시간(1배속 기준).
    /// </summary>
    public const float DefeatDuration = DefeatHitstopDuration + DefeatFlyDuration;

    // 걷기: 다음 적을 만나러 가는 동안 위아래로 통통 튄다
    private const float WalkHeight = 0.2f;
    private const float WalkStepDuration = 0.3f;

    // 그림자: 검 그림 아래 끝 높이의 바닥에 붙어 있고, 검이 높이 뜰수록 작고 옅어진다
    private const float ShadowFadeHeight = 1.5f; // 검이 제자리보다 이만큼 높이 뜨면 그림자가 다 사라진다

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount"); // SwordMaster/SpriteFlash 셰이더의 흰색으로 덮는 정도

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer shadowRenderer;
    private Color shadowBaseColor;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 vecBasePosition; // 공격 연출이 끝나면 돌아올 제자리
    private Vector3 vecBaseScale;
    private Color baseColor;

    // 공격은 위치, 피격은 크기, 피격/흡혈은 색만 움직여서 연출이 겹쳐도 서로 값을 덮어쓰지 않게 한다
    // 색은 피격과 흡혈이 함께 쓰므로 새 연출을 시작할 때 이전 연출을 끝내 원래 색으로 돌려놓는다
    private Tween attackTween;
    private Tween hitScaleTween;
    private Tween colorTween;

    // 데미지 숫자를 띄울 머리 위 위치
    public Vector3 HeadPosition => new Vector3(transform.position.x, spriteRenderer.bounds.max.y, 0f);

    // 처치 골드 동전이 튀어나올 몸 가운데 위치
    // 처치된 적은 날아가고 있으므로 지금 위치가 아니라 맞은 제자리를 기준으로 구한다
    public Vector3 CenterPosition => vecBasePosition + Vector3.Scale(spriteRenderer.sprite.bounds.center, vecBaseScale);

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref spriteRenderer);
        vecBasePosition = transform.position;
        vecBaseScale = transform.localScale;
        baseColor = spriteRenderer.color;
        propertyBlock = new MaterialPropertyBlock();
        shadowRenderer = GameUtil.Bind<SpriteRenderer>(transform, "Shadow");
        shadowBaseColor = shadowRenderer.color;
    }

    // 검은 공격, 걷기, 처치 연출로 돌거나 뜨지만 그림자는 바닥에 남아야 하므로, 연출이 끝난 뒤의 위치를 보고 매 프레임 맞춘다
    private void LateUpdate()
    {
        UpdateShadow();
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

    // 미리 읽은 검 이미지로 바꾼다. 이미지가 없으면 씬에 놓인 자리 표시 이미지를 그대로 둔다
    public void SetSprite(string imageCode)
    {
        Sprite sprite = ResourceManager.Instance.GetSprite(imageCode);
        if (sprite != null)
            spriteRenderer.sprite = sprite;
    }

    // 상대 쪽으로 검을 휘두른다. speed는 전투 배속이고, 배속만큼 빨리 끝난다
    public void PlayAttack(Vector3 targetPosition, float speed)
    {
        // 공격 간격이 연출보다 짧으면 이전 연출을 끝낸 자리에서 다시 시작해야 위치가 밀리지 않는다
        CompleteIfActive(attackTween);

        float facing = Mathf.Sign(targetPosition.x - vecBasePosition.x);
        attackTween = DOVirtual.Float(0f, AttackDuration, AttackDuration, time => ApplyAttackPose(time, facing))
            .SetEase(Ease.Linear)
            .SetTarget(transform);
        attackTween.timeScale = speed;
    }

    // 잠깐 커졌다 돌아오고 붉게 번쩍인다. 치명타는 더 크게 튄다
    public void PlayHit(bool isCrit, float speed)
    {
        CompleteIfActive(hitScaleTween);
        CompleteIfActive(colorTween);

        var scale = isCrit ? CritHitScale : HitScale;
        hitScaleTween = transform.DOPunchScale(transform.localScale * scale, HitDuration, 1, 0f);
        hitScaleTween.timeScale = speed;
        colorTween = spriteRenderer.DOColor(GameConstants.Colors.UnitHit, HitDuration * 0.5f).SetLoops(2, LoopType.Yoyo);
        colorTween.timeScale = speed;
    }

    // 흡혈로 체력을 회복하면 초록색으로 번쩍인다
    public void PlayHeal(float speed)
    {
        CompleteIfActive(colorTween);
        colorTween = spriteRenderer.DOColor(GameConstants.Colors.Heal, HealDuration * 0.5f).SetLoops(2, LoopType.Yoyo);
        colorTween.timeScale = speed;
    }

    // 처치 공격이 닿은 순간 공격 모션을 잠깐 멈춘다. duration은 배속을 이미 반영한 시간이다
    public void Freeze(float duration)
    {
        if (attackTween == null || attackTween.IsActive() == false)
            return;

        Tween frozenTween = attackTween;
        frozenTween.Pause();
        DOVirtual.DelayedCall(duration, () => frozenTween.Play(), false).SetTarget(transform);
    }

    // 하얗게 번쩍인 뒤 히트스톱만큼 멈췄다가 direction 쪽(1이면 오른쪽)으로 튕겨 날아간다
    public void PlayDefeat(float direction, float speed)
    {
        transform.DOKill();
        spriteRenderer.DOKill();
        transform.localScale = vecBaseScale;
        spriteRenderer.color = baseColor;

        // 스프라이트 색은 그림에 곱해지므로 흰색으로 바꿔도 하얘지지 않는다. 머티리얼에서 흰색으로 덮었다가 걷어 낸다
        SetFlashAmount(1f);
        Tween flashTween = DOVirtual.Float(1f, 0f, DefeatFlashDuration, SetFlashAmount)
            .SetEase(Ease.Linear)
            .SetTarget(transform);
        flashTween.timeScale = speed;

        Tween flyTween = DOVirtual.Float(0f, DefeatFlyDuration, DefeatFlyDuration, time => ApplyDefeatPose(time, direction))
            .SetDelay(DefeatHitstopDuration)
            .SetEase(Ease.Linear)
            .SetTarget(transform);
        flyTween.timeScale = speed;
    }

    // 오른쪽으로 distance만큼 떨어진 곳에서 제자리까지 다가온다. 배경 스크롤과 같은 곡선을 넘겨받아 땅에 붙어 오는 것처럼 보인다
    public void PlayEntrance(float distance, float duration, Ease ease, float speed)
    {
        ResetPose();
        transform.position = vecBasePosition + Vector3.right * distance;

        Tween tween = transform.DOMoveX(vecBasePosition.x, duration).SetEase(ease);
        tween.timeScale = speed;
    }

    // 제자리에서 위아래로 통통 튀며 걷는다
    public void PlayWalk(float duration, float speed)
    {
        Tween tween = DOVirtual.Float(0f, duration, duration, time =>
            {
                float height = WalkHeight * Mathf.Abs(Mathf.Sin(Mathf.PI * time / WalkStepDuration));
                transform.position = vecBasePosition + Vector3.up * height;
            })
            .SetEase(Ease.Linear)
            .SetTarget(transform)
            .OnComplete(() => transform.position = vecBasePosition);
        tween.timeScale = speed;
    }

    // 재도전처럼 연출 도중에 처음 상태로 돌아가야 할 때 쓴다
    public void ResetPose()
    {
        transform.DOKill();
        spriteRenderer.DOKill();
        transform.SetPositionAndRotation(vecBasePosition, Quaternion.identity);
        transform.localScale = vecBaseScale;
        spriteRenderer.color = baseColor;
        spriteRenderer.enabled = true;
        SetFlashAmount(0f);
    }

    // 가로 위치만 검을 따라가고, 높이는 제자리 검 그림의 아래 끝에 고정한다. 검의 회전과 피격 크기 변화는 따라가지 않는다
    private void UpdateShadow()
    {
        float groundY = vecBasePosition.y + spriteRenderer.sprite.bounds.min.y * vecBaseScale.y;
        float height = Mathf.Max(0f, transform.position.y - vecBasePosition.y);
        float ratio = 1f - Mathf.Clamp01(height / ShadowFadeHeight);

        Transform shadowTransform = shadowRenderer.transform;
        shadowTransform.SetPositionAndRotation(new Vector3(transform.position.x, groundY, 0f), Quaternion.identity);
        shadowTransform.localScale = new Vector3(
            vecBaseScale.x / transform.localScale.x * ratio,
            vecBaseScale.y / transform.localScale.y * ratio,
            1f);

        shadowRenderer.enabled = spriteRenderer.enabled;
        Color color = shadowBaseColor;
        color.a *= ratio * spriteRenderer.color.a;
        shadowRenderer.color = color;
    }

    private void SetFlashAmount(float amount)
    {
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FlashAmountId, amount);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    // 회전은 스프라이트 가운데가 아니라 가드를 중심으로 하므로, 돌린 만큼 위치도 가드 둘레로 옮긴다
    // facing이 양수면 오른쪽을 보고 있어 시계 방향(z 각도 음수)으로 벤다
    private void ApplyAttackPose(float time, float facing)
    {
        GetAttackPose(time, out float angle, out float distance);

        float spriteHeight = spriteRenderer.sprite.bounds.size.y * transform.localScale.y;
        var pivot = vecBasePosition + Vector3.up * (spriteHeight * (AttackPivotHeightRatio - 0.5f));
        Quaternion rotation = Quaternion.Euler(0f, 0f, -facing * angle);
        Vector3 position = pivot + rotation * (vecBasePosition - pivot) + Vector3.right * (facing * distance);
        transform.SetPositionAndRotation(position, rotation);
    }

    // time초 시점의 각도와 상대 쪽으로 나간 거리. 젖힐 때는 둘 다 음수다
    private static void GetAttackPose(float time, out float angle, out float distance)
    {
        if (time < WindupDuration)
        {
            float windup = DOVirtual.EasedValue(0f, 1f, time / WindupDuration, Ease.InOutQuart);
            angle = -WindupAngle * windup;
            distance = -WindupDistance * windup;
            return;
        }

        time -= WindupDuration;
        if (time < SwingDuration)
        {
            float swing = DOVirtual.EasedValue(0f, 1f, time / SwingDuration, Ease.OutQuart);
            angle = Mathf.LerpUnclamped(-WindupAngle, SwingAngle, swing);
            distance = Mathf.LerpUnclamped(-WindupDistance, SwingDistance, swing);
            return;
        }

        time -= SwingDuration + SwingHoldDuration;
        float recover = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01(time / RecoverDuration), Ease.OutQuart);
        angle = SwingAngle * (1f - recover);
        distance = SwingDistance * (1f - recover);
    }

    // 날아가는 방향으로 시계 방향 회전한다. 끝 무렵에는 흐려져서 사라진다
    private void ApplyDefeatPose(float time, float direction)
    {
        float angle = DefeatFlyAngle * Mathf.Deg2Rad;
        float x = direction * DefeatFlySpeed * Mathf.Cos(angle) * time;
        float y = DefeatFlySpeed * Mathf.Sin(angle) * time - 0.5f * DefeatGravity * time * time;
        transform.SetPositionAndRotation(vecBasePosition + new Vector3(x, y, 0f), Quaternion.Euler(0f, 0f, -direction * DefeatSpinSpeed * time));

        float fadeProgress = Mathf.InverseLerp(DefeatFadeStartRatio, 1f, time / DefeatFlyDuration);
        Color color = spriteRenderer.color;
        color.a = 1f - fadeProgress;
        spriteRenderer.color = color;
    }

    private static void CompleteIfActive(Tween tween)
    {
        if (tween != null && tween.IsActive())
            tween.Complete();
    }
}
