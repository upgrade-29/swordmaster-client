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

    [ReadOnly] [SerializeField] private EnhanceTitle enhanceTitle;
    [ReadOnly] [SerializeField] private EnhanceEquipmentSelect enhanceEquipmentSelect;
    [ReadOnly] [SerializeField] private EnhanceEquipmentInfo enhanceEquipmentInfo;
    [ReadOnly] [SerializeField] private EnhanceEquipmentAction enhanceEquipmentAction;
    [ReadOnly] [SerializeField] private EnhanceStatsPanel enhanceStatsPanel;

    [SerializeField] private Sprite spriteAttack;
    [SerializeField] private Sprite spriteAttackSpeed;
    [SerializeField] private Sprite spriteMaxHp;
    [SerializeField] private Sprite spriteCritRate;
    [SerializeField] private Sprite spriteLifesteal;

    // 아티팩트 버튼과 교체 창 이미지 칸에 겹치는 등급 테두리
    [SerializeField] private Sprite spriteGradeFrameCommon;
    [SerializeField] private Sprite spriteGradeFrameRare;
    [SerializeField] private Sprite spriteGradeFrameEpic;
    [SerializeField] private Sprite spriteGradeFrameLegendary;

    private GameDB GameDB => GameSession.Instance.GameDB;
    private User User => GameSession.Instance.User;
    private ArtifactChangePopup ArtifactChangePopup => Popup.Instance.Get<ArtifactChangePopup>();
    private DialogPopup DialogPopup => Popup.Instance.Get<DialogPopup>();
    private EnhancePopup EnhancePopup => Popup.Instance.Get<EnhancePopup>();

    private bool isSwordSelected;
    private int selectedArtifactSlot;
    private bool isStatsOpened;
    private bool isRequesting;
    // 결과를 알 수 없어 다시 보내기를 기다리는 요청. 같은 요청 정보(같은 requestId)를 그대로 다시 보낸다
    private Action retryRequest;

    private long combatPower;
    private Color? statChangeColor; // 요청 결과로 갱신하는 동안만 능력치 줄 강조 색을 넣는다

    protected override void Awake()
    {
        base.Awake();

        enhanceTitle = GameUtil.Bind<EnhanceTitle>(transform, "Viewport/Title");
        enhanceTitle.OnClickCombatPowerEvent += OnClickCombatPower;

        enhanceEquipmentSelect = GameUtil.Bind<EnhanceEquipmentSelect>(transform, "Viewport/ScrollView/Content/EquipmentSelect");
        enhanceEquipmentSelect.OnClickSwordButtonEvent += OnClickSwordButton;
        enhanceEquipmentSelect.OnClickArtifactButtonEvent += OnClickArtifactButton;

        enhanceEquipmentInfo = GameUtil.Bind<EnhanceEquipmentInfo>(transform, "Viewport/ScrollView/Content/EquipmentInfo");
        enhanceEquipmentAction = GameUtil.Bind<EnhanceEquipmentAction>(transform, "Viewport/EquipmentAction");
        enhanceEquipmentAction.OnClickEnhanceEvent += OnClickEnhance;
        enhanceEquipmentAction.OnClickSellEvent += OnClickSell;
        enhanceEquipmentAction.OnClickChangeEvent += OnClickChange;

        enhanceStatsPanel = GameUtil.Bind<EnhanceStatsPanel>(transform, "Viewport/StatsPanel");
        enhanceStatsPanel.OnClickOutsideEvent += OnClickStatsOutside;
    }

    private void Start()
    {
        RefreshCombatStats();
        RefreshEquipmentButtons();
        SelectSword();
        SetStatsOpened(false, true);
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

        if (enhanceStatsPanel != null)
        {
            enhanceStatsPanel.OnClickOutsideEvent -= OnClickStatsOutside;
        }

        // 강화 연출 중에 로비를 떠나도 상단 공지가 계속 막혀 있지 않게 푼다
        if (ChatClient.IsInitialize())
        {
            ChatClient.Instance.ReleaseTopNotices();
        }
    }

    // animate면 전투력이 지금 숫자에서 새 숫자까지 올라가거나 내려간다
    private void RefreshCombatStats(bool animate = false)
    {
        CombatStat stat = StatCalculator.CalculatePlayer(GameDB, User);
        combatPower = CombatPowerCalculator.Calculate(stat, GameDB);

        enhanceTitle.SetCombatPower(combatPower, animate);
        enhanceStatsPanel.SetStats(
            FormatInteger(stat.attack),
            FormatInteger(stat.maxHp),
            FormatAttackSpeed(stat.attackSpeed),
            FormatPercentage(stat.critRate),
            FormatPercentage(stat.lifesteal));
    }

    private void RefreshEquipmentButtons()
    {
        enhanceEquipmentSelect.SetSword($"검 {SwordLevelColor.Format(User.Sword.Level)}", GetSwordData(User.Sword.Level).code);
        for (int slotIndex = 0; slotIndex < GameConstants.Rules.ArtifactSlotCount; slotIndex++)
        {
            // 빈 슬롯이면 이미지 주소와 등급 테두리 없이 넘겨서 숨긴다
            string imageCode = GetEquippedArtifact(slotIndex)?.ArtifactCode;
            Sprite gradeFrame = imageCode == null ? null : GetGradeFrame(GetArtifactData(imageCode).grade);
            enhanceEquipmentSelect.SetArtifact(slotIndex, GetEquippedArtifactName(slotIndex), imageCode, gradeFrame);
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

        enhanceEquipmentInfo.SetSwordImage(current.code);
        enhanceEquipmentInfo.SetEquipment(SwordLevelColor.Format(current.level), current.name);
        enhanceEquipmentInfo.ShowStatRows(SwordStatRowCount);
        SetStatRow(0, spriteAttack, "공격력", current.attackPower, next?.attackPower, FormatInteger);
        SetStatRow(1, spriteAttackSpeed, "공격 속도", current.attackSpeed, next?.attackSpeed, FormatAttackSpeed);
        SetStatRow(2, spriteMaxHp, "최대 체력", current.maxHp, next?.maxHp, FormatInteger);
    }

    private void SetStatRow(int index, Sprite icon, string name, double current, double? next, Func<double, string> format)
    {
        string nextText = next.HasValue
            ? format(next.Value)
            : EmptyValue;

        enhanceEquipmentInfo.SetStatRow(index, icon, name, format(current), nextText, statChangeColor);
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
            enhanceEquipmentInfo.SetArtifactImage(null);
            enhanceEquipmentInfo.SetEquipment(string.Empty, EmptySlotName);
            enhanceEquipmentInfo.ShowEmptyGuide();
            return;
        }

        ArtifactData data = GetArtifactData(artifact.ArtifactCode);
        IReadOnlyList<ArtifactStatData> stats = GameDB.GetArtifactStats(artifact.ArtifactCode);
        bool canEnhance = GetArtifactEnhanceData(artifact) != null;

        enhanceEquipmentInfo.SetArtifactImage(artifact.ArtifactCode);
        enhanceEquipmentInfo.SetEquipment($"Lv. {artifact.Level.ToString()}", data.name);
        enhanceEquipmentInfo.ShowStatRows(stats.Count);
        for (int i = 0; i < stats.Count; i++)
        {
            ArtifactStatData stat = stats[i];
            double? nextValue = canEnhance
                ? StatCalculator.GetArtifactValue(stat, artifact.Level + 1)
                : (double?)null;
            SetStatRow(i, GetStatIcon(stat.statType), ArtifactText.GetStatName(stat.statType), StatCalculator.GetArtifactValue(stat, artifact.Level), nextValue,
                GetStatFormat(stat.statType));
        }
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

    private Sprite GetGradeFrame(ArtifactGrade grade)
    {
        Sprite frame = null;
        switch (grade)
        {
            case ArtifactGrade.Common:
                frame = spriteGradeFrameCommon;
                break;
            case ArtifactGrade.Rare:
                frame = spriteGradeFrameRare;
                break;
            case ArtifactGrade.Epic:
                frame = spriteGradeFrameEpic;
                break;
            case ArtifactGrade.Legendary:
                frame = spriteGradeFrameLegendary;
                break;
        }

        return frame;
    }

    private Sprite GetStatIcon(StatType statType)
    {
        Sprite icon = null;
        switch (statType)
        {
            case StatType.AttackSpeed:
                icon = spriteAttackSpeed;
                break;
            case StatType.MaxHp:
                icon = spriteMaxHp;
                break;
            case StatType.CritRate:
                icon = spriteCritRate;
                break;
            case StatType.Lifesteal:
                icon = spriteLifesteal;
                break;
        }

        return icon;
    }

    private static Func<double, string> GetStatFormat(StatType statType)
    {
        return value => ArtifactText.FormatStat(statType, value);
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
        ArtifactStatData mainStat = GameDB.GetArtifactStats(artifact.ArtifactCode)[0];
        string value = GetStatFormat(mainStat.statType)(StatCalculator.GetArtifactValue(mainStat, artifact.Level));

        return new ArtifactChangePopup.ItemInfo(
            ArtifactText.GetGradeName(data.grade),
            ArtifactText.GetGradeColor(data.grade),
            $"Lv. {artifact.Level.ToString()}",
            data.name,
            ResourceManager.Instance.GetSprite(artifact.ArtifactCode),
            GetGradeFrame(data.grade),
            GetStatIcon(mainStat.statType),
            $"{ArtifactText.GetStatName(mainStat.statType)} {value}",
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

    private void SetStatsOpened(bool opened, bool immediately = false)
    {
        isStatsOpened = opened;
        enhanceStatsPanel.SetOpened(opened, immediately);
        enhanceTitle.SetStatsOpened(opened);
    }

    // 성공하면 applyResponse가 응답을 User에 반영하고 화면 갱신 시점도 정한다. 실패는 HttpRequest가 남기는 로그로 확인한다
    // holdsTopNotices: 응답보다 먼저 오는 강화 결과 공지가 연출 전에 보이지 않도록, 강화 팝업의 확인까지 상단 공지를 미룬다
    private void SendRequest<T>(HttpRequestInfo<T> requestInfo, Action<T> applyResponse, bool holdsTopNotices = false) where T : class
    {
        isRequesting = true;
        if (holdsTopNotices)
        {
            ChatClient.Instance.HoldTopNotices();
        }

        HttpRequest.Request(requestInfo, (error, response) => OnResponse(requestInfo, applyResponse, holdsTopNotices, error, response));
    }

    // 연출보다 먼저 강화 결과가 탭에 드러나지 않도록, 탭 갱신은 강화 팝업의 확인을 누른 뒤에 한다
    private void ApplySwordEnhance(EnhanceApi.SwordEnhanceResponse response)
    {
        int levelBefore = User.Sword.Level;
        User.Sword.SetLevel(response.swordResponse.level);
        SyncCurrencies(response.currenciesResponse);

        bool isSuccess = response.result == EnhanceApi.EnhanceResultType.Success;
        EnhancePopup.Setup(CreateSwordResult(isSuccess, levelBefore, User.Sword.Level), OnConfirmSwordEnhance);
        EnhancePopup.Open();
    }

    private void ApplySwordSell(EnhanceApi.SwordSellResponse response)
    {
        User.Sword.SetLevel(response.swordResponse.level);
        SyncCurrencies(response.currenciesResponse);
        SoundManager.Instance.PlaySfx(GameConstants.Sounds.EnhanceSell);
        RefreshAfterRequest(true);
    }

    private void ApplyArtifactEnhance(EnhanceApi.ArtifactEnhanceResponse response)
    {
        ArtifactResponse enhanced = response.artifactResponses.FirstOrDefault();
        int levelBefore = enhanced != null ? User.GetArtifact(enhanced.code).Level : 0;

        foreach (ArtifactResponse artifact in response.artifactResponses)
        {
            User.GetArtifact(artifact.code)?.SyncFrom(artifact.ToUserArtifact());
        }

        SyncCurrencies(response.currenciesResponse);

        // 검 강화와 같이, 연출보다 먼저 강화 결과가 탭에 드러나지 않도록 탭 갱신은 강화 팝업의 확인을 누른 뒤에 한다
        if (enhanced == null)
        {
            RefreshAfterRequest(true);
            return;
        }

        EnhancePopup.Setup(CreateArtifactResult(User.GetArtifact(enhanced.code), levelBefore), OnConfirmArtifactEnhance);
        EnhancePopup.Open();
    }

    private EnhancePopup.ResultInfo CreateSwordResult(bool isSuccess, int levelBefore, int levelAfter)
    {
        SwordData before = GetSwordData(levelBefore);
        SwordData after = GetSwordData(levelAfter);
        var stats = new List<EnhancePopup.StatInfo>
        {
            new EnhancePopup.StatInfo(spriteAttack, "공격력", FormatInteger(before.attackPower), FormatInteger(after.attackPower)),
            new EnhancePopup.StatInfo(spriteAttackSpeed, "공격 속도", FormatAttackSpeed(before.attackSpeed), FormatAttackSpeed(after.attackSpeed)),
            new EnhancePopup.StatInfo(spriteMaxHp, "최대 체력", FormatInteger(before.maxHp), FormatInteger(after.maxHp)),
        };
        string message = isSuccess ? string.Empty : "검이 파괴되었습니다";
        // 연출 동안은 원래 검을 보여 주고, 성공하면 결과가 드러날 때 새 검으로 바꾼다. 실패하면 파괴되는 원래 검을 그대로 두고 실패 연출이 어둡게 만든다
        string imageCode = isSuccess ? after.code : before.code;

        return new EnhancePopup.ResultInfo(isSuccess, SwordLevelColor.Format(levelBefore), SwordLevelColor.Format(levelAfter), after.name, message, stats,
            before.code, imageCode);
    }

    private EnhancePopup.ResultInfo CreateArtifactResult(UserArtifact artifact, int levelBefore)
    {
        ArtifactData data = GetArtifactData(artifact.ArtifactCode);
        var stats = GameDB.GetArtifactStats(artifact.ArtifactCode)
            .Select(x => new EnhancePopup.StatInfo(GetStatIcon(x.statType), ArtifactText.GetStatName(x.statType),
                GetStatFormat(x.statType)(StatCalculator.GetArtifactValue(x, levelBefore)),
                GetStatFormat(x.statType)(StatCalculator.GetArtifactValue(x, artifact.Level))))
            .ToList();

        return new EnhancePopup.ResultInfo(true, $"Lv. {levelBefore.ToString()}", $"Lv. {artifact.Level.ToString()}", data.name,
            string.Empty, stats, artifact.ArtifactCode, artifact.ArtifactCode);
    }

    // 응답에는 장착한 아티팩트만 들어 있으므로, 응답에 없는 아티팩트는 장착 해제된 것이다
    private void ApplyEquippedArtifacts(List<ArtifactResponse> equippedArtifacts)
    {
        foreach (UserArtifact artifact in User.Artifacts)
        {
            ArtifactResponse equipped = equippedArtifacts.FirstOrDefault(x => x.code == artifact.ArtifactCode);
            artifact.SetEquippedSlot(equipped?.equipSlot);
        }

        SoundManager.Instance.PlaySfx(GameConstants.Sounds.ArtifactEquip);
        RefreshAfterRequest(false);
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
        DialogPopup.Setup("요청 결과 확인 필요", "요청 결과를 확인하지 못했습니다. 다시 시도하시겠습니까?",
            new DialogPopup.DialogPopupButtonInfo("확인", OnConfirmRetry),
            new DialogPopup.DialogPopupButtonInfo("취소", OnCancelRetry));
        DialogPopup.Open();
    }

    // 요청 결과를 탭에 반영한다. 전투력은 연출과 함께 바꾸고, highlightStats면 같은 장비의 능력치가 바뀐 값을
    // 전투력이 오르면 초록, 내리면 빨강으로 강조한다. 장착/해제는 능력치 줄이 다른 장비로 바뀌므로 강조하지 않는다
    private void RefreshAfterRequest(bool highlightStats)
    {
        long combatPowerBefore = combatPower;
        RefreshCombatStats(true);
        RefreshEquipmentButtons();

        if (highlightStats)
        {
            statChangeColor = combatPower >= combatPowerBefore
                ? GameConstants.Colors.StatIncrease
                : GameConstants.Colors.StatDecrease;
        }

        if (isSwordSelected)
        {
            SelectSword();
        }
        else
        {
            SelectArtifact(selectedArtifactSlot);
        }

        statChangeColor = null;
    }

    private void OnResponse<T>(HttpRequestInfo<T> requestInfo, Action<T> applyResponse, bool holdsTopNotices, HttpError error, T response)
        where T : class
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
            return;
        }

        if (HttpErrorUtil.IsResultUnknown(error))
        {
            retryRequest = () => SendRequest(requestInfo, applyResponse, holdsTopNotices);
            ShowRetryPopup();
            return;
        }

        isRequesting = false;
        if (holdsTopNotices)
        {
            ChatClient.Instance.ReleaseTopNotices();
        }
    }

    private void OnClickEnhance()
    {
        if (TryReopenRetryPopup() || isRequesting)
        {
            return;
        }

        if (isSwordSelected)
        {
            SendRequest(EnhanceApi.EnhanceSword(Guid.NewGuid(), User.Sword.Level), ApplySwordEnhance, true);
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
        DialogPopup.Close();

        Action retry = retryRequest;
        retryRequest = null;
        retry();
    }

    // 요청은 그대로 두고, 강화 화면의 버튼을 누르면 재전송 팝업을 다시 연다
    private void OnCancelRetry()
    {
        DialogPopup.Close();
        ChatClient.Instance.ReleaseTopNotices();
    }

    private void OnConfirmSwordEnhance()
    {
        EnhancePopup.Close();
        RefreshAfterRequest(true);
        ChatClient.Instance.ReleaseTopNotices();
    }

    private void OnConfirmArtifactEnhance()
    {
        EnhancePopup.Close();
        RefreshAfterRequest(true);
    }

    private void OnClickCombatPower()
    {
        SetStatsOpened(isStatsOpened == false);
    }

    private void OnClickStatsOutside()
    {
        SetStatsOpened(false);
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
