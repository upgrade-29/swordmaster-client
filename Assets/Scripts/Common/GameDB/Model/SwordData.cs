using Newtonsoft.Json;

public class SwordData
{
    public readonly int level; 
    public readonly double? successRate; // 마지막 행(최대 강화 단계)에서는 null
    public readonly long? enhanceCost; // 마지막 행(최대 강화 단계)에서는 null
    public readonly long sellPrice;
    public readonly double attack;
    public readonly double attackSpeed;
    public readonly double maxHp;
    public readonly string name;
    public readonly string appearanceCode;

    [JsonConstructor]
    public SwordData(int level, double? successRate, long? enhanceCost, long sellPrice,
        double attack, double attackSpeed, double maxHp, string name, string appearanceCode)
    {
        this.level = level;
        this.successRate = successRate;
        this.enhanceCost = enhanceCost;
        this.sellPrice = sellPrice;
        this.attack = attack;
        this.attackSpeed = attackSpeed;
        this.maxHp = maxHp;
        this.name = name;
        this.appearanceCode = appearanceCode;
    }
}
