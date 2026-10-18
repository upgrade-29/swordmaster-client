using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceStatRow : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image imgIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCurrent;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNext;

    private void Awake()
    {
        imgIcon = GameUtil.Bind<Image>(transform, "Icon");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        txtCurrent = GameUtil.Bind<TextMeshProUGUI>(transform, "Current");
        txtNext = GameUtil.Bind<TextMeshProUGUI>(transform, "Next");
    }

    public void Set(Sprite icon, string name, string current, string next)
    {
        imgIcon.sprite = icon;
        txtName.text = name;
        txtCurrent.text = current;
        txtNext.text = next;
    }
}
