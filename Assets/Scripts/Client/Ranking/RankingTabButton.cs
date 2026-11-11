using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랭킹 팝업 위쪽의 종류 탭 하나. 고른 탭은 밝은 이미지와 글자색으로 바꾼다
public class RankingTabButton : MonoBehaviour
{
    [ReadOnly] [SerializeField] private ButtonUI btnTab;
    [ReadOnly] [SerializeField] private Image imgTab;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLabel;

    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteSelected;

    private RankingType rankingType;
    public RankingType RankingType => rankingType;

    public event Action<RankingTabButton> OnClickTabEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnTab);
        GameUtil.Bind(gameObject, ref imgTab);
        txtLabel = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");

        btnTab.OnClickEvent += OnClickTab;
    }

    public void Init(RankingType rankingType)
    {
        this.rankingType = rankingType;
    }

    public void SetSelected(bool selected)
    {
        imgTab.sprite = selected
            ? spriteSelected
            : spriteNormal;
        txtLabel.color = selected
            ? GameConstants.Colors.TextPrimary
            : GameConstants.Colors.TextSecondary;
    }

    private void OnClickTab()
    {
        OnClickTabEvent.Invoke(this);
    }
}
