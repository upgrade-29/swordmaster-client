using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

// 서버가 없기 때문에 임시 코드. User는 서버 DB에 있는 유저 역할이며 강화 결과가 이 객체에 반영된다
public class EnhanceLocalService : IEnhanceService
{
    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

    private readonly Random random;

    public EnhanceLocalService(Random random)
    {
        this.random = random;
    }

    public async Task<EnhanceSwordResult> EnhanceSwordAsync(int expectedLevel)
    {
        await Task.Yield();

        if (User.Sword.Level != expectedLevel)
        {
            throw new EnhanceRequestException(EnhanceRequestException.LevelMismatch, "검 강화 단계가 다릅니다.");
        }

        SwordData sword = GameDB.swords.First(x => x.level == User.Sword.Level);
        if (sword.successRate.HasValue == false || sword.enhanceCost.HasValue == false)
        {
            throw new EnhanceRequestException(EnhanceRequestException.SwordMaxLevel, "최대 강화 단계입니다.");
        }

        SpendGold(sword.enhanceCost.Value);

        bool isSuccess = random.NextDouble() < sword.successRate.Value;
        User.Sword.SetLevel(isSuccess ? sword.level + 1 : 0);
        TestLobbyDataLoader.Instance.NotifyChangeCurrencies();

        CombatStat stats = CalculateStats();
        var result = new EnhanceSwordResult(
            isSuccess ? EnhanceSwordOutcome.Success : EnhanceSwordOutcome.Destroyed,
            User.Sword, User.Currencies, stats, CalculateCombatPower(stats));
        return CopyAsResponse(result);
    }

    public async Task<EnhanceSwordSellResult> SellSwordAsync(int expectedLevel)
    {
        await Task.Yield();

        if (User.Sword.Level != expectedLevel)
        {
            throw new EnhanceRequestException(EnhanceRequestException.LevelMismatch, "검 강화 단계가 다릅니다.");
        }

        if (User.Sword.Level == 0)
        {
            throw new EnhanceRequestException(EnhanceRequestException.SwordNotSellable, "+0 검은 판매할 수 없습니다.");
        }

        long sellPrice = GameDB.swords.First(x => x.level == User.Sword.Level).sellPrice;
        User.Currencies.Add(CurrencyType.Gold, sellPrice);
        User.Sword.SetLevel(0);
        TestLobbyDataLoader.Instance.NotifyChangeCurrencies();

        CombatStat stats = CalculateStats();
        var rewards = new List<PurchaseRewardResult> { new PurchaseRewardResult(RewardType.Gold, null, sellPrice) };
        var result = new EnhanceSwordSellResult(User.Sword, rewards, User.Currencies, stats, CalculateCombatPower(stats));
        return CopyAsResponse(result);
    }

    public async Task<EnhanceArtifactResult> EnhanceArtifactAsync(string artifactCode)
    {
        await Task.Yield();

        UserArtifact artifact = User.GetArtifact(artifactCode);
        if (artifact == null)
        {
            throw new EnhanceRequestException(EnhanceRequestException.ArtifactNotOwned, "보유하지 않은 아티팩트입니다.");
        }

        ArtifactGrade grade = GameDB.artifacts.First(x => x.artifactCode == artifactCode).grade;
        ArtifactEnhanceData enhance = GameDB.artifactEnhance.FirstOrDefault(x => x.grade == grade && x.level == artifact.Level);
        if (enhance == null)
        {
            throw new EnhanceRequestException(EnhanceRequestException.ArtifactMaxLevel, "아티팩트가 최대 레벨입니다.");
        }

        if (artifact.MaterialCount < enhance.materialCount)
        {
            throw new EnhanceRequestException(EnhanceRequestException.NotEnoughMaterial, "강화 재료가 부족합니다.");
        }

        SpendGold(enhance.gold);
        artifact.LevelUp(enhance.materialCount);
        TestLobbyDataLoader.Instance.NotifyChangeCurrencies();

        CombatStat stats = CalculateStats();
        var result = new EnhanceArtifactResult(new List<UserArtifact> { artifact }, User.Currencies, stats, CalculateCombatPower(stats));
        return CopyAsResponse(result);
    }

    private void SpendGold(long cost)
    {
        if (User.Currencies.Gold < cost)
        {
            throw new EnhanceRequestException(EnhanceRequestException.NotEnoughGold, "골드가 부족합니다.");
        }

        User.Currencies.Add(CurrencyType.Gold, -cost);
    }

    private CombatStat CalculateStats()
    {
        SwordData sword = GameDB.swords.First(x => x.level == User.Sword.Level);
        var equippedArtifacts = User.EquippedArtifacts
            .Select(x => (GameDB.artifacts.First(y => y.artifactCode == x.ArtifactCode), x.Level));
        return StatCalculator.CalculatePlayer(sword, equippedArtifacts);
    }

    private long CalculateCombatPower(CombatStat stats)
    {
        return CombatPowerCalculator.Calculate(stats, GetConfigValue("CRIT_MULTIPLIER"), GetConfigValue("BASE_BATTLE_TIME"));
    }

    private double GetConfigValue(string key)
    {
        return double.Parse(GameDB.config.First(x => x.key == key).value, CultureInfo.InvariantCulture);
    }

    // 서버 응답처럼 JSON을 거쳐서 넘긴다. 서버 역할의 User와 객체를 공유하지 않게 된다
    private static T CopyAsResponse<T>(T result)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(result));
    }
}
