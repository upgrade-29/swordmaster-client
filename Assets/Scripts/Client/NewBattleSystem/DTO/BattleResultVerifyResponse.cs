using Newtonsoft.Json;

// 서버가 세부스테이지 보고를 검증한 뒤의 응답. 보고가 서버 계산과 다르면 응답 대신 에러가 오고 세션은 끝난다
// battleEnd가 null이면 다음 세부스테이지를 진행하고, 있으면 전투가 끝난 것이다
public class BattleResultVerifyResponse
{
    public readonly int subStageIndex;
    public readonly BattleSubStageResultEnum resultEnum;
    public readonly int nextSubStageIndex;
    public readonly double nextPlayerHp; // 처치 회복까지 더한 다음 세부스테이지 시작 체력
    public readonly long accumulatedGold; // 이번 처치까지 모은 골드
    public readonly BattleEndResponse battleEnd;

    [JsonIgnore] public bool IsBattleEnded => battleEnd != null;

    [JsonConstructor]
    public BattleResultVerifyResponse(int subStageIndex, BattleSubStageResultEnum resultEnum, int nextSubStageIndex,
        double nextPlayerHp, long accumulatedGold, BattleEndResponse battleEnd)
    {
        this.subStageIndex = subStageIndex;
        this.resultEnum = resultEnum;
        this.nextSubStageIndex = nextSubStageIndex;
        this.nextPlayerHp = nextPlayerHp;
        this.accumulatedGold = accumulatedGold;
        this.battleEnd = battleEnd;
    }
}
