using System.Collections.Generic;
using Newtonsoft.Json;

public class BattleRewards
{
    public readonly long gold;
    public readonly IReadOnlyList<string> artifactCodes; // 드랍한 아티팩트. 이미 있으면 재료로 들어감

    [JsonConstructor]
    public BattleRewards(long gold, IReadOnlyList<string> artifactCodes)
    {
        this.gold = gold;
        this.artifactCodes = artifactCodes ?? new List<string>();
    }
}
