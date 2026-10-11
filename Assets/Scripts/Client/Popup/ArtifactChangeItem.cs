using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactChangeItem : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Button btnItem;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGrade;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLevel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStat;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStatus;

    public event Action<ArtifactChangeItem> OnClickItemEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnItem);
        txtGrade = GameUtil.Bind<TextMeshProUGUI>(transform, "Top/Grade");
        txtLevel = GameUtil.Bind<TextMeshProUGUI>(transform, "Top/Level");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Name");
        txtStat = GameUtil.Bind<TextMeshProUGUI>(transform, "Stat");
        txtStatus = GameUtil.Bind<TextMeshProUGUI>(transform, "Status/Label");

        btnItem.onClick.AddListener(OnClickItem);
    }

    public void Set(string grade, string level, string name, string stat, string status, bool interactable)
    {
        txtGrade.text = grade;
        txtLevel.text = level;
        txtName.text = name;
        txtStat.text = stat;
        txtStatus.text = status;
        btnItem.interactable = interactable;
    }

    private void OnClickItem()
    {
        OnClickItemEvent.Invoke(this);
    }
}
