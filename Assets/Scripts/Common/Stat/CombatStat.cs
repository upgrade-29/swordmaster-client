using Newtonsoft.Json;

public class CombatStat
{
    public readonly double attack;
    public readonly double attackSpeed;
    public readonly double maxHp;
    public readonly double critRate;
    public readonly double lifesteal;

    [JsonConstructor]
    public CombatStat(double attack, double attackSpeed, double maxHp, double critRate, double lifesteal)
    {
        this.attack = attack;
        this.attackSpeed = attackSpeed;
        this.maxHp = maxHp;
        this.critRate = critRate;
        this.lifesteal = lifesteal;
    }
}
