using System.Threading.Tasks;

// 클라이언트가 전투를 요청하는 창구. 지금은 LocalBattleService, 서버가 생기면 서버 통신 구현으로 바꾼다
public interface IBattleService
{
    // 도전할 수 없으면 BattleRequestException을 던진다
    Task<BattleResult> RequestBattleAsync(int stageNumber);
}
