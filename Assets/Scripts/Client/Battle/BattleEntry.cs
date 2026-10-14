// 로비(스테이지 선택 화면)가 전투 씬에 넘겨 주는 값. 고른 스테이지를 담는다. 유저와 게임 데이터는 GameSession에서 읽는다
// 한 번 읽으면 지워진다. 값이 없으면(전투 씬을 바로 연 경우) 전투 씬이 로비로 돌아간다
public static class BattleEntry
{
    private static int stage; // 스테이지는 1부터 시작하므로 0이면 값이 없다

    public static void Set(int stage)
    {
        BattleEntry.stage = stage;
    }

    public static bool TryConsume(out int stage)
    {
        stage = BattleEntry.stage;
        BattleEntry.stage = 0;
        return stage > 0;
    }
}
