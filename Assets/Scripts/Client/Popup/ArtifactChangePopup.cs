using System;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactChangePopup : BasePopup
{
    // 목록 한 줄에 표시할 내용
    public readonly struct ItemInfo
    {
        public readonly string grade;
        public readonly Color gradeColor;
        public readonly string level;
        public readonly string name;
        public readonly Sprite image;
        public readonly Sprite gradeFrame;
        public readonly Sprite statIcon;
        public readonly string stat;
        public readonly string status;
        public readonly bool interactable;

        public ItemInfo(string grade, Color gradeColor, string level, string name, Sprite image, Sprite gradeFrame, Sprite statIcon, string stat,
            string status, bool interactable)
        {
            this.grade = grade;
            this.gradeColor = gradeColor;
            this.level = level;
            this.name = name;
            this.image = image;
            this.gradeFrame = gradeFrame;
            this.statIcon = statIcon;
            this.stat = stat;
            this.status = status;
            this.interactable = interactable;
        }
    }

    [ReadOnly] [SerializeField] private ButtonUI btnClose;
    [ReadOnly] [SerializeField] private ButtonUI btnUnequip;
    [ReadOnly] [SerializeField] private RectTransform rectItemContent;
    [ReadOnly] [SerializeField] private List<ArtifactChangeItem> listArtifactChangeItem = new List<ArtifactChangeItem>();

    private IReadOnlyList<ItemInfo> items = new List<ItemInfo>();
    private bool canUnequip;
    private Action<int> onSelectArtifact; // 고른 항목의 순서를 넘긴다
    private Action onUnequip;

    protected override void Awake()
    {
        base.Awake();

        btnClose = GameUtil.Bind<ButtonUI>(transform, "Panel/Header/CloseButton");
        btnUnequip = GameUtil.Bind<ButtonUI>(transform, "Panel/Footer/UnequipButton");
        rectItemContent = GameUtil.Bind<RectTransform>(transform, "Panel/ArtifactList/Viewport/Content");
        AddItem(GameUtil.Bind<ArtifactChangeItem>(rectItemContent, "Item"));

        btnClose.OnClickEvent += OnClickClose;
        btnUnequip.OnClickEvent += OnClickUnequip;
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

    public void Setup(IReadOnlyList<ItemInfo> items, bool canUnequip, Action<int> onSelectArtifact, Action onUnequip)
    {
        this.items = items;
        this.canUnequip = canUnequip;
        this.onSelectArtifact = onSelectArtifact;
        this.onUnequip = onUnequip;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        ShowItems(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            ItemInfo item = items[i];
            listArtifactChangeItem[i].Set(item.grade, item.gradeColor, item.level, item.name, item.image, item.gradeFrame, item.statIcon, item.stat, item.status,
                item.interactable);
        }

        btnUnequip.gameObject.SetActive(canUnequip);
    }

    private void ShowItems(int count)
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

    private void AddItem(ArtifactChangeItem artifactChangeItem)
    {
        artifactChangeItem.OnClickItemEvent += OnClickItem;
        listArtifactChangeItem.Add(artifactChangeItem);
    }

    private void OnClickItem(ArtifactChangeItem artifactChangeItem)
    {
        onSelectArtifact?.Invoke(listArtifactChangeItem.IndexOf(artifactChangeItem));
    }

    private void OnClickUnequip()
    {
        onUnequip?.Invoke();
    }

    private void OnClickClose()
    {
        Close();
    }
}
