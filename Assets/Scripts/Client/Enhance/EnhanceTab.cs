using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EnhanceTab : LobbyTab
{
    private const string EmptySlotName = "빈 슬롯";
    private const string EmptyValue = "-";
    private const int SwordStatRowCount = 3;
    private const int ArtifactStatRowCount = 1;

    [ReadOnly] [SerializeField] private EnhanceTitle enhanceTitle;
    [ReadOnly] [SerializeField] private EnhanceEquipmentSelect enhanceEquipmentSelect;
    [ReadOnly] [SerializeField] private EnhanceEquipmentInfo enhanceEquipmentInfo;
    [ReadOnly] [SerializeField] private EnhanceEquipmentAction enhanceEquipmentAction;
    [ReadOnly] [SerializeField] private EnhanceStatsPanel enhanceStatsPanel;

    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

    private IEnhanceService enhanceService;

    private bool isSwordSelected;
    private int selectedArtifactSlot;
    private bool isStatsOpened;
    private bool isRequesting;

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
        enhanceEquipmentAction.OnClickEnhanceEvent += OnClickEnhance;
        enhanceEquipmentAction.OnClickSellEvent += OnClickSell;

        enhanceStatsPanel = GameUtil.Bind<EnhanceStatsPanel>(transform, "StatsPanel");
    }

    private void Start()
    {
        enhanceService = new EnhanceLocalService(new System.Random());

        RefreshCombatStats();
        RefreshEquipmentNames();
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

        if (enhanceEquipmentAction != null)
        {
            enhanceEquipmentAction.OnClickEnhanceEvent -= OnClickEnhance;
            enhanceEquipmentAction.OnClickSellEvent -= OnClickSell;
        }
    }

    private void RefreshCombatStats()
    {
        CombatStat stat = StatCalculator.CalculatePlayer(GetSwordData(User.Sword.Level), GetEquippedArtifactLevels());
        long combatPower = CombatPowerCalculator.Calculate(stat, GetConfigValue("CRIT_MULTIPLIER"), GetConfigValue("BASE_BATTLE_TIME"));

        enhanceTitle.SetCombatPower(FormatInteger(combatPower));
        enhanceStatsPanel.SetStats(
            FormatInteger(stat.attack),
            FormatInteger(stat.maxHp),
            FormatAttackSpeed(stat.attackSpeed),
            FormatPercentage(stat.critRate),
            FormatPercentage(stat.lifesteal));
    }

    private IEnumerable<(ArtifactData data, int level)> GetEquippedArtifactLevels()
    {
        return User.EquippedArtifacts.Select(x => (GetArtifactData(x.ArtifactCode), x.Level));
    }

    private double GetConfigValue(string key)
    {
        return double.Parse(GameDB.config.First(x => x.key == key).value, CultureInfo.InvariantCulture);
    }

    private void RefreshEquipmentNames()
    {
        enhanceEquipmentSelect.SetSwordName($"검 +{User.Sword.Level.ToString()}");
        for (int slotIndex = 0; slotIndex < EnhanceEquipmentSelect.ArtifactSlotCount; slotIndex++)
        {
            enhanceEquipmentSelect.SetArtifactName(slotIndex, GetEquippedArtifactName(slotIndex));
        }
    }

    private string GetEquippedArtifactName(int slotIndex)
    {
        UserArtifact artifact = GetEquippedArtifact(slotIndex);
        if (artifact == null)
        {
            return EmptySlotName;
        }

        return GetArtifactData(artifact.ArtifactCode).name;
    }

    private void SelectSword()
    {
        isSwordSelected = true;
        enhanceEquipmentSelect.SetSelectedSword();
        enhanceEquipmentInfo.ShowSword();
        enhanceEquipmentAction.ShowSword();
        RefreshSwordInfo();
        RefreshSwordAction();
    }

    private void RefreshSwordInfo()
    {
        SwordData current = GetSwordData(User.Sword.Level);
        SwordData next = GetSwordData(User.Sword.Level + 1);

        enhanceEquipmentInfo.SetEquipment($"+{current.level.ToString()}", current.name);
        enhanceEquipmentInfo.ShowStatRows(SwordStatRowCount);
        SetStatRow(0, "공격력", current.attackPower, next?.attackPower, FormatInteger);
        SetStatRow(1, "공격 속도", current.attackSpeed, next?.attackSpeed, FormatAttackSpeed);
        SetStatRow(2, "최대 체력", current.maxHp, next?.maxHp, FormatInteger);
    }

    private void SetStatRow(int index, string name, double current, double? next, Func<double, string> format)
    {
        string nextText = next.HasValue
            ? format(next.Value)
            : EmptyValue;

        enhanceEquipmentInfo.SetStatRow(index, name, format(current), nextText);
    }

    private void RefreshSwordAction()
    {
        SwordData current = GetSwordData(User.Sword.Level);

        string percentage = current.successRate.HasValue
            ? FormatPercentage(current.successRate.Value)
            : EmptyValue;
        string cost = current.enhanceCost.HasValue
            ? FormatCost(current.enhanceCost.Value)
            : EmptyValue;

        enhanceEquipmentAction.SetEnhancePercentage(percentage);
        enhanceEquipmentAction.SetEnhanceCost(cost);
        enhanceEquipmentAction.SetSellPrice($"+{FormatInteger(current.sellPrice)}");
    }

    private void RefreshArtifactInfo(int slotIndex)
    {
        UserArtifact artifact = GetEquippedArtifact(slotIndex);
        if (artifact == null)
        {
            enhanceEquipmentInfo.SetEquipment(string.Empty, EmptySlotName);
            enhanceEquipmentInfo.ShowEmptyGuide();
            return;
        }

        ArtifactData data = GetArtifactData(artifact.ArtifactCode);
        double? nextValue = null;
        if (GetArtifactEnhanceData(artifact) != null)
        {
            nextValue = StatCalculator.GetArtifactValue(data, artifact.Level + 1);
        }

        enhanceEquipmentInfo.SetEquipment($"Lv. {artifact.Level.ToString()}", data.name);
        enhanceEquipmentInfo.ShowStatRows(ArtifactStatRowCount);
        SetStatRow(0, GetStatName(data.statType), StatCalculator.GetArtifactValue(data, artifact.Level), nextValue, GetStatFormat(data.statType));
    }

    private void RefreshArtifactAction(int slotIndex)
    {
        UserArtifact artifact = GetEquippedArtifact(slotIndex);
        ArtifactEnhanceData enhance = artifact != null
            ? GetArtifactEnhanceData(artifact)
            : null;

        if (enhance == null)
        {
            enhanceEquipmentAction.SetEnhanceCost(EmptyValue);
            enhanceEquipmentAction.SetMaterial(EmptyValue, 0f);
            return;
        }

        enhanceEquipmentAction.SetEnhanceCost(FormatCost(enhance.gold));
        enhanceEquipmentAction.SetMaterial($"{artifact.MaterialCount.ToString()}/{enhance.materialCount.ToString()}",
            Mathf.Clamp01((float)artifact.MaterialCount / enhance.materialCount));
    }

    private SwordData GetSwordData(int level)
    {
        return GameDB.swords.FirstOrDefault(x => x.level == level);
    }

    private ArtifactData GetArtifactData(string artifactCode)
    {
        return GameDB.artifacts.First(x => x.code == artifactCode);
    }

    private UserArtifact GetEquippedArtifact(int slotIndex)
    {
        return User.GetEquippedArtifact(slotIndex + 1);
    }

    private ArtifactEnhanceData GetArtifactEnhanceData(UserArtifact artifact)
    {
        ArtifactGrade grade = GetArtifactData(artifact.ArtifactCode).grade;
        return GameDB.artifactEnhance.FirstOrDefault(x => x.grade == grade && x.level == artifact.Level);
    }

    private static string GetStatName(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackSpeed:
                return "공격 속도";
            case StatType.MaxHp:
                return "최대 체력";
            case StatType.CritRate:
                return "치명타 확률";
            case StatType.Lifesteal:
                return "생명력 흡수";
        }
        return string.Empty;
    }

    private static Func<double, string> GetStatFormat(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackSpeed:
                return FormatAttackSpeed;
            case StatType.MaxHp:
                return FormatInteger;
            case StatType.CritRate:
            case StatType.Lifesteal:
                return FormatPercentage;
        }
        return FormatInteger;
    }

    private static string FormatInteger(double value)
    {
        return Math.Floor(value).ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatCost(long cost)
    {
        return $"-{FormatInteger(cost)}";
    }

    private static string FormatPercentage(double rate)
    {
        return $"{(rate * 100).ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    private static string FormatAttackSpeed(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private void SelectArtifact(int slotIndex)
    {
        isSwordSelected = false;
        selectedArtifactSlot = slotIndex;
        enhanceEquipmentSelect.SetSelectedArtifact(slotIndex);
        enhanceEquipmentInfo.ShowArtifact();
        enhanceEquipmentAction.ShowArtifact();
        RefreshArtifactInfo(slotIndex);
        RefreshArtifactAction(slotIndex);
    }

    private void SetStatsOpened(bool opened)
    {
        isStatsOpened = opened;
        enhanceStatsPanel.SetOpened(opened);
        enhanceTitle.SetStatsOpened(opened);
    }

    private async Task EnhanceSwordAsync()
    {
        await enhanceService.EnhanceSwordAsync(User.Sword.Level);
    }

    private async Task EnhanceArtifactAsync()
    {
        UserArtifact artifact = GetEquippedArtifact(selectedArtifactSlot);
        if (artifact == null)
        {
            return;
        }

        await enhanceService.EnhanceArtifactAsync(artifact.ArtifactCode);
    }

    private async Task SellSwordAsync()
    {
        await enhanceService.SellSwordAsync(User.Sword.Level);
    }

    private async Task RequestAsync(Func<Task> request)
    {
        if (isRequesting)
        {
            return;
        }

        isRequesting = true;
        try
        {
            await request();
            RefreshAfterRequest();
        }
        catch (EnhanceRequestException e)
        {
            Debug.LogWarning($"[Enhance] {e.Code}: {e.Message}");
        }
        finally
        {
            isRequesting = false;
        }
    }

    private void RefreshAfterRequest()
    {
        RefreshCombatStats();
        RefreshEquipmentNames();

        if (isSwordSelected)
        {
            SelectSword();
            return;
        }

        SelectArtifact(selectedArtifactSlot);
    }

    private async void OnClickEnhance()
    {
        if (isSwordSelected)
        {
            await RequestAsync(EnhanceSwordAsync);
            return;
        }

        await RequestAsync(EnhanceArtifactAsync);
    }

    private async void OnClickSell()
    {
        await RequestAsync(SellSwordAsync);
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
