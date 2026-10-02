using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum EnhanceSwordOutcome
{
    None = 0,
    Success,
    Destroyed,
}
