using UnityEngine;

public class EnhanceTab : LobbyTab
{
    [ReadOnly] [SerializeField] private EnhanceTitle enhanceTitle;
    [ReadOnly] [SerializeField] private EnhanceEquipmentSelect enhanceEquipmentSelect;
    [ReadOnly] [SerializeField] private EnhanceEquipmentInfo enhanceEquipmentInfo;
    [ReadOnly] [SerializeField] private EnhanceEquipmentAction enhanceEquipmentAction;
    [ReadOnly] [SerializeField] private EnhanceStatsPanel enhanceStatsPanel;

    private bool isSwordSelected;
    private int selectedArtifactSlot;
    private bool isStatsOpened;

    protected override void Awake()
    {
        base.Awake();

        enhanceTitle = GameUtil.Bind<EnhanceTitle>(transform, "Title");
        enhanceTitle.OnClickCombatPowerEvent += OnClickCombatPower;

        enhanceEquipmentSelect = GameUtil.Bind<EnhanceEquipmentSelect>(transform, "Viewport/Content/EquipmentSelect");
        enhanceEquipmentSelect.OnClickSwordButtonEvent += OnClickSwordButton;
        enhanceEquipmentSelect.OnClickArtifactButtonEvent += OnClickArtifactButton;

        enhanceEquipmentInfo = GameUtil.Bind<EnhanceEquipmentInfo>(transform, "Viewport/Content/EquipmentInfo");
        enhanceEquipmentAction = GameUtil.Bind<EnhanceEquipmentAction>(transform, "EquipmentAction");
        enhanceStatsPanel = GameUtil.Bind<EnhanceStatsPanel>(transform, "StatsPanel");
    }

    private void Start()
    {
        SelectSword();
        SetStatsOpened(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (enhanceTitle != null)
        {
            enhanceTitle.OnClickCombatPowerEvent -= OnClickCombatPower;
        }

        if (enhanceEquipmentSelect != null)
        {
            enhanceEquipmentSelect.OnClickSwordButtonEvent -= OnClickSwordButton;
            enhanceEquipmentSelect.OnClickArtifactButtonEvent -= OnClickArtifactButton;
        }
    }

    private void SelectSword()
    {
        isSwordSelected = true;
        enhanceEquipmentSelect.SetSelectedSword();
        enhanceEquipmentInfo.ShowSword();
        enhanceEquipmentAction.ShowSword();
    }

    private void SelectArtifact(int slotIndex)
    {
        isSwordSelected = false;
        selectedArtifactSlot = slotIndex;
        enhanceEquipmentSelect.SetSelectedArtifact(slotIndex);
        enhanceEquipmentInfo.ShowArtifact();
        enhanceEquipmentAction.ShowArtifact();
    }

    private void SetStatsOpened(bool opened)
    {
        isStatsOpened = opened;
        enhanceStatsPanel.SetOpened(opened);
        enhanceTitle.SetStatsOpened(opened);
    }

    private void OnClickCombatPower()
    {
        SetStatsOpened(isStatsOpened == false);
    }

    private void OnClickSwordButton()
    {
        SelectSword();
    }

    private void OnClickArtifactButton(int slotIndex)
    {
        SelectArtifact(slotIndex);
    }
}
