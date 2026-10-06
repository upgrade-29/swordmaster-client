using System.Collections.Generic;
using Newtonsoft.Json;

public class EnhanceArtifactResult
{
    public readonly IReadOnlyList<UserArtifact> artifacts;
    public readonly UserCurrencies currencies;

    [JsonConstructor]
    public EnhanceArtifactResult(IReadOnlyList<UserArtifact> artifacts, UserCurrencies currencies)
    {
        this.artifacts = artifacts ?? new List<UserArtifact>();
        this.currencies = currencies;
    }
    
}
