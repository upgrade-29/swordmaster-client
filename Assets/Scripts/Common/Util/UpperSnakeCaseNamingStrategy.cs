using Newtonsoft.Json.Serialization;

// Abcdef -> ABC_DEF
public class UpperSnakeCaseNamingStrategy : SnakeCaseNamingStrategy
{
    protected override string ResolvePropertyName(string name)
    {
        return base.ResolvePropertyName(name).ToUpperInvariant();
    }
}
