using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum BattleSide
{
    None = 0,
    Player,
    Enemy,
}
