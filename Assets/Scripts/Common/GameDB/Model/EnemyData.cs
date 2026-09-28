using Newtonsoft.Json;

public class EnemyData
{
    public readonly string enemyCode;
    public readonly string name;
    public readonly string appearanceCode;
    public readonly bool boss;
    public readonly CombatStat stat;

    [JsonConstructor]
    public EnemyData(string enemyCode, string name, string appearanceCode, bool boss,
        double attack, double attackSpeed, double maxHp, double critRate, double lifesteal)
    {
        this.enemyCode = enemyCode;
        this.name = name;
        this.appearanceCode = appearanceCode;
        this.boss = boss;
        this.stat = new CombatStat(attack, attackSpeed, maxHp, critRate, lifesteal);
    }
}
