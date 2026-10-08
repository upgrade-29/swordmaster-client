using System.Collections.Generic;
using Newtonsoft.Json;

public class EnhanceArtifactEquipResult
{
    public readonly IReadOnlyList<EnhanceEquippedArtifactResult> equippedArtifacts;

    [JsonConstructor]
    public EnhanceArtifactEquipResult(IReadOnlyList<EnhanceEquippedArtifactResult> equippedArtifacts)
    {
        this.equippedArtifacts = equippedArtifacts ?? new List<EnhanceEquippedArtifactResult>();
    }
}
