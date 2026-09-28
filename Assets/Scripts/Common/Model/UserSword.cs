using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class UserSword
{
    [JsonProperty("level")] private int level;

    public int Level => level;

    [JsonConstructor]
    public UserSword(int level)
    {
        this.level = level;
    }

    public void SetLevel(int level)
    {
        this.level = level;
    }
}
