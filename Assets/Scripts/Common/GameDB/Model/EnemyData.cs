using Newtonsoft.Json;

public class EnemyData
{
    public readonly string enemyCode;
    public readonly string name;
    public readonly string appearanceCode;
    public readonly bool boss;
    public readonly double attack;
    public readonly double attackSpeed;
    public readonly double maxHp;
    public readonly double critRate;
    public readonly double lifesteal;

    [JsonConstructor]
    public EnemyData(string enemyCode, string name, string appearanceCode, bool boss,
        double attack, double attackSpeed, double maxHp, double critRate, double lifesteal)
    {
        this.enemyCode = enemyCode;
        this.name = name;
        this.appearanceCode = appearanceCode;
        this.boss = boss;
        this.attack = attack;
        this.attackSpeed = attackSpeed;
        this.maxHp = maxHp;
        this.critRate = critRate;
        this.lifesteal = lifesteal;
    }
}
