using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class User
{
    [JsonProperty("userId")] private long userId;
    [JsonProperty("nickname")] private string nickname;
    [JsonProperty("currencies")] private UserCurrencies currencies;
    [JsonProperty("sword")] private UserSword sword;
    [JsonProperty("artifacts")] private List<UserArtifact> artifacts;
    [JsonProperty("stageProgress")] private StageProgress stageProgress;

    public long UserId => userId;
    public string Nickname => nickname;
    public UserCurrencies Currencies => currencies;
    public UserSword Sword => sword;
    public IReadOnlyList<UserArtifact> Artifacts => artifacts;
    public StageProgress StageProgress => stageProgress;
    
    // 장착 목록은 따로 저장하지 않고, 보유 목록에서 만듬
    public IEnumerable<UserArtifact> EquippedArtifacts => artifacts.Where(artifact => artifact.IsEquipped).OrderBy(artifact => artifact.EquippedSlot);
    
    [JsonConstructor]
    public User(long userId, string nickname, UserCurrencies currencies, UserSword sword,
        List<UserArtifact> artifacts, StageProgress stageProgress)
    {
        this.userId = userId;
        this.nickname = nickname;
        this.currencies = currencies;
        this.sword = sword;
        this.artifacts = artifacts ?? new List<UserArtifact>();
        this.stageProgress = stageProgress;
    }

    public UserArtifact GetArtifact(string artifactCode)
    {
        return artifacts.FirstOrDefault(artifact => artifact.ArtifactCode == artifactCode);
    }

    public UserArtifact GetEquippedArtifact(int slot)
    {
        return artifacts.FirstOrDefault(artifact => artifact.EquippedSlot == slot);
    }

    public void AddArtifact(UserArtifact artifact)
    {
        artifacts.Add(artifact);
    }
}
