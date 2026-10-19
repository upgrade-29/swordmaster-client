using Newtonsoft.Json;

public class EnemyData
{
    public readonly string code;
    public readonly string name;
    public readonly bool boss;
    public readonly CombatStat stat;

    [JsonConstructor]
    public EnemyData(string code, string name, bool boss,
        double attack, double attackSpeed, double maxHp, double critRate, double lifesteal)
    {
        this.code = code;
        this.name = name;
        this.boss = boss;
        this.stat = new CombatStat(attack, attackSpeed, maxHp, critRate, lifesteal);
    }
}
