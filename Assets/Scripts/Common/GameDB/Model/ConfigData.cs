using Newtonsoft.Json;

public class ConfigData
{
    public readonly string key;
    public readonly string value;

    [JsonConstructor]
    public ConfigData(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}
