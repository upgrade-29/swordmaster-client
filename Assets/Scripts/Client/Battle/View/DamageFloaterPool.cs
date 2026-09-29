using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

// 머리 위로 떠오르며 사라지는 데미지 숫자. 공격마다 새로 만들지 않고 다 쓴 것을 다시 쓴다
public class DamageFloaterPool : IDisposable
{
    private const float RiseHeight = 0.8f;
    private const float Duration = 0.7f;
    private const float FadeDelay = 0.3f; // 잠깐 또렷하게 보인 뒤에 흐려진다
    private const float FontSize = 5f;
    private const float CritFontSize = 7f;
    private const int SortingOrder = 100; // 유닛 스프라이트보다 앞에 그린다

    private readonly Transform root;
    private readonly Stack<TextMeshPro> idleTexts = new Stack<TextMeshPro>();

    public DamageFloaterPool()
    {
        root = new GameObject("DamageFloaters").transform;
    }

    public void Dispose()
    {
        if (root == null)
            return;

        foreach (Transform child in root)
            child.DOKill();
        Object.Destroy(root.gameObject);
    }

    public void Show(Vector3 position, string text, Color color, bool isCrit)
    {
        TextMeshPro floater = idleTexts.Count > 0 ? idleTexts.Pop() : Create();

        floater.text = text;
        floater.color = color;
        floater.fontSize = isCrit ? CritFontSize : FontSize;
        floater.transform.position = position;
        floater.gameObject.SetActive(true);

        DOTween.Sequence()
            .Append(floater.transform.DOMoveY(position.y + RiseHeight, Duration).SetEase(Ease.OutQuad))
            .Insert(FadeDelay, DOTween.To(() => floater.alpha, alpha => floater.alpha = alpha, 0f, Duration - FadeDelay))
            .SetTarget(floater.transform)
            .OnComplete(() => Release(floater));
    }

    private TextMeshPro Create()
    {
        var floater = new GameObject("DamageFloater").AddComponent<TextMeshPro>();
        floater.transform.SetParent(root, false);
        floater.alignment = TextAlignmentOptions.Center;
        floater.fontStyle = FontStyles.Bold;
        floater.textWrappingMode = TextWrappingModes.NoWrap;
        floater.sortingOrder = SortingOrder;
        return floater;
    }

    private void Release(TextMeshPro floater)
    {
        floater.gameObject.SetActive(false);
        idleTexts.Push(floater);
    }
}
