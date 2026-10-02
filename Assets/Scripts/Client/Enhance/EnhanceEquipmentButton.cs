using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceEquipmentButton : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Button btnEquipment;
    [ReadOnly] [SerializeField] private Image imgIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private GameObject objSelectedMark;

    public event Action<EnhanceEquipmentButton> OnClickEquipmentButtonEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnEquipment);
        imgIcon = GameUtil.Bind<Image>(transform, "Icon");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        objSelectedMark = GameUtil.Bind<RectTransform>(transform, "Selected").gameObject;

        btnEquipment.onClick.AddListener(OnClickEquipment);
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
