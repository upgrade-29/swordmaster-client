using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceEquipmentInfo : MonoBehaviour
{
    private const int StatRowCount = 3;

    [ReadOnly] [SerializeField] private Image imgSword;
    [ReadOnly] [SerializeField] private Image imgArtifact;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLevel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;

    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDetailTitle;
    [ReadOnly] [SerializeField] private GameObject objDetailGuide;
    [ReadOnly] [SerializeField] private List<EnhanceStatRow> listEnhanceStatRow = new List<EnhanceStatRow>();

    [ReadOnly] [SerializeField] private GameObject objEmptyGuide;

    private void Awake()
    {
        imgSword = GameUtil.Bind<Image>(transform, "EquipmentRow/EquipmentImage/SwordImage");
        imgArtifact = GameUtil.Bind<Image>(transform, "EquipmentRow/EquipmentImage/ArtifactImage");
        txtLevel = GameUtil.Bind<TextMeshProUGUI>(transform, "EquipmentName/NameRow/Level");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "EquipmentName/NameRow/Name");

        txtDetailTitle = GameUtil.Bind<TextMeshProUGUI>(transform, "EquipmentRow/EnhanceDetail/TitleRow/Title");
        objDetailGuide = GameUtil.Bind<RectTransform>(transform, "EquipmentRow/EnhanceDetail/TitleRow/Guide").gameObject;
        for (int i = 1; i <= StatRowCount; i++)
        {
            listEnhanceStatRow.Add(GameUtil.Bind<EnhanceStatRow>(transform, $"EquipmentRow/EnhanceDetail/StatRow{i.ToString()}"));
        }

        objEmptyGuide = GameUtil.Bind<RectTransform>(transform, "EquipmentRow/EnhanceDetail/EmptyGuide").gameObject;
    }

    public void SetEquipment(string level, string name)
    {
        txtLevel.text = level;
        txtName.text = name;
    }

    public void SetStatRow(int index, string name, string current, string next)
    {
        listEnhanceStatRow[index].Set(name, current, next);
    }

    public void ShowSword()
    {
        imgSword.gameObject.SetActive(true);
        imgArtifact.gameObject.SetActive(false);
    }

    public void ShowArtifact()
    {
        imgSword.gameObject.SetActive(false);
        imgArtifact.gameObject.SetActive(true);
    }
}
