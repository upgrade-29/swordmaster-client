using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum CurrencyType
{
    None = 0,
    Gold,
    Diamond,
}
