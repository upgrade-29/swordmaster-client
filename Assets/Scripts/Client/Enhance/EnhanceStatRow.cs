using TMPro;
using UnityEngine;

public class EnhanceStatRow : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCurrent;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNext;

    private void Awake()
    {
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        txtCurrent = GameUtil.Bind<TextMeshProUGUI>(transform, "Current");
        txtNext = GameUtil.Bind<TextMeshProUGUI>(transform, "Next");
    }

    public void Set(string name, string current, string next)
    {
        txtName.text = name;
        txtCurrent.text = current;
        txtNext.text = next;
    }
}
