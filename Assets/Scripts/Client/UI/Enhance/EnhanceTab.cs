using UnityEngine;

public class EnhanceTab : LobbyTab
{
    private const int SwordIndex = 0;

    [ReadOnly] [SerializeField] private EnhanceEquipmentSelect enhanceEquipmentSelect;

    private int selectedEquipmentIndex;

    protected override void Awake()
    {
        base.Awake();

        enhanceEquipmentSelect = GameUtil.Bind<EnhanceEquipmentSelect>(transform, "Viewport/Content/EquipmentSelect");
        enhanceEquipmentSelect.OnClickEquipmentButtonEvent += OnClickEquipmentButton;
    }

    private void Start()
    {
        SelectEquipment(SwordIndex);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (enhanceEquipmentSelect == null)
        {
            return;
        }

        enhanceEquipmentSelect.OnClickEquipmentButtonEvent -= OnClickEquipmentButton;
    }

    private void SelectEquipment(int equipmentIndex)
    {
        selectedEquipmentIndex = equipmentIndex;
        enhanceEquipmentSelect.SetSelectedIndex(equipmentIndex);
    }

    private void OnClickEquipmentButton(int equipmentIndex)
    {
        SelectEquipment(equipmentIndex);
    }
}
