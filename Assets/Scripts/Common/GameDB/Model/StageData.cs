using Newtonsoft.Json;

public class StageData
{
    public readonly int stage;
    public readonly string enemyCode;
    public readonly int enemyCount; // 세부 스테이지 수는 enemyCount + 1(보스)
    public readonly string bossCode;
    public readonly double statMultiplier;
    public readonly long goldPerEnemy;
    public readonly long bossGold;
    public readonly double artifactDropRate;
    public readonly long recommendedPower;
    public readonly string imageCode;

    [JsonConstructor]
    public StageData(int stage, string enemyCode, int enemyCount, string bossCode, double statMultiplier,
        long goldPerEnemy, long bossGold, double artifactDropRate, long recommendedPower, string imageCode)
    {
        this.stage = stage;
        this.enemyCode = enemyCode;
        this.enemyCount = enemyCount;
        this.bossCode = bossCode;
        this.statMultiplier = statMultiplier;
        this.goldPerEnemy = goldPerEnemy;
        this.bossGold = bossGold;
        this.artifactDropRate = artifactDropRate;
        this.recommendedPower = recommendedPower;
        this.imageCode = imageCode;
    }
}
