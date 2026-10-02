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
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDesc;

    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDetailTitle;
    [ReadOnly] [SerializeField] private GameObject objDetailGuide;
    [ReadOnly] [SerializeField] private List<EnhanceStatRow> listEnhanceStatRow = new List<EnhanceStatRow>();

    [ReadOnly] [SerializeField] private GameObject objEmptyGuide;

    private void Awake()
    {
        imgSword = GameUtil.Bind<Image>(transform, "EquipmentPreview/EquipmentImage/SwordImage");
        imgArtifact = GameUtil.Bind<Image>(transform, "EquipmentPreview/EquipmentImage/ArtifactImage");
        txtLevel = GameUtil.Bind<TextMeshProUGUI>(transform, "EquipmentPreview/EquipmentName/NameRow/Level");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "EquipmentPreview/EquipmentName/NameRow/Name");
        txtDesc = GameUtil.Bind<TextMeshProUGUI>(transform, "EquipmentPreview/EquipmentName/Desc");

        txtDetailTitle = GameUtil.Bind<TextMeshProUGUI>(transform, "EnhanceDetail/TitleRow/Title");
        objDetailGuide = GameUtil.Bind<RectTransform>(transform, "EnhanceDetail/TitleRow/Guide").gameObject;
        for (int i = 1; i <= StatRowCount; i++)
        {
            listEnhanceStatRow.Add(GameUtil.Bind<EnhanceStatRow>(transform, $"EnhanceDetail/StatRow{i.ToString()}"));
        }

        objEmptyGuide = GameUtil.Bind<RectTransform>(transform, "EnhanceDetail/EmptyGuide").gameObject;
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
