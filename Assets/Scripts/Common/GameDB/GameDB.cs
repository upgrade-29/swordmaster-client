using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

public class GameDB
{
    // 서버와 같은 데이터인지 확인하는 버전. 파일 맨 위 version은 추출한 시각이라 서버와 다를 수 있어,
    // 서버가 읽는 config 시트의 X_GAME_DATA_VERSION 값을 쓴다
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
    public GameDB(IReadOnlyList<SwordData> swords,
        IReadOnlyList<ArtifactData> artifacts,
        IReadOnlyList<ArtifactEnhanceData> artifactEnhance,
        IReadOnlyList<EnemyData> enemies,
        IReadOnlyList<StageData> stages,
        IReadOnlyList<DropRateData> dropRates,
        IReadOnlyList<ShopProductData> shopProducts,
        IReadOnlyList<ShopProductRewardData> shopProductRewards,
        IReadOnlyList<ConfigData> config)
    {
        this.swords = swords;
        this.artifacts = artifacts;
        this.artifactEnhance = artifactEnhance;
        this.enemies = enemies;
        this.stages = stages;
        this.dropRates = dropRates;
        this.shopProducts = shopProducts;
        this.shopProductRewards = shopProductRewards;
        this.config = config;
        version = config.First(x => x.key == ConfigKey.GameDataVersion).value;
    }

    // config 값은 문자열로 저장되어 있으므로 숫자로 바꿔서 돌려준다
    public double GetConfigValue(string key)
    {
        return double.Parse(config.First(x => x.key == key).value, CultureInfo.InvariantCulture);
    }
}
