using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

// 서버가 없기 때문에 임시 코드. user는 서버 DB에 있는 유저 역할이며 강화 결과가 이 객체에 반영된다
public class EnhanceLocalService : IEnhanceService
{
    private readonly GameDB gameDB;
    private readonly User user;
    private readonly Random random;

    public EnhanceLocalService(GameDB gameDB, User user, Random random)
    {
        this.gameDB = gameDB;
        this.user = user;
        this.random = random;
    }

    public async Task<EnhanceSwordResult> EnhanceSwordAsync(int expectedLevel)
    {
        await Task.Yield();

        if (user.Sword.Level != expectedLevel)
        {
            throw new EnhanceRequestException(EnhanceRequestException.LevelMismatch, "검 강화 단계가 다릅니다.");
        }

        SwordData sword = gameDB.swords.First(data => data.level == user.Sword.Level);
        if (sword.successRate.HasValue == false || sword.enhanceCost.HasValue == false)
        {
            throw new EnhanceRequestException(EnhanceRequestException.SwordMaxLevel, "최대 강화 단계입니다.");
        }

        SpendGold(sword.enhanceCost.Value);

        bool isSuccess = random.NextDouble() < sword.successRate.Value;
        user.Sword.SetLevel(isSuccess ? sword.level + 1 : 0);

        CombatStat stats = CalculateStats();
        var result = new EnhanceSwordResult(
            isSuccess ? EnhanceSwordOutcome.Success : EnhanceSwordOutcome.Destroyed,
            user.Sword, user.Currencies, stats, CalculateCombatPower(stats));
        return CopyAsResponse(result);
    }

    public async Task<EnhanceSwordSellResult> SellSwordAsync(int expectedLevel)
    {
        await Task.Yield();

        if (user.Sword.Level != expectedLevel)
        {
            throw new EnhanceRequestException(EnhanceRequestException.LevelMismatch, "검 강화 단계가 다릅니다.");
        }

        if (user.Sword.Level == 0)
        {
            throw new EnhanceRequestException(EnhanceRequestException.SwordNotSellable, "+0 검은 판매할 수 없습니다.");
        }

        long sellPrice = gameDB.swords.First(data => data.level == user.Sword.Level).sellPrice;
        user.Currencies.Add(CurrencyType.Gold, sellPrice);
        user.Sword.SetLevel(0);

        CombatStat stats = CalculateStats();
        var rewards = new List<PurchaseRewardResult> { new PurchaseRewardResult(RewardType.Gold, null, sellPrice) };
        var result = new EnhanceSwordSellResult(user.Sword, rewards, user.Currencies, stats, CalculateCombatPower(stats));
        return CopyAsResponse(result);
    }

    public async Task<EnhanceArtifactResult> EnhanceArtifactAsync(string artifactCode)
    {
        await Task.Yield();

        UserArtifact artifact = user.GetArtifact(artifactCode);
        if (artifact == null)
        {
            throw new EnhanceRequestException(EnhanceRequestException.ArtifactNotOwned, "보유하지 않은 아티팩트입니다.");
        }

        ArtifactGrade grade = gameDB.artifacts.First(data => data.artifactCode == artifactCode).grade;
        ArtifactEnhanceData enhance = gameDB.artifactEnhance.FirstOrDefault(data => data.grade == grade && data.level == artifact.Level);
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

        CombatStat stats = CalculateStats();
        var result = new EnhanceArtifactResult(new List<UserArtifact> { artifact }, user.Currencies, stats, CalculateCombatPower(stats));
        return CopyAsResponse(result);
    }

    private void SpendGold(long cost)
    {
        if (user.Currencies.Gold < cost)
        {
            throw new EnhanceRequestException(EnhanceRequestException.NotEnoughGold, "골드가 부족합니다.");
        }

        user.Currencies.Add(CurrencyType.Gold, -cost);
    }

    private CombatStat CalculateStats()
    {
        SwordData sword = gameDB.swords.First(data => data.level == user.Sword.Level);
        var equippedArtifacts = user.EquippedArtifacts
            .Select(artifact => (gameDB.artifacts.First(data => data.artifactCode == artifact.ArtifactCode), artifact.Level));
        return StatCalculator.CalculatePlayer(sword, equippedArtifacts);
    }

    private long CalculateCombatPower(CombatStat stats)
    {
        return CombatPowerCalculator.Calculate(stats, GetConfigValue("CRIT_MULTIPLIER"), GetConfigValue("BASE_BATTLE_TIME"));
    }

    private double GetConfigValue(string key)
    {
        return double.Parse(gameDB.config.First(data => data.key == key).value, CultureInfo.InvariantCulture);
    }

    // 서버 응답처럼 JSON을 거쳐서 넘긴다. 서버 역할의 user와 객체를 공유하지 않게 된다
    private static T CopyAsResponse<T>(T result)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(result));
    }
}
