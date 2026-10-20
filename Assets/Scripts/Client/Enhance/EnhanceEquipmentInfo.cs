using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceEquipmentInfo : MonoBehaviour
{
    private const int StatRowCount = 3;
    private const string StatsTitle = "선택 장비 능력치";
    private const string EmptySlotTitle = "장착 안내";

    [ReadOnly] [SerializeField] private Image imgSword;
    [ReadOnly] [SerializeField] private Image imgArtifact;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLevel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;

    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDetailTitle;
    [ReadOnly] [SerializeField] private GameObject objDetailGuide;
    [ReadOnly] [SerializeField] private List<EnhanceStatRow> listEnhanceStatRow = new List<EnhanceStatRow>();

    [ReadOnly] [SerializeField] private GameObject objEmptyGuide;

    // 이미지를 못 읽었거나 빈 슬롯일 때 보여줄 회색 상자 색
    private Color colorSwordPlaceholder;
    private Color colorArtifactPlaceholder;

    private void Awake()
    {
        imgSword = GameUtil.Bind<Image>(transform, "EquipmentRow/EquipmentImage/SwordImage");
        imgArtifact = GameUtil.Bind<Image>(transform, "EquipmentRow/EquipmentImage/ArtifactImage");
        colorSwordPlaceholder = imgSword.color;
        colorArtifactPlaceholder = imgArtifact.color;
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
        // 빈 슬롯처럼 단계가 없으면 숨겨서 이름이 가운데에 오게 한다
        txtLevel.gameObject.SetActive(string.IsNullOrEmpty(level) == false);
        txtLevel.text = level;
        txtName.text = name;
    }

    public void SetSwordImage(string imageCode)
    {
        SetImage(imgSword, imageCode, colorSwordPlaceholder);
    }

    // 빈 슬롯이면 imageCode에 null을 넘긴다
    public void SetArtifactImage(string imageCode)
    {
        SetImage(imgArtifact, imageCode, colorArtifactPlaceholder);
    }

    public void SetStatRow(int index, Sprite icon, string name, string current, string next)
    {
        listEnhanceStatRow[index].Set(icon, name, current, next);
    }

    public void ShowStatRows(int rowCount)
    {
        txtDetailTitle.text = StatsTitle;
        objDetailGuide.SetActive(true);
        objEmptyGuide.SetActive(false);

        for (int i = 0; i < listEnhanceStatRow.Count; i++)
        {
            listEnhanceStatRow[i].gameObject.SetActive(i < rowCount);
        }
    }

    public void ShowEmptyGuide()
    {
        txtDetailTitle.text = EmptySlotTitle;
        objDetailGuide.SetActive(false);
        objEmptyGuide.SetActive(true);

        foreach (var enhanceStatRow in listEnhanceStatRow)
        {
            enhanceStatRow.gameObject.SetActive(false);
        }
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

    // 미리 읽은 이미지가 없으면 회색 상자를 그대로 둔다
    private static void SetImage(Image image, string imageCode, Color placeholderColor)
    {
        Sprite sprite = ResourceManager.Instance.GetSprite(imageCode);
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : placeholderColor;
    }
}
