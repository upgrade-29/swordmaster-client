using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// 적을 처치하면 적 주변에 동전을 흩뿌리고, 잠깐 뒤 골드 HUD로 빨아들인다
// 화면 전체를 덮는 BattleCanvas/CoinLayer에 붙인다. 자식 CoinTemplate을 복제해서 쓰고, 다 쓴 동전은 다시 쓴다
public class CoinFlyView : MonoBehaviour
{
    private const float ScatterDuration = 0.45f;
    private const float ScatterMinDistance = 40f; // 좌우로 튀는 거리
    private const float ScatterMaxDistance = 140f;
    private const float JumpMinPower = 60f; // 떨어지기 전에 위로 솟는 높이
    private const float JumpMaxPower = 120f;
    private const float DropMinDistance = 20f; // 출발점보다 아래에 떨어진다
    private const float DropMaxDistance = 70f;
    private const float HoldDuration = 0.25f; // 바닥에 떨어진 채로 잠깐 보여준다
    private const float FlyStagger = 0.08f; // 한꺼번에 날아가지 않고 하나씩 따라간다
    private const float FlyDuration = 0.45f;
    private const float ArriveScale = 0.6f;

    private RectTransform layer;
    private Image template;
    private Camera worldCamera;
    private readonly Stack<Image> idleCoins = new Stack<Image>();

    private void Awake()
    {
        layer = (RectTransform)transform;
        template = GameUtil.Bind<Image>(gameObject, "CoinTemplate");
        template.gameObject.SetActive(false);
        worldCamera = Camera.main;
    }

    private void OnDestroy()
    {
        foreach (Transform child in transform)
            child.DOKill();
    }

    // worldPosition은 적이 서 있던 월드 좌표, target은 동전이 빨려 들어갈 HUD
    // onCoinArrived는 동전 하나가 HUD에 닿을 때마다 불린다
    public void Play(Vector3 worldPosition, RectTransform target, int count, Action onCoinArrived)
    {
        Vector2 start = ToLocal(worldCamera.WorldToScreenPoint(worldPosition));
        Vector2 end = ToLocal(RectTransformUtility.WorldToScreenPoint(null, target.position));

        for (var i = 0; i < count; i++)
        {
            Image coin = idleCoins.Count > 0 ? idleCoins.Pop() : Create();
            RectTransform rect = coin.rectTransform;
            rect.localPosition = start;
            rect.localScale = Vector3.one;
            coin.gameObject.SetActive(true);

            var side = Random.value < 0.5f ? -1f : 1f;
            var landing = new Vector2(
                start.x + side * Random.Range(ScatterMinDistance, ScatterMaxDistance),
                start.y - Random.Range(DropMinDistance, DropMaxDistance));

            DOTween.Sequence()
                .Append(rect.DOLocalJump(landing, Random.Range(JumpMinPower, JumpMaxPower), 1, ScatterDuration))
                .AppendInterval(HoldDuration + i * FlyStagger)
                .Append(rect.DOLocalMove(end, FlyDuration).SetEase(Ease.InCubic))
                .Join(rect.DOScale(ArriveScale, FlyDuration).SetEase(Ease.InCubic))
                .SetTarget(rect)
                .OnComplete(() =>
                {
                    Release(coin);
                    onCoinArrived?.Invoke();
                });
        }
    }

    // BattleCanvas는 Screen Space Overlay라 카메라 없이 화면 좌표를 바꾼다
    private Vector2 ToLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, null, out Vector2 local);
        return local;
    }

    private Image Create()
    {
        Image coin = Instantiate(template, layer);
        coin.name = "Coin";
        return coin;
    }

    private void Release(Image coin)
    {
        coin.gameObject.SetActive(false);
        idleCoins.Push(coin);
    }
}
