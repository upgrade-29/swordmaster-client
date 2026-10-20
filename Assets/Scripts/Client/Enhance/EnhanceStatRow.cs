using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceStatRow : MonoBehaviour
{
    private const float ChangedHighlightDuration = 0.5f; // 바뀐 현재 값이 변화 색에서 원래 색으로 돌아오는 시간

    [ReadOnly] [SerializeField] private Image imgIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCurrent;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNext;

    private Color colorCurrent;

    private void Awake()
    {
        imgIcon = GameUtil.Bind<Image>(transform, "Icon");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        txtCurrent = GameUtil.Bind<TextMeshProUGUI>(transform, "Current");
        txtNext = GameUtil.Bind<TextMeshProUGUI>(transform, "Next");
        colorCurrent = txtCurrent.color;
    }

    // changedColor를 넘기면, 같은 능력치의 현재 값이 바뀐 경우에만 그 색에서 원래 색으로 돌아온다
    public void Set(Sprite icon, string name, string current, string next, Color? changedColor = null)
    {
        bool isChanged = changedColor.HasValue && txtName.text == name && txtCurrent.text != current;

        imgIcon.sprite = icon;
        txtName.text = name;
        txtCurrent.text = current;
        txtNext.text = next;

        txtCurrent.DOKill();
        txtCurrent.color = colorCurrent;
        if (isChanged)
        {
            txtCurrent.color = changedColor.Value;
            txtCurrent.DOColor(colorCurrent, ChangedHighlightDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }
    }
}
