using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactChangePopup : BasePopup
{
    [ReadOnly] [SerializeField] private Button btnClose;
    [ReadOnly] [SerializeField] private Button btnUnequip;
    [ReadOnly] [SerializeField] private RectTransform rectItemContent;
    [ReadOnly] [SerializeField] private List<ArtifactChangeItem> listArtifactChangeItem = new List<ArtifactChangeItem>();

    public event Action<int> OnClickArtifactEvent = delegate { };
    public event Action OnClickUnequipEvent = delegate { };

    protected override void Awake()
    {
        base.Awake();

        btnClose = GameUtil.Bind<Button>(transform, "Panel/Header/CloseButton");
        btnUnequip = GameUtil.Bind<Button>(transform, "Panel/Footer/UnequipButton");
        rectItemContent = GameUtil.Bind<RectTransform>(transform, "Panel/ArtifactList/Viewport/Content");
        AddItem(GameUtil.Bind<ArtifactChangeItem>(rectItemContent, "Item"));

        btnClose.onClick.AddListener(OnClickClose);
        btnUnequip.onClick.AddListener(OnClickUnequip);
    }

    private void OnDestroy()
    {
        foreach (var artifactChangeItem in listArtifactChangeItem)
        {
            if (artifactChangeItem == null)
            {
                continue;
            }

            artifactChangeItem.OnClickItemEvent -= OnClickItem;
        }
    }

    public void SetUnequipInteractable(bool interactable)
    {
        btnUnequip.interactable = interactable;
    }

    public void ShowItems(int count)
    {
        while (listArtifactChangeItem.Count < count)
        {
            AddItem(Instantiate(listArtifactChangeItem[0], rectItemContent));
        }

        for (int i = 0; i < listArtifactChangeItem.Count; i++)
        {
            listArtifactChangeItem[i].gameObject.SetActive(i < count);
        }
    }

    public void SetItem(int index, string grade, string level, string name, string stat, string status, bool interactable)
    {
        listArtifactChangeItem[index].Set(grade, level, name, stat, status, interactable);
    }

    private void AddItem(ArtifactChangeItem artifactChangeItem)
    {
        artifactChangeItem.OnClickItemEvent += OnClickItem;
        listArtifactChangeItem.Add(artifactChangeItem);
    }

    private void OnClickItem(ArtifactChangeItem artifactChangeItem)
    {
        OnClickArtifactEvent.Invoke(listArtifactChangeItem.IndexOf(artifactChangeItem));
    }

    private void OnClickUnequip()
    {
        OnClickUnequipEvent.Invoke();
    }

    private void OnClickClose()
    {
        Close();
    }
}
