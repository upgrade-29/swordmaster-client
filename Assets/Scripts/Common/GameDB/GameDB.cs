using System.Collections.Generic;
using Newtonsoft.Json;

public class GameDB
{
    public readonly string version;
    public readonly IReadOnlyList<SwordData> swords;
    public readonly IReadOnlyList<ArtifactData> artifacts;
    public readonly IReadOnlyList<ArtifactEnhanceData> artifactEnhance;
    public readonly IReadOnlyList<EnemyData> enemies;
    public readonly IReadOnlyList<StageData> stages;
    public readonly IReadOnlyList<DropRateData> dropRates;
    public readonly IReadOnlyList<ShopProductData> shopProducts;
    public readonly IReadOnlyList<ShopProductRewardData> shopProductRewards;
    public readonly IReadOnlyList<ConfigData> config;

    [JsonConstructor]
    public GameDB(string version,
        IReadOnlyList<SwordData> swords,
        IReadOnlyList<ArtifactData> artifacts,
        IReadOnlyList<ArtifactEnhanceData> artifactEnhance,
        IReadOnlyList<EnemyData> enemies,
        IReadOnlyList<StageData> stages,
        IReadOnlyList<DropRateData> dropRates,
        IReadOnlyList<ShopProductData> shopProducts,
        IReadOnlyList<ShopProductRewardData> shopProductRewards,
        IReadOnlyList<ConfigData> config)
    {
        this.version = version;
        this.swords = swords;
        this.artifacts = artifacts;
        this.artifactEnhance = artifactEnhance;
        this.enemies = enemies;
        this.stages = stages;
        this.dropRates = dropRates;
        this.shopProducts = shopProducts;
        this.shopProductRewards = shopProductRewards;
        this.config = config;
    }
}
