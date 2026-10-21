using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// 출발점 주변에 재화를 흩뿌리고, 잠깐 뒤 재화 HUD로 빨아들인다. 재화가 HUD에 닿으면 그 자리에서 반짝임이 터진다
// 배틀에서는 처치한 적 자리에서 골드가, 로비 상점에서는 구매한 상품 카드에서 골드나 다이아가 나온다
// 게임 표시 영역의 Screen Space Overlay 캔버스의 CoinLayer에 붙인다. 자식 CoinTemplate과 PopTemplate을 복제해서 쓰고, 다 쓴 것은 다시 쓴다
// 배틀은 동전이 HUD를 가리지 않게 CoinLayer를 Hud 뒤에 두고, 반짝임은 골드 아이콘 위에 보이게 Hud 앞의 CoinPopLayer/PopTemplate을 쓴다
public class CoinFlyView : MonoBehaviour
{
    private const string PopLayerName = "CoinPopLayer"; // CoinLayer와 같은 부모 밑에 이 레이어가 있으면 반짝임을 여기에 그린다
    private const float SpawnInterval = 0.02f; // 한꺼번에 터지지 않고 하나씩 쏟아진다
    private const float ScatterDuration = 0.5f; // 떨어진 뒤 튕기기까지 합친 시간
    private const Ease ScatterEase = Ease.OutQuad;
    private const int BounceCount = 2; // 땅에 떨어진 뒤 튕기는 횟수
    private const float BounceHeightRatio = 0.25f; // 튕길 때마다 이전 높이의 이 비율로 줄어든다
    private const float ScatterMinDistance = 40f; // 좌우로 튀는 거리
    private const float ScatterMaxDistance = 140f;
    private const float JumpMinPower = 60f; // 떨어지기 전에 위로 솟는 높이
    private const float JumpMaxPower = 120f;
    private const float DropMinDistance = 100f; // 출발점(적 몸 가운데)보다 아래, 땅 위에 떨어진다
    private const float DropMaxDistance = 160f;
    private const float HoldDuration = 0.25f; // 바닥에 떨어진 채로 잠깐 보여준다
    private const float FlyStagger = 0.08f; // 한꺼번에 날아가지 않고 하나씩 따라간다
    private const float FlyDuration = 0.45f;
    private const float ArriveScale = 0.6f;
    private const float PopFrameRate = 24f; // 반짝임 스프라이트 시트를 1초에 몇 장 넘기는지

    // 동전이 HUD에 닿을 때 재생할 기본 반짝임 프레임. 스프라이트 시트(gold_effect)를 자른 순서대로 넣는다
    // 다른 재화는 Play에 그 재화의 아이콘과 반짝임 프레임(diamond_effect 등)을 따로 넘긴다
    [SerializeField] private Sprite[] popFrames;

    private RectTransform layer;
    private Image coinTemplate;
    private Image popTemplate;
    private Camera worldCamera;
    private readonly Stack<Image> idleCoins = new Stack<Image>();
    private readonly Stack<Image> idlePops = new Stack<Image>();

    private void Awake()
    {
        layer = (RectTransform)transform;
        coinTemplate = GameUtil.Bind<Image>(gameObject, "CoinTemplate");
        Transform popLayer = transform.parent.Find(PopLayerName);
        popTemplate = GameUtil.Bind<Image>(popLayer != null ? popLayer : transform, "PopTemplate");
        coinTemplate.gameObject.SetActive(false);
        popTemplate.gameObject.SetActive(false);
        worldCamera = Camera.main;
    }

    private void OnDestroy()
    {
        foreach (Transform child in transform)
            child.DOKill();
        foreach (Transform child in popTemplate.transform.parent)
            child.DOKill();
    }

    // worldPosition은 적이 서 있던 월드 좌표, targetPosition은 동전이 빨려 들어갈 HUD 자리(캔버스 UI의 월드 좌표)
    // onCoinArrived는 동전 하나가 HUD에 닿을 때마다 불린다. speed는 전투 배속이고, 배속만큼 빨리 끝난다
    // 동전 그림과 반짝임은 템플릿에 넣어 둔 골드를 쓴다
    public void Play(Vector3 worldPosition, Vector3 targetPosition, int count, float speed, Action onCoinArrived)
    {
        Vector2 start = ToLocal(worldCamera.WorldToScreenPoint(worldPosition));
        Play(start, targetPosition, count, speed, coinTemplate.sprite, popFrames, onCoinArrived);
    }

    // uiPosition은 재화가 쏟아질 자리(캔버스 UI의 월드 좌표)다. 상품 카드처럼 UI에서 출발할 때 쓴다
    // coinSprite와 frames로 날아가는 재화 그림과 도착 반짝임을 정한다. frames가 비어 있으면 반짝임 없이 사라진다
    public void PlayFromUI(Vector3 uiPosition, Vector3 targetPosition, int count, Sprite coinSprite, Sprite[] frames,
        Action onCoinArrived)
    {
        Vector2 start = ToLocal(RectTransformUtility.WorldToScreenPoint(null, uiPosition));
        Play(start, targetPosition, count, 1f, coinSprite, frames, onCoinArrived);
    }

    private void Play(Vector2 start, Vector3 targetPosition, int count, float speed, Sprite coinSprite, Sprite[] frames,
        Action onCoinArrived)
    {
        Vector2 end = ToLocal(RectTransformUtility.WorldToScreenPoint(null, targetPosition));

        for (var i = 0; i < count; i++)
        {
            Image coin = Get(idleCoins, coinTemplate, "Coin");
            coin.sprite = coinSprite;
            RectTransform rect = coin.rectTransform;
            rect.localPosition = start;
            rect.localScale = Vector3.one;

            var side = Random.value < 0.5f ? -1f : 1f;
            var landing = new Vector2(
                start.x + side * Random.Range(ScatterMinDistance, ScatterMaxDistance),
                start.y - Random.Range(DropMinDistance, DropMaxDistance));
            float jumpPower = Random.Range(JumpMinPower, JumpMaxPower);

            // DOLocalJump는 안에서 Sequence를 하나 더 만들어 동전마다 Sequence가 2개가 된다. 동전이 많으면 DOTween 용량을 넘으므로
            // 튕기는 위치를 직접 계산해 트윈 하나로 움직인다
            Tween scatter = DOVirtual.Float(0f, 1f, ScatterDuration, progress => rect.localPosition = GetScatterPosition(start, landing, jumpPower, progress))
                .SetEase(ScatterEase);

            // 차례가 올 때까지 숨겨 둔다
            coin.enabled = false;
            Sequence sequence = DOTween.Sequence()
                .AppendInterval(i * SpawnInterval)
                .AppendCallback(() => coin.enabled = true)
                .Append(scatter)
                .AppendInterval(HoldDuration + i * FlyStagger)
                .Append(rect.DOLocalMove(end, FlyDuration).SetEase(Ease.InCubic))
                .Join(rect.DOScale(ArriveScale, FlyDuration).SetEase(Ease.InCubic))
                .SetTarget(rect)
                .OnComplete(() =>
                {
                    Release(idleCoins, coin);
                    SoundManager.Instance.PlaySfx(GameConstants.Sounds.Coin);
                    PlayPop(end, speed, frames);
                    onCoinArrived?.Invoke();
                });
            sequence.timeScale = speed;
        }
    }

    // 처음에는 jumpPower만큼 솟았다가 landing에 떨어지고, 그 자리에서 BounceCount번 튕긴다. progress는 0~1
    // 튕길 때마다 높이는 BounceHeightRatio배로 줄고, 구간 시간은 높이의 제곱근에 비례해서 실제로 튕기는 것처럼 짧아진다
    private static Vector2 GetScatterPosition(Vector2 start, Vector2 landing, float jumpPower, float progress)
    {
        float totalWeight = 0f;
        for (int i = 0; i <= BounceCount; i++)
        {
            totalWeight += Mathf.Sqrt(Mathf.Pow(BounceHeightRatio, i));
        }

        float x = Mathf.LerpUnclamped(start.x, landing.x, progress);
        float remaining = progress * totalWeight;
        for (int i = 0; i < BounceCount; i++)
        {
            float weight = Mathf.Sqrt(Mathf.Pow(BounceHeightRatio, i));
            if (remaining <= weight)
            {
                return new Vector2(x, GetBounceY(start, landing, jumpPower, i, remaining / weight));
            }

            remaining -= weight;
        }

        float lastWeight = Mathf.Sqrt(Mathf.Pow(BounceHeightRatio, BounceCount));
        return new Vector2(x, GetBounceY(start, landing, jumpPower, BounceCount, Mathf.Clamp01(remaining / lastWeight)));
    }

    // index번째 구간의 높이. 0번은 출발점에서 landing까지 내려가고, 그 뒤로는 landing 높이에서 튕긴다
    private static float GetBounceY(Vector2 start, Vector2 landing, float jumpPower, int index, float segment)
    {
        float baseY = index == 0 ? Mathf.Lerp(start.y, landing.y, segment) : landing.y;
        float height = jumpPower * Mathf.Pow(BounceHeightRatio, index);
        return baseY + height * 4f * segment * (1f - segment);
    }

    // 동전이 닿은 자리에서 반짝임 프레임을 한 번 넘기고 사라진다
    private void PlayPop(Vector2 position, float speed, Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
            return;

        Image pop = Get(idlePops, popTemplate, "Pop");
        pop.rectTransform.localPosition = position;
        pop.sprite = frames[0];

        // 0부터 프레임 수까지 올리면서 정수 부분을 프레임 번호로 쓴다. 마지막 프레임도 한 프레임 시간만큼 보인다
        var frame = 0f;
        Tween tween = DOTween.To(() => frame, value =>
            {
                frame = value;
                pop.sprite = frames[Mathf.Min((int)value, frames.Length - 1)];
            }, frames.Length, frames.Length / PopFrameRate)
            .SetEase(Ease.Linear)
            .SetTarget(pop.rectTransform)
            .OnComplete(() => Release(idlePops, pop));
        tween.timeScale = speed;
    }

    // BattleCanvas와 LobbyCanvas는 Screen Space Overlay라 카메라 없이 화면 좌표를 바꾼다
    private Vector2 ToLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out Vector2 local);
        return local;
    }

    // 쉬고 있는 것이 있으면 다시 쓰고, 없으면 템플릿을 템플릿과 같은 레이어에 복제한다. 나중에 꺼낸 것이 위에 그려지도록 맨 뒤로 보낸다
    // CoinPopLayer도 화면 전체를 덮으므로 CoinLayer 기준 좌표를 그대로 쓸 수 있다
    private Image Get(Stack<Image> idle, Image template, string name)
    {
        Image image = idle.Count > 0 ? idle.Pop() : Instantiate(template, template.transform.parent);
        image.name = name;
        image.transform.SetAsLastSibling();
        image.gameObject.SetActive(true);
        return image;
    }

    private void Release(Stack<Image> idle, Image image)
    {
        image.gameObject.SetActive(false);
        idle.Push(image);
    }
}
