using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum ShopCategory
{
    None = 0,
    Gold,
    Gacha,
    Profile,
    Item,
}
