using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum RewardType
{
    None = 0,
    Gold,
    Artifact,
    GachaBox,
    ProfileImage,
    Item,
}
