using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTabButton : MonoBehaviour
{
    [ReadOnly] [SerializeField] private ButtonUI btnTab;
    [ReadOnly] [SerializeField] private LayoutElement layoutElement;
    [ReadOnly] [SerializeField] private GameObject objSelectedMark;
    [ReadOnly] [SerializeField] private RectTransform rectIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLabel;
    [ReadOnly] [SerializeField] private GameObject objSeparator;

    [ReadOnly(true)] [SerializeField] private float normalWidthRatio = 1f; // 하단 메뉴의 남는 너비를 이 비율대로 나눠 가짐
    [ReadOnly(true)] [SerializeField] private float selectedWidthRatio = 1.6f;
    [ReadOnly(true)] [SerializeField] private float widthAnimationDuration = 0.28f;

    [ReadOnly(true)] [SerializeField] private float normalIconSize = 96f; // 아이콘은 BottomCenter 피벗이라 위로 커짐
    [ReadOnly(true)] [SerializeField] private float selectedIconSize = 160f;
    [ReadOnly(true)] [SerializeField] private float normalLabelFontSize = 32f;
    [ReadOnly(true)] [SerializeField] private float selectedLabelFontSize = 44f;
    [ReadOnly(true)] [SerializeField] private Color normalLabelColor = GameConstants.Colors.TextSecondary;
    [ReadOnly(true)] [SerializeField] private Color selectedLabelColor = GameConstants.Colors.TextPrimary;

    private Tween tweenWidth;
    private Tween tweenIcon;

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
        rectIcon = GameUtil.Bind<RectTransform>(transform, "Icon");
        txtLabel = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        objSeparator = GameUtil.TryBind<RectTransform>(transform, "Separator")?.gameObject;

        btnTab.OnClickEvent += OnClickTab;
    }

    private void OnDestroy()
    {
        tweenWidth?.Kill();
        tweenIcon?.Kill();
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

        tweenIcon?.Kill();
        rectIcon.sizeDelta = GetIconSize(selected);
    }

    public void AnimateSelected(bool selected)
    {
        UpdateSelected(selected);

        tweenWidth?.Kill();
        tweenWidth = DOTween.To(() => layoutElement.flexibleWidth, x => layoutElement.flexibleWidth = x, GetWidthRatio(selected), widthAnimationDuration)
            .SetEase(Ease.OutCubic);

        tweenIcon?.Kill();
        tweenIcon = rectIcon.DOSizeDelta(GetIconSize(selected), widthAnimationDuration)
            .SetEase(Ease.OutCubic);
    }

    public void SetSeparatorVisible(bool visible)
    {
        objSeparator?.SetActive(visible);
    }

    private void UpdateSelected(bool selected)
    {
        this.selected = selected;
        objSelectedMark.SetActive(selected);

        txtLabel.fontSize = selected
            ? selectedLabelFontSize
            : normalLabelFontSize;
        txtLabel.color = selected
            ? selectedLabelColor
            : normalLabelColor;
    }

    private float GetWidthRatio(bool selected)
    {
        return selected
            ? selectedWidthRatio
            : normalWidthRatio;
    }

    private Vector2 GetIconSize(bool selected)
    {
        float size = selected
            ? selectedIconSize
            : normalIconSize;

        return new Vector2(size, size);
    }

    private void OnClickTab()
    {
        OnClickTabButtonEvent.Invoke(this);
    }
}
