using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 공격이 닿은 자리에 타격, 치명타, 처치 이펙트를, 처치 회복 때 플레이어 검에 회복 이펙트를 프레임 애니메이션으로 재생한다
// 프레임 넘김과 튀어나오는 크기 변화는 DOTween으로 돌려서 전투 배속과 처치 느린 화면을 그대로 따른다. 다 쓴 렌더러는 다시 쓴다
public class BattleVfxView : MonoBehaviour
{
    private const int FrontSortingOrder = 50; // 검(0)보다 앞, 데미지 숫자(100)보다 뒤
    private const int BackSortingOrder = -1; // 검 바로 뒤

    private static readonly Vector2 HitSpawnJitter = new Vector2(0.12f, 0.35f); // 검 가운데에서 이 범위 안으로 흩뜨린다. 칼날 방향(세로)으로 더 넓다

    [SerializeField] private List<Sprite> listHitFrame = new List<Sprite>();
    [SerializeField] private List<Sprite> listCritFrame = new List<Sprite>();
    [SerializeField] private List<Sprite> listKnockoutFrame = new List<Sprite>();
    [SerializeField] private List<Sprite> listHealBackFrame = new List<Sprite>();
    [SerializeField] private List<Sprite> listHealFrontFrame = new List<Sprite>();

    private readonly List<SpriteRenderer> listRenderer = new List<SpriteRenderer>();
    private readonly Stack<SpriteRenderer> idleRenderers = new Stack<SpriteRenderer>();

    private Clip hitClip;
    private Clip critClip;
    private Clip knockoutClip;
    private Clip healBackClip;
    private Clip healFrontClip;

    // 타격감: 처음에 popScale배로 나타나 popDuration 동안 원래 크기로 줄고, holdFrame에서 holdDuration만큼 멈춘다(1배속 기준)
    // 재생 시간의 fadeStartRatio 지점부터 투명해지며 endScale배까지 줄어든다. spawnJitter 안의 무작위 자리에서 터진다
    private class Clip
    {
        public List<Sprite> listFrame;
        public float fps;
        public int sortingOrder;
        public float popScale = 1f;
        public float popDuration;
        public Ease popEase = Ease.OutQuad;
        public int holdFrame = -1;
        public float holdDuration;
        public bool useRandomFlip;
        public float fadeStartRatio = 0.6f;
        public float endScale = 1f;
        public Vector2 spawnJitter;

        public float FrameDuration => 1f / fps;
        public float Duration => listFrame.Count * FrameDuration + holdDuration;
    }

    private void Awake()
    {
        hitClip = new Clip { listFrame = listHitFrame, fps = 16f, sortingOrder = FrontSortingOrder, popScale = 1.4f, popDuration = 0.08f, holdFrame = 1, holdDuration = 0.05f, useRandomFlip = true, endScale = 0.85f, spawnJitter = HitSpawnJitter };
        critClip = new Clip { listFrame = listCritFrame, fps = 16f, sortingOrder = FrontSortingOrder, popScale = 1.35f, popDuration = 0.12f, popEase = Ease.OutBack, holdFrame = 2, holdDuration = 0.08f, useRandomFlip = true, endScale = 0.85f, spawnJitter = HitSpawnJitter };
        knockoutClip = new Clip { listFrame = listKnockoutFrame, fps = 16f, sortingOrder = FrontSortingOrder, popScale = 1.5f, popDuration = 0.12f, popEase = Ease.OutBack, holdFrame = 0, holdDuration = 0.06f, useRandomFlip = true, endScale = 0.85f };
        healBackClip = new Clip { listFrame = listHealBackFrame, fps = 14f, sortingOrder = BackSortingOrder };
        healFrontClip = new Clip { listFrame = listHealFrontFrame, fps = 14f, sortingOrder = FrontSortingOrder };
    }

    private void OnDestroy()
    {
        StopAll();
    }

    /// <summary>
    /// 맞은 검의 가운데(position)에 타격 이펙트를 재생한다. 치명타면 치명타 이펙트를 쓴다. speed는 전투 배속이다.
    /// </summary>
    public void PlayHit(Vector3 position, bool isCrit, float speed)
    {
        Play(isCrit ? critClip : hitClip, position, speed);
    }

    /// <summary>
    /// 쓰러지는 검의 가운데(position)에 처치 이펙트를 재생한다. 검이 날아가도 이펙트는 맞은 자리에 남는다.
    /// </summary>
    public void PlayKnockout(Vector3 position, float speed)
    {
        Play(knockoutClip, position, speed);
    }

    /// <summary>
    /// 회복하는 검의 가운데(position)에 회복 이펙트를 재생한다. 빛은 검 뒤에, 십자는 검 앞에 그린다.
    /// </summary>
    public void PlayHeal(Vector3 position, float speed)
    {
        Play(healBackClip, position, speed);
        Play(healFrontClip, position, speed);
    }

    /// <summary>
    /// 재생 중인 이펙트를 모두 끈다. 재도전으로 전투를 다시 시작할 때 부른다.
    /// </summary>
    public void StopAll()
    {
        foreach (SpriteRenderer renderer in listRenderer)
        {
            if (renderer == null || renderer.gameObject.activeSelf == false)
            {
                continue;
            }

            renderer.transform.DOKill();
            Release(renderer);
        }
    }

    private void Play(Clip clip, Vector3 position, float speed)
    {
        if (clip.listFrame.Count == 0)
        {
            return;
        }

        SpriteRenderer renderer = idleRenderers.Count > 0 ? idleRenderers.Pop() : Create();
        renderer.sortingOrder = clip.sortingOrder;
        renderer.flipX = clip.useRandomFlip && Random.value < 0.5f;
        var jitter = new Vector3(Random.Range(-clip.spawnJitter.x, clip.spawnJitter.x), Random.Range(-clip.spawnJitter.y, clip.spawnJitter.y), 0f);
        renderer.transform.position = position + jitter;
        Apply(renderer, clip, 0f);
        renderer.gameObject.SetActive(true);

        Tween tween = DOVirtual.Float(0f, clip.Duration, clip.Duration, time => Apply(renderer, clip, time))
            .SetEase(Ease.Linear)
            .SetTarget(renderer.transform)
            .OnComplete(() => Release(renderer));
        tween.timeScale = speed;
    }

    // time초 시점의 프레임, 크기, 투명도를 한 번에 정한다. 튀어나오는 크기와 사라지며 줄어드는 크기는 곱한다
    private static void Apply(SpriteRenderer renderer, Clip clip, float time)
    {
        renderer.sprite = clip.listFrame[GetFrameIndex(clip, time)];

        float popProgress = clip.popDuration > 0f ? Mathf.Clamp01(time / clip.popDuration) : 1f;
        float popScale = DOVirtual.EasedValue(clip.popScale, 1f, popProgress, clip.popEase);

        float fadeProgress = Mathf.Clamp01((time / clip.Duration - clip.fadeStartRatio) / (1f - clip.fadeStartRatio));
        float fade = DOVirtual.EasedValue(0f, 1f, fadeProgress, Ease.InQuad);

        renderer.transform.localScale = Vector3.one * (popScale * Mathf.Lerp(1f, clip.endScale, fade));
        renderer.color = new Color(1f, 1f, 1f, 1f - fade);
    }

    // holdFrame까지는 그대로 넘기고, holdFrame에서 holdDuration만큼 더 머문 뒤 나머지를 넘긴다
    private static int GetFrameIndex(Clip clip, float time)
    {
        float holdStart = clip.holdFrame * clip.FrameDuration;
        float holdEnd = holdStart + clip.FrameDuration + clip.holdDuration;
        int index;
        if (clip.holdFrame < 0 || time < holdStart)
        {
            index = Mathf.FloorToInt(time / clip.FrameDuration);
        }
        else if (time < holdEnd)
        {
            index = clip.holdFrame;
        }
        else
        {
            index = Mathf.FloorToInt((time - clip.holdDuration) / clip.FrameDuration);
        }

        return Mathf.Clamp(index, 0, clip.listFrame.Count - 1);
    }

    private SpriteRenderer Create()
    {
        var renderer = new GameObject("Vfx").AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(transform, false);
        listRenderer.Add(renderer);
        return renderer;
    }

    private void Release(SpriteRenderer renderer)
    {
        renderer.gameObject.SetActive(false);
        idleRenderers.Push(renderer);
    }
}
