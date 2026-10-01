// 로비(스테이지 선택 화면)가 전투 씬에 넘겨 주는 값. GameDB, 유저, 고른 스테이지를 담는다
// 한 번 읽으면 지워진다. 값이 없으면(전투 씬을 바로 연 경우) 전투 씬이 로비로 돌아간다
public static class BattleEntry
{
    private static GameDB gameDB;
    private static User user;
    private static int stage;

    public static void Set(GameDB gameDB, User user, int stage)
    {
        BattleEntry.gameDB = gameDB;
        BattleEntry.user = user;
        BattleEntry.stage = stage;
    }

    public static bool TryConsume(out GameDB gameDB, out User user, out int stage)
    {
        gameDB = BattleEntry.gameDB;
        user = BattleEntry.user;
        stage = BattleEntry.stage;

        bool hasValue = user != null;
        BattleEntry.gameDB = null;
        BattleEntry.user = null;
        BattleEntry.stage = 0;
        return hasValue;
    }
}
