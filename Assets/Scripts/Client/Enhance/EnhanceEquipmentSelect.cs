using System;
using System.Collections.Generic;
using UnityEngine;

public class EnhanceEquipmentSelect : MonoBehaviour
{
    public const int ArtifactSlotCount = 3;

    [ReadOnly] [SerializeField] private EnhanceEquipmentButton swordEquipmentButton;
    [ReadOnly] [SerializeField] private List<EnhanceEquipmentButton> listArtifactEquipmentButton = new List<EnhanceEquipmentButton>();

    public event Action OnClickSwordButtonEvent = delegate { };
    public event Action<int> OnClickArtifactButtonEvent = delegate { };

    private void Awake()
    {
        swordEquipmentButton = GameUtil.Bind<EnhanceEquipmentButton>(transform, "SwordButton");
        swordEquipmentButton.OnClickEquipmentButtonEvent += OnClickSwordButton;

        for (int i = 1; i <= ArtifactSlotCount; i++)
        {
            var artifactEquipmentButton = GameUtil.Bind<EnhanceEquipmentButton>(transform, $"ArtifactButton{i.ToString()}");
            artifactEquipmentButton.OnClickEquipmentButtonEvent += OnClickArtifactButton;
            listArtifactEquipmentButton.Add(artifactEquipmentButton);
        }
    }

    private void OnDestroy()
    {
        if (swordEquipmentButton != null)
        {
            swordEquipmentButton.OnClickEquipmentButtonEvent -= OnClickSwordButton;
        }

        foreach (var artifactEquipmentButton in listArtifactEquipmentButton)
        {
            if (artifactEquipmentButton == null)
            {
                continue;
            }

            artifactEquipmentButton.OnClickEquipmentButtonEvent -= OnClickArtifactButton;
        }
    }

    public void SetSwordName(string name)
    {
        swordEquipmentButton.SetName(name);
    }

    public void SetArtifactName(int slotIndex, string name)
    {
        listArtifactEquipmentButton[slotIndex].SetName(name);
    }

    public void SetSelectedSword()
    {
        swordEquipmentButton.SetSelected(true);
        foreach (var artifactEquipmentButton in listArtifactEquipmentButton)
        {
            artifactEquipmentButton.SetSelected(false);
        }
    }

    public void SetSelectedArtifact(int slotIndex)
    {
        swordEquipmentButton.SetSelected(false);
        for (int i = 0; i < listArtifactEquipmentButton.Count; i++)
        {
            listArtifactEquipmentButton[i].SetSelected(i == slotIndex);
        }
    }

    private void OnClickSwordButton(EnhanceEquipmentButton enhanceEquipmentButton)
    {
        OnClickSwordButtonEvent.Invoke();
    }

    private void OnClickArtifactButton(EnhanceEquipmentButton enhanceEquipmentButton)
    {
        OnClickArtifactButtonEvent.Invoke(listArtifactEquipmentButton.IndexOf(enhanceEquipmentButton));
    }
}
