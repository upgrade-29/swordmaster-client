using System.Collections.Generic;
using Newtonsoft.Json;

public class EnhanceArtifactResult
{
    public readonly IReadOnlyList<UserArtifact> artifacts;
    public readonly UserCurrencies currencies;
    public readonly CombatStat stats;
    public readonly long combatPower;

    [JsonConstructor]
    public EnhanceArtifactResult(IReadOnlyList<UserArtifact> artifacts, UserCurrencies currencies,
        CombatStat stats, long combatPower)
    {
        this.artifacts = artifacts ?? new List<UserArtifact>();
        this.currencies = currencies;
        this.stats = stats;
        this.combatPower = combatPower;
    }
}
