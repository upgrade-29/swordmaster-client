using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum StatType
{
    None = 0,
    AttackSpeed,
    MaxHp,
    CritRate,
    Lifesteal,
}
