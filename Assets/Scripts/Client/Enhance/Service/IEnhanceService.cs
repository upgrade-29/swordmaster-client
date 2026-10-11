using System.Threading.Tasks;

// 강화할 수 없으면 EnhanceRequestException을 던진다
public interface IEnhanceService
{
    Task<EnhanceSwordResult> EnhanceSwordAsync(int expectedLevel);
    Task<EnhanceSwordSellResult> SellSwordAsync(int expectedLevel);
    Task<EnhanceArtifactResult> EnhanceArtifactAsync(string artifactCode);
    Task<EnhanceArtifactEquipResult> EquipArtifactAsync(int slot, string artifactCode);
    Task<EnhanceArtifactEquipResult> UnequipArtifactAsync(int slot);
}
