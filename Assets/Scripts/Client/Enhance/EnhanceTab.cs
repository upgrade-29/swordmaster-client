using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    private GameDB GameDB => GameSession.Instance.GameDB;
    private User User => GameSession.Instance.User;
    private ArtifactChangePopup ArtifactChangePopup => Popup.Instance.Get<ArtifactChangePopup>();
    private ConfirmPopup ConfirmPopup => Popup.Instance.Get<ConfirmPopup>();

    private bool isSwordSelected;
    private int selectedArtifactSlot;
    private bool isStatsOpened;
    private bool isRequesting;
    // 결과를 알 수 없어 다시 보내기를 기다리는 요청. 같은 요청 정보(같은 requestId)를 그대로 다시 보낸다
    private Action retryRequest;

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
        enhanceEquipmentAction.OnClickChangeEvent += OnClickChange;

        enhanceStatsPanel = GameUtil.Bind<EnhanceStatsPanel>(transform, "StatsPanel");
    }

    private void Start()
    {
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
            enhanceEquipmentAction.OnClickChangeEvent -= OnClickChange;
        }
    }

    private void RefreshCombatStats()
    {
        CombatStat stat = StatCalculator.CalculatePlayer(GameDB, User);
        long combatPower = CombatPowerCalculator.Calculate(stat, GameDB);

        enhanceTitle.SetCombatPower(FormatInteger(combatPower));
        enhanceStatsPanel.SetStats(
            FormatInteger(stat.attack),
            FormatInteger(stat.maxHp),
            FormatAttackSpeed(stat.attackSpeed),
            FormatPercentage(stat.critRate),
            FormatPercentage(stat.lifesteal));
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

    private static string GetGradeName(ArtifactGrade grade)
    {
        string gradeName = string.Empty;
        switch (grade)
        {
            case ArtifactGrade.Common:
                gradeName = "일반";
                break;
            case ArtifactGrade.Rare:
                gradeName = "희귀";
                break;
            case ArtifactGrade.Epic:
                gradeName = "영웅";
                break;
            case ArtifactGrade.Legendary:
                gradeName = "전설";
                break;
        }

        return gradeName;
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
        enhanceEquipmentAction.ShowArtifact(GetEquippedArtifact(slotIndex) != null);
        RefreshArtifactInfo(slotIndex);
        RefreshArtifactAction(slotIndex);
    }

    private void OpenArtifactChangePopup()
    {
        var items = User.Artifacts.Select(CreateArtifactChangeItem).ToList();
        bool canUnequip = GetEquippedArtifact(selectedArtifactSlot) != null;

        ArtifactChangePopup.Setup(items, canUnequip, OnClickChangeArtifact, OnClickUnequip);
        ArtifactChangePopup.Open();
    }

    private ArtifactChangePopup.ItemInfo CreateArtifactChangeItem(UserArtifact artifact)
    {
        ArtifactData data = GetArtifactData(artifact.ArtifactCode);
        string value = GetStatFormat(data.statType)(StatCalculator.GetArtifactValue(data, artifact.Level));

        return new ArtifactChangePopup.ItemInfo(
            GetGradeName(data.grade),
            $"Lv. {artifact.Level.ToString()}",
            data.name,
            $"{GetStatName(data.statType)} {value}",
            GetArtifactChangeStatus(artifact),
            artifact.IsEquipped == false);
    }

    private string GetArtifactChangeStatus(UserArtifact artifact)
    {
        if (artifact.IsEquipped == false)
        {
            return "장착하기";
        }

        if (artifact.EquippedSlot == selectedArtifactSlot + 1)
        {
            return "현재 장착";
        }

        return $"슬롯 {artifact.EquippedSlot.Value.ToString()} 장착 중";
    }

    private void SetStatsOpened(bool opened)
    {
        isStatsOpened = opened;
        enhanceStatsPanel.SetOpened(opened);
        enhanceTitle.SetStatsOpened(opened);
    }

    // 성공하면 응답을 User에 반영하고 화면을 갱신한다. 실패는 HttpRequest가 남기는 로그로 확인한다
    private void SendRequest<T>(HttpRequestInfo<T> requestInfo, Action<T> applyResponse) where T : class
    {
        isRequesting = true;
        HttpRequest.Request(requestInfo, (error, response) => OnResponse(requestInfo, applyResponse, error, response));
    }

    private void ApplySwordEnhance(EnhanceApi.SwordEnhanceResponse response)
    {
        User.Sword.SetLevel(response.swordResponse.level);
        SyncCurrencies(response.currenciesResponse);
    }

    private void ApplySwordSell(EnhanceApi.SwordSellResponse response)
    {
        User.Sword.SetLevel(response.swordResponse.level);
        SyncCurrencies(response.currenciesResponse);
    }

    private void ApplyArtifactEnhance(EnhanceApi.ArtifactEnhanceResponse response)
    {
        foreach (ArtifactResponse artifact in response.artifactResponses)
        {
            User.GetArtifact(artifact.code)?.SyncFrom(artifact.ToUserArtifact());
        }

        SyncCurrencies(response.currenciesResponse);
    }

    // 응답에는 장착한 아티팩트만 들어 있으므로, 응답에 없는 아티팩트는 장착 해제된 것이다
    private void ApplyEquippedArtifacts(List<ArtifactResponse> equippedArtifacts)
    {
        foreach (UserArtifact artifact in User.Artifacts)
        {
            ArtifactResponse equipped = equippedArtifacts.FirstOrDefault(x => x.code == artifact.ArtifactCode);
            artifact.SetEquippedSlot(equipped?.equipSlot);
        }
    }

    private void SyncCurrencies(UserCurrencies currencies)
    {
        User.Currencies.SyncFrom(currencies);
        GameSession.Instance.NotifyChangeCurrencies();
    }

    // 재전송을 기다리는 요청이 있으면 재전송 팝업을 다시 열고 true를 돌려준다
    private bool TryReopenRetryPopup()
    {
        if (retryRequest == null)
        {
            return false;
        }

        ShowRetryPopup();
        return true;
    }

    private void ShowRetryPopup()
    {
        ConfirmPopup.Setup("요청 결과 확인 필요", "요청 결과를 확인하지 못했습니다. 다시 시도하시겠습니까?",
            OnConfirmRetry, OnCancelRetry);
        ConfirmPopup.Open();
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

    private void OnResponse<T>(HttpRequestInfo<T> requestInfo, Action<T> applyResponse, HttpError error, T response) where T : class
    {
        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (error == null)
        {
            isRequesting = false;
            applyResponse(response);
            RefreshAfterRequest();
            return;
        }

        if (HttpErrorUtil.IsResultUnknown(error))
        {
            retryRequest = () => SendRequest(requestInfo, applyResponse);
            ShowRetryPopup();
            return;
        }

        isRequesting = false;
    }

    private void OnClickEnhance()
    {
        if (TryReopenRetryPopup() || isRequesting)
        {
            return;
        }

        if (isSwordSelected)
        {
            SendRequest(EnhanceApi.EnhanceSword(Guid.NewGuid(), User.Sword.Level), ApplySwordEnhance);
            return;
        }

        UserArtifact artifact = GetEquippedArtifact(selectedArtifactSlot);
        if (artifact == null)
        {
            OpenArtifactChangePopup();
            return;
        }

        SendRequest(EnhanceApi.EnhanceArtifact(Guid.NewGuid(), artifact.ArtifactCode), ApplyArtifactEnhance);
    }

    private void OnClickSell()
    {
        if (TryReopenRetryPopup() || isRequesting)
        {
            return;
        }

        SendRequest(EnhanceApi.SellSword(Guid.NewGuid(), User.Sword.Level), ApplySwordSell);
    }

    private void OnClickChange()
    {
        OpenArtifactChangePopup();
    }

    private void OnClickChangeArtifact(int index)
    {
        string artifactCode = User.Artifacts[index].ArtifactCode;
        ArtifactChangePopup.Close();

        if (TryReopenRetryPopup() || isRequesting)
        {
            return;
        }

        SendRequest(EnhanceApi.EquipArtifact(artifactCode, selectedArtifactSlot + 1), ApplyEquippedArtifacts);
    }

    private void OnClickUnequip()
    {
        ArtifactChangePopup.Close();

        if (TryReopenRetryPopup() || isRequesting)
        {
            return;
        }

        SendRequest(EnhanceApi.UnequipArtifact(selectedArtifactSlot + 1), ApplyEquippedArtifacts);
    }

    private void OnConfirmRetry()
    {
        ConfirmPopup.Close();

        Action retry = retryRequest;
        retryRequest = null;
        retry();
    }

    // 요청은 그대로 두고, 강화 화면의 버튼을 누르면 재전송 팝업을 다시 연다
    private void OnCancelRetry()
    {
        ConfirmPopup.Close();
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
