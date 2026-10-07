using System;
using Newtonsoft.Json;

// 전투 시작 응답이자 재접속 응답. 서버의 배틀 세션 상태를 그대로 내려준다
// 클라는 subStageIndex 세부스테이지를 처음부터 진행한다. 재접속이면 끊긴 세부스테이지를 처음부터 다시 하고,
// 시드가 같으므로 결과도 같다
public class BattleSessionInfo
{
    public readonly string battleId;
    public readonly int stage;
    public readonly ulong battleSeed;
    public readonly CombatStat playerStat; // 서버가 계산한 값. 클라도 검증도 이 값으로 계산해야 결과가 같다
    public readonly int subStageIndex; // 지금 진행할 세부스테이지 (0부터)
    public readonly double playerHp; // 이 세부스테이지를 시작할 때의 체력
    public readonly long accumulatedGold; // 지금까지 처치해서 모은 골드. 전투가 끝날 때 한 번에 반영된다
    public readonly DateTime serverTime; // UTC

    [JsonConstructor]
    public BattleSessionInfo(string battleId, int stage, ulong battleSeed, CombatStat playerStat, int subStageIndex,
        double playerHp, long accumulatedGold, DateTime serverTime)
    {
        this.battleId = battleId;
        this.stage = stage;
        this.battleSeed = battleSeed;
        this.playerStat = playerStat;
        this.subStageIndex = subStageIndex;
        this.playerHp = playerHp;
        this.accumulatedGold = accumulatedGold;
        this.serverTime = serverTime;
    }
}
