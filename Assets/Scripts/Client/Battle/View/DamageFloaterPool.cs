using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// 맞은 자리에서 톡 튀어나와 옆으로 포물선을 그리며 떨어지고 흐려지는 데미지 숫자. 공격마다 새로 만들지 않고 다 쓴 것을 다시 쓴다
// 치명타면 숫자가 더 커지고 위에 CRITICAL이 붙으며, 숫자만 잠깐 흔들린다
public class DamageFloaterPool : IDisposable
{
    private const float Duration = 0.85f; // 숫자 하나가 떠 있는 전체 시간(초)
    private const float BaseFontSize = 3.75f; // 크기 배율 1일 때의 글꼴 크기
    private const int SortingOrder = 100; // 유닛 스프라이트와 이름판보다 앞에 그린다

    // 팝: 작게 나타나 크게 튀었다가 원래 크기로 돌아온다
    private const float PopStartScale = 0.3f;
    private const float PopPeakScale = 1.2f;
    private const float PopGrowDuration = 0.1f;
    private const float PopSettleDuration = 0.15f;

    // 플로팅: 위로 떠오른다
    private const float FloatHeight = 0.8f;
    private const float FloatDuration = 0.7f;

    // 아칭: 무작위 좌우로 튀어 올랐다가 시작 높이보다 아래로 떨어진다. Duration 동안 일정한 속도로 진행한다
    private const float ArcDistance = 0.6f;
    private const float ArcJumpHeight = 0.3f;
    private const float ArcEndHeight = -1f;

    // 페이딩
    private const float FadeDelay = 0.31f;
    private const float FadeDuration = 0.5f;

    // 사이징: 대상 최대 체력에 대한 데미지 비율로 크기 배율을 정한다
    private const float MinSizeScale = 0.75f;
    private const float MaxSizeScale = 1.25f; // 최대 체력의 StrongHitDamageRatio 이상을 잃으면 최대 크기

    // 치명타
    private const float CritSizeScale = 1.25f; // 사이징 결과에 곱한다
    private const float CritLabelSizeRatio = 0.25f; // 숫자 글꼴 크기에 대한 비율
    private const float CritLabelGap = 0.05f; // 숫자 위쪽 끝과 CRITICAL 사이 간격
    private const string CritLabelText = "CRITICAL";
    private const float ShakeDuration = 0.3f;
    private const float ShakeStrength = 0.08f;
    private const int ShakeVibrato = 30;
    private const float ShakeRandomness = 90f;

    // 겹침 방지: 시작 위치를 이 범위 안에서 무작위로 흩뿌린다
    private static readonly Vector2 SpawnJitter = new Vector2(0.3f, 0.2f);

    private readonly Transform root;
    private readonly TMP_FontAsset font;
    private readonly List<Floater> listFloater = new List<Floater>();
    private readonly Stack<Floater> idleFloaters = new Stack<Floater>();

    // 위치, 팝, 투명도는 루트가 맡고, 치명타 흔들림은 숫자만 받는다. CRITICAL은 숫자와 따로 두어 흔들리지 않는다
    private class Floater
    {
        public Transform trnsRoot;
        public TextMeshPro txtNumber;
        public TextMeshPro txtLabel;
    }

    public DamageFloaterPool()
    {
        root = new GameObject("DamageFloaters").transform;
        font = Resources.Load<TMP_FontAsset>(GameConstants.ResourcePaths.DamageFont);
    }

    public void Dispose()
    {
        if (root == null)
        {
            return;
        }

        foreach (Floater floater in listFloater)
        {
            floater.trnsRoot.DOKill();
            floater.txtNumber.transform.DOKill();
        }

        Object.Destroy(root.gameObject);
    }

    /// <summary>
    /// 대상 최대 체력에 대한 데미지 비율로 숫자 크기 배율을 구한다. Show의 sizeScale에 넘긴다.
    /// </summary>
    public static float GetSizeScale(double damage, double targetMaxHp)
    {
        float damageRatio = targetMaxHp > 0
            ? (float)(damage / targetMaxHp)
            : 0f;
        return Mathf.Lerp(MinSizeScale, MaxSizeScale, damageRatio / GameConstants.BattleEffect.StrongHitDamageRatio);
    }

    /// <summary>
    /// position에서 숫자를 띄운다. 치명타면 sizeScale에 치명타 배율을 더 곱하고 CRITICAL을 붙인다.
    /// </summary>
    public void Show(Vector3 position, string text, Color color, float sizeScale, bool isCrit)
    {
        Floater floater = idleFloaters.Count > 0 ? idleFloaters.Pop() : Create();

        float fontSize = BaseFontSize * sizeScale * (isCrit ? CritSizeScale : 1f);
        floater.txtNumber.text = text;
        floater.txtNumber.color = color;
        floater.txtNumber.fontSize = fontSize;
        floater.txtNumber.transform.localPosition = Vector3.zero;

        floater.txtLabel.gameObject.SetActive(isCrit);
        if (isCrit)
        {
            SetCritLabel(floater, fontSize);
        }

        var jitter = new Vector3(Random.Range(-SpawnJitter.x, SpawnJitter.x), Random.Range(-SpawnJitter.y, SpawnJitter.y), 0f);
        Vector3 startPosition = position + jitter;
        float arcSign = Random.value < 0.5f ? -1f : 1f;

        Apply(floater, startPosition, arcSign, 0f);
        floater.trnsRoot.gameObject.SetActive(true);

        DOVirtual.Float(0f, Duration, Duration, time => Apply(floater, startPosition, arcSign, time))
            .SetEase(Ease.Linear)
            .SetTarget(floater.trnsRoot)
            .OnComplete(() => Release(floater));

        if (isCrit)
        {
            floater.txtNumber.transform.DOShakePosition(ShakeDuration, new Vector3(ShakeStrength, ShakeStrength, 0f),
                ShakeVibrato, ShakeRandomness);
        }
    }

    // 숫자 글자의 위쪽 끝에서 간격만큼 띄운 자리에 CRITICAL의 아래쪽 끝을 맞춘다
    private void SetCritLabel(Floater floater, float numberFontSize)
    {
        floater.txtLabel.color = GameConstants.Colors.CritLabel;
        floater.txtLabel.fontSize = numberFontSize * CritLabelSizeRatio;

        floater.txtNumber.ForceMeshUpdate();
        float numberTop = floater.txtNumber.textBounds.max.y;
        floater.txtLabel.transform.localPosition = new Vector3(0f, numberTop + CritLabelGap, 0f);
    }

    // time초 시점의 위치, 크기, 투명도를 한 번에 정한다. 플로팅과 아칭은 높이를 더한다
    private void Apply(Floater floater, Vector3 startPosition, float arcSign, float time)
    {
        float floatHeight = DOVirtual.EasedValue(0f, FloatHeight, Mathf.Clamp01(time / FloatDuration), Ease.OutCubic);

        float arcProgress = time / Duration;
        float arcX = arcSign * ArcDistance * arcProgress;
        float arcHeight = ArcEndHeight * arcProgress + ArcJumpHeight * 4f * arcProgress * (1f - arcProgress);

        float fadeProgress = Mathf.Clamp01((time - FadeDelay) / FadeDuration);
        float alpha = 1f - DOVirtual.EasedValue(0f, 1f, fadeProgress, Ease.InOutSine);

        floater.trnsRoot.position = startPosition + new Vector3(arcX, floatHeight + arcHeight, 0f);
        floater.trnsRoot.localScale = Vector3.one * GetPopScale(time);
        floater.txtNumber.alpha = alpha;
        floater.txtLabel.alpha = alpha;
    }

    private float GetPopScale(float time)
    {
        if (time < PopGrowDuration)
        {
            return DOVirtual.EasedValue(PopStartScale, PopPeakScale, time / PopGrowDuration, Ease.OutCubic);
        }

        float settleTime = time - PopGrowDuration;
        if (settleTime < PopSettleDuration)
        {
            return DOVirtual.EasedValue(PopPeakScale, 1f, settleTime / PopSettleDuration, Ease.InOutQuad);
        }

        return 1f;
    }

    private Floater Create()
    {
        var trnsFloater = new GameObject("DamageFloater").transform;
        trnsFloater.SetParent(root, false);

        TextMeshPro txtLabel = CreateText("CriticalLabel", trnsFloater, TextAlignmentOptions.Bottom);
        txtLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
        txtLabel.text = CritLabelText;

        var floater = new Floater
        {
            trnsRoot = trnsFloater,
            txtNumber = CreateText("Number", trnsFloater, TextAlignmentOptions.Center),
            txtLabel = txtLabel,
        };
        listFloater.Add(floater);
        return floater;
    }

    private TextMeshPro CreateText(string name, Transform parent, TextAlignmentOptions alignment)
    {
        var txt = new GameObject(name).AddComponent<TextMeshPro>();
        txt.transform.SetParent(parent, false);
        txt.font = font;
        txt.alignment = alignment;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.sortingOrder = SortingOrder;
        return txt;
    }

    private void Release(Floater floater)
    {
        floater.txtNumber.transform.DOKill();
        floater.trnsRoot.gameObject.SetActive(false);
        idleFloaters.Push(floater);
    }
}
