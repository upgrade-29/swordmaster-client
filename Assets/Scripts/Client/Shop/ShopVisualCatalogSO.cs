using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 상점 아이콘의 iconCode/CurrencyType/RewardType ↔ Sprite 매핑. 가격, 보상량 등 서버 밸런스 값은 포함하지 않는다.
[CreateAssetMenu(fileName = "ShopVisualCatalog", menuName = "Swordmaster/Shop/Shop Visual Catalog")]
public class ShopVisualCatalogSO : ScriptableObject
{
    [Serializable]
    private class ProductIconEntry
    {
        [SerializeField] private string iconCode;
        [SerializeField] private Sprite icon;

        public string IconCode => iconCode;
        public Sprite Icon => icon;
    }

    [Serializable]
    private class CurrencyIconEntry
    {
        [SerializeField] private CurrencyType currencyType;
        [SerializeField] private Sprite icon;

        public CurrencyType CurrencyType => currencyType;
        public Sprite Icon => icon;
    }

    [Serializable]
    private class RewardTypeIconEntry
    {
        [SerializeField] private RewardType rewardType;
        [SerializeField] private Sprite icon;

        public RewardType RewardType => rewardType;
        public Sprite Icon => icon;
    }

    [Serializable]
    private class CategoryLabelEntry
    {
        [SerializeField] private ShopCategory category;
        [TextArea] [SerializeField] private string label;

        public ShopCategory Category => category;
        public string Label => label;
    }

    [Header("상품/카테고리 아이콘")]
    [SerializeField] private List<ProductIconEntry> productIcons = new List<ProductIconEntry>();

    [Header("재화 아이콘")]
    [SerializeField] private List<CurrencyIconEntry> currencyIcons = new List<CurrencyIconEntry>();

    [Header("보상 타입 아이콘")]
    [SerializeField] private List<RewardTypeIconEntry> rewardTypeIcons = new List<RewardTypeIconEntry>();

    [Header("카테고리 섹션 라벨")]
    [SerializeField] private List<CategoryLabelEntry> categoryLabels = new List<CategoryLabelEntry>();

    [Space(10)]
    [Header("매핑 누락 시 대체 아이콘")]
    [SerializeField] private Sprite fallbackIcon;

    // productCode/rewardCode의 iconCode에 대응하는 Sprite. 매핑이 없으면 fallbackIcon을 반환한다.
    public Sprite GetProductIcon(string iconCode)
    {
        ProductIconEntry entry = productIcons.FirstOrDefault(e => e.IconCode == iconCode);
        return entry != null ? entry.Icon : fallbackIcon;
    }

    // 가격 표시에 사용하는 재화 아이콘. 매핑이 없으면 fallbackIcon을 반환한다.
    public Sprite GetCurrencyIcon(CurrencyType currencyType)
    {
        CurrencyIconEntry entry = currencyIcons.FirstOrDefault(e => e.CurrencyType == currencyType);
        return entry != null ? entry.Icon : fallbackIcon;
    }

    // 대표 보상 표시에 사용하는 보상 타입 아이콘. 매핑이 없으면 fallbackIcon을 반환한다.
    public Sprite GetRewardTypeIcon(RewardType rewardType)
    {
        RewardTypeIconEntry entry = rewardTypeIcons.FirstOrDefault(e => e.RewardType == rewardType);
        return entry != null ? entry.Icon : fallbackIcon;
    }

    // 섹션 헤더에 표시할 카테고리 라벨. 매핑이 없으면 enum 이름을 그대로 반환한다 (서버 밸런스 값이 아닌 표시 전용 텍스트).
    public string GetCategoryLabel(ShopCategory category)
    {
        CategoryLabelEntry entry = categoryLabels.FirstOrDefault(e => e.Category == category);
        return entry != null ? entry.Label : category.ToString();
    }
}
