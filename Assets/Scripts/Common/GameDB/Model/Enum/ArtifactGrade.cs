using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum ArtifactGrade
{
    None = 0,
    Common,
    Rare,
    Epic,
    Legendary,
}
