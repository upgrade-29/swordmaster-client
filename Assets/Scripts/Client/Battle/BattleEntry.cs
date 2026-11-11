// 로비(스테이지 선택 화면)가 전투 씬에 넘겨 주는 값. 로비에서 서버로부터 받은 배틀 세션을 담는다. 유저와 게임 데이터는 GameSession에서 읽는다
// 세션 요청이 성공했을 때만 담고 전투 씬으로 넘어간다. 실패하면 로비에 남는다
// 한 번 읽으면 지워진다. 값이 없으면(전투 씬을 바로 연 경우) 전투 씬이 로비로 돌아간다
public static class BattleEntry
{
    private static BattleApi.BattleSessionInfoResponse session;

    public static void Set(BattleApi.BattleSessionInfoResponse session)
    {
        BattleEntry.session = session;
    }

    public static bool TryConsume(out BattleApi.BattleSessionInfoResponse session)
    {
        session = BattleEntry.session;
        BattleEntry.session = null;
        return session != null;
    }
}
