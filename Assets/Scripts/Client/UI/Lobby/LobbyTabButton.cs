using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTabButton : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Button btnTab;
    [ReadOnly] [SerializeField] private LayoutElement layoutElement;
    [ReadOnly] [SerializeField] private GameObject objSelectedMark;

    [ReadOnly(true)] [SerializeField] private float normalWidthRatio = 1f; // 하단 메뉴의 남는 너비를 이 비율대로 나눠 가짐
    [ReadOnly(true)] [SerializeField] private float selectedWidthRatio = 1.6f;
    [ReadOnly(true)] [SerializeField] private float widthAnimationDuration = 0.28f;

    private Tween tweenWidth;

    private LobbyTabType tabType;
    public LobbyTabType TabType => tabType;

    private bool selected;
    public bool Selected => selected;

    public event Action<LobbyTabButton> OnClickTabButtonEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnTab);
        GameUtil.Bind(gameObject, ref layoutElement);
        objSelectedMark = GameUtil.Bind<RectTransform>(transform, "Selected").gameObject;

        btnTab.onClick.AddListener(OnClickTab);
    }

    private void OnDestroy()
    {
        tweenWidth?.Kill();
    }

    public void Init(LobbyTabType tabType)
    {
        this.tabType = tabType;
    }

    public void SetSelected(bool selected)
    {
        UpdateSelected(selected);

        tweenWidth?.Kill();
        layoutElement.flexibleWidth = GetWidthRatio(selected);
    }

    public void AnimateSelected(bool selected)
    {
        UpdateSelected(selected);

        tweenWidth?.Kill();
        tweenWidth = DOTween.To(() => layoutElement.flexibleWidth, x => layoutElement.flexibleWidth = x, GetWidthRatio(selected), widthAnimationDuration)
            .SetEase(Ease.OutCubic);
    }

    private void UpdateSelected(bool selected)
    {
        this.selected = selected;
        
        // 테스트 용 코드
        // objSelectedMark.SetActive(selected);
    }

    private float GetWidthRatio(bool selected)
    {
        return selected
            ? selectedWidthRatio
            : normalWidthRatio;
    }

    private void OnClickTab()
    {
        OnClickTabButtonEvent.Invoke(this);
    }
}
