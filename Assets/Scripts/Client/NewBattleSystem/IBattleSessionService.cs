using System.Threading.Tasks;

// 클라가 서버의 배틀 세션과 주고받는 창구. 유저당 세션은 하나뿐이다
// 지금은 가짜 서버로 구현하고, 서버가 생기면 서버 통신 구현으로 바꾼다
// 요청을 받아들일 수 없으면(세션 없음, 이미 전투 중, 잠긴 스테이지, 검증 실패) BattleRequestException을 던진다
public interface IBattleSessionService
{
    // 진행 중인 세션. 없으면 null. 앱을 다시 켰을 때 이어서 할 전투가 있는지 확인한다
    Task<BattleSessionInfo> GetActiveSessionAsync();

    // 새 전투를 시작한다. 진행 중인 세션이 있으면 거절한다
    Task<BattleSessionInfo> StartBattleAsync(int stage);

    // 끝난 세부스테이지를 보고한다. 서버 계산과 다르면 거절하고 세션을 패배로 끝낸다
    Task<BattleResultVerifyResponse> ReportSubStageAsync(BattleResultVerifyRequest request);

    // 전투를 포기한다. 패배로 처리하고, 그때까지 모은 골드는 반영한 뒤 세션을 끝낸다
    Task<BattleEndResult> AbandonAsync(string battleId);
}
