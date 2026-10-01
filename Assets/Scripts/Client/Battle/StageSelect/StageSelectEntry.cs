// 전투 씬이 스테이지 선택 화면에 돌려주는 값. 전투 결과가 반영된 GameDB와 유저를 담는다
// 한 번 읽으면 지워진다. 값이 없으면(스테이지 선택 씬을 바로 연 경우) 테스트 데이터로 시작한다
public static class StageSelectEntry
{
    private static GameDB gameDB;
    private static User user;

    public static void Set(GameDB gameDB, User user)
    {
        StageSelectEntry.gameDB = gameDB;
        StageSelectEntry.user = user;
    }

    public static bool TryConsume(out GameDB gameDB, out User user)
    {
        gameDB = StageSelectEntry.gameDB;
        user = StageSelectEntry.user;

        bool hasValue = user != null;
        StageSelectEntry.gameDB = null;
        StageSelectEntry.user = null;
        return hasValue;
    }
}
