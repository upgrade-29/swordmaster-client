using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceEquipmentButton : MonoBehaviour
{
    [ReadOnly] [SerializeField] private ButtonUI btnEquipment;
    [ReadOnly] [SerializeField] private Image imgIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private GameObject objSelectedMark;

    private Color colorIconPlaceholder; // 이미지가 없을 때 보여줄 회색 상자 색

    public event Action<EnhanceEquipmentButton> OnClickEquipmentButtonEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnEquipment);
        imgIcon = GameUtil.Bind<Image>(transform, "Icon");
        colorIconPlaceholder = imgIcon.color;
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        objSelectedMark = GameUtil.Bind<RectTransform>(transform, "Selected").gameObject;

        btnEquipment.OnClickEvent += OnClickEquipment;
    }

    public void SetName(string name)
    {
        txtName.text = name;
    }

    // 아이콘 칸 크기는 프리팹에서 원본의 정수 배율로 맞춰 둔다. 미리 읽은 이미지가 없으면 회색 상자를 그대로 둔다
    // 빈 슬롯이면 imageCode에 null을 넘기고, 아이콘을 숨긴다
    public void SetIcon(string imageCode)
    {
        imgIcon.enabled = string.IsNullOrEmpty(imageCode) == false;
        Sprite sprite = ResourceManager.Instance.GetSprite(imageCode);
        imgIcon.sprite = sprite;
        imgIcon.color = sprite != null ? Color.white : colorIconPlaceholder;
    }

    public void SetSelected(bool selected)
    {
        objSelectedMark.SetActive(selected);
    }

    private void OnClickEquipment()
    {
        OnClickEquipmentButtonEvent.Invoke(this);
    }
}
