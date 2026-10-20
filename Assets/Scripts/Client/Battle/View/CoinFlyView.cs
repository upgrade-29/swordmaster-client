using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// 적을 처치하면 적 주변에 동전을 흩뿌리고, 잠깐 뒤 골드 HUD로 빨아들인다. 동전이 HUD에 닿으면 그 자리에서 반짝임이 터진다
// 화면 전체를 덮는 BattleCanvas/CoinLayer에 붙인다. 자식 CoinTemplate과 PopTemplate을 복제해서 쓰고, 다 쓴 것은 다시 쓴다
public class CoinFlyView : MonoBehaviour
{
    private const float ScatterDuration = 0.45f;
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

    // 동전이 HUD에 닿을 때 재생할 반짝임 프레임. 스프라이트 시트(gold_effect)를 자른 순서대로 넣는다
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
        popTemplate = GameUtil.Bind<Image>(gameObject, "PopTemplate");
        coinTemplate.gameObject.SetActive(false);
        popTemplate.gameObject.SetActive(false);
        worldCamera = Camera.main;
    }

    private void OnDestroy()
    {
        foreach (Transform child in transform)
            child.DOKill();
    }

    // worldPosition은 적이 서 있던 월드 좌표, targetPosition은 동전이 빨려 들어갈 HUD 자리(캔버스 UI의 월드 좌표)
    // onCoinArrived는 동전 하나가 HUD에 닿을 때마다 불린다. speed는 전투 배속이고, 배속만큼 빨리 끝난다
    public void Play(Vector3 worldPosition, Vector3 targetPosition, int count, float speed, Action onCoinArrived)
    {
        Vector2 start = ToLocal(worldCamera.WorldToScreenPoint(worldPosition));
        Vector2 end = ToLocal(RectTransformUtility.WorldToScreenPoint(null, targetPosition));

        for (var i = 0; i < count; i++)
        {
            Image coin = Get(idleCoins, coinTemplate, "Coin");
            RectTransform rect = coin.rectTransform;
            rect.localPosition = start;
            rect.localScale = Vector3.one;

            var side = Random.value < 0.5f ? -1f : 1f;
            var landing = new Vector2(
                start.x + side * Random.Range(ScatterMinDistance, ScatterMaxDistance),
                start.y - Random.Range(DropMinDistance, DropMaxDistance));

            Sequence sequence = DOTween.Sequence()
                .Append(rect.DOLocalJump(landing, Random.Range(JumpMinPower, JumpMaxPower), 1, ScatterDuration))
                .AppendInterval(HoldDuration + i * FlyStagger)
                .Append(rect.DOLocalMove(end, FlyDuration).SetEase(Ease.InCubic))
                .Join(rect.DOScale(ArriveScale, FlyDuration).SetEase(Ease.InCubic))
                .SetTarget(rect)
                .OnComplete(() =>
                {
                    Release(idleCoins, coin);
                    PlayPop(end, speed);
                    onCoinArrived?.Invoke();
                });
            sequence.timeScale = speed;
        }
    }

    // 동전이 닿은 자리에서 반짝임 프레임을 한 번 넘기고 사라진다
    private void PlayPop(Vector2 position, float speed)
    {
        if (popFrames == null || popFrames.Length == 0)
            return;

        Image pop = Get(idlePops, popTemplate, "Pop");
        pop.rectTransform.localPosition = position;
        pop.sprite = popFrames[0];

        // 0부터 프레임 수까지 올리면서 정수 부분을 프레임 번호로 쓴다. 마지막 프레임도 한 프레임 시간만큼 보인다
        var frame = 0f;
        Tween tween = DOTween.To(() => frame, value =>
            {
                frame = value;
                pop.sprite = popFrames[Mathf.Min((int)value, popFrames.Length - 1)];
            }, popFrames.Length, popFrames.Length / PopFrameRate)
            .SetEase(Ease.Linear)
            .SetTarget(pop.rectTransform)
            .OnComplete(() => Release(idlePops, pop));
        tween.timeScale = speed;
    }

    // BattleCanvas는 Screen Space Overlay라 카메라 없이 화면 좌표를 바꾼다
    private Vector2 ToLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out Vector2 local);
        return local;
    }

    // 쉬고 있는 것이 있으면 다시 쓰고, 없으면 템플릿을 복제한다. 나중에 꺼낸 것이 위에 그려지도록 맨 뒤로 보낸다
    private Image Get(Stack<Image> idle, Image template, string name)
    {
        Image image = idle.Count > 0 ? idle.Pop() : Instantiate(template, layer);
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
