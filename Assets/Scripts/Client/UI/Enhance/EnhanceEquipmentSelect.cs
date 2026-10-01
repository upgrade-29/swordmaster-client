using System;
using System.Collections.Generic;
using UnityEngine;

public class EnhanceEquipmentSelect : MonoBehaviour
{
    private const int ArtifactSlotCount = 3;

    [ReadOnly] [SerializeField] private List<EnhanceEquipmentButton> listEnhanceEquipmentButton = new List<EnhanceEquipmentButton>();

    public event Action<int> OnClickEquipmentButtonEvent = delegate { };

    private void Awake()
    {
        listEnhanceEquipmentButton.Add(GameUtil.Bind<EnhanceEquipmentButton>(transform, "SwordButton"));
        for (int i = 1; i <= ArtifactSlotCount; i++)
        {
            listEnhanceEquipmentButton.Add(GameUtil.Bind<EnhanceEquipmentButton>(transform, $"ArtifactButton{i.ToString()}"));
        }

        foreach (var enhanceEquipmentButton in listEnhanceEquipmentButton)
        {
            enhanceEquipmentButton.OnClickEquipmentButtonEvent += OnClickEquipmentButton;
        }
    }

    private void OnDestroy()
    {
        foreach (var enhanceEquipmentButton in listEnhanceEquipmentButton)
        {
            if (enhanceEquipmentButton == null)
            {
                continue;
            }

            enhanceEquipmentButton.OnClickEquipmentButtonEvent -= OnClickEquipmentButton;
        }
    }

    public void SetSelectedIndex(int selectedIndex)
    {
        for (int i = 0; i < listEnhanceEquipmentButton.Count; i++)
        {
            listEnhanceEquipmentButton[i].SetSelected(i == selectedIndex);
        }
    }

    private void OnClickEquipmentButton(EnhanceEquipmentButton enhanceEquipmentButton)
    {
        OnClickEquipmentButtonEvent.Invoke(listEnhanceEquipmentButton.IndexOf(enhanceEquipmentButton));
    }
}
