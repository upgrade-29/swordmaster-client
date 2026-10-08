using Newtonsoft.Json;

// 세부스테이지 하나가 끝났을 때 클라가 서버에 보내는 요약. 서버는 같은 시드로 다시 돌려서 이 값과 비교한다
// 공격 이벤트 전체는 보내지 않는다. 시드와 스탯이 같으면 서버가 똑같이 재현할 수 있다
public class BattleResultVerifyRequest
{
    public readonly string battleId;
    public readonly int subStageIndex;
    public readonly BattleSubStageResultEnum resultEnum;
    public readonly double playerHp; // 끝났을 때의 체력. 처치 회복은 더하기 전
    public readonly double enemyHp;
    public readonly double duration;
    public readonly int playerAttackCount;
    public readonly int enemyAttackCount;

    [JsonConstructor]
    public BattleResultVerifyRequest(string battleId, int subStageIndex, BattleSubStageResultEnum resultEnum, double playerHp,
        double enemyHp, double duration, int playerAttackCount, int enemyAttackCount)
    {
        this.battleId = battleId;
        this.subStageIndex = subStageIndex;
        this.resultEnum = resultEnum;
        this.playerHp = playerHp;
        this.enemyHp = enemyHp;
        this.duration = duration;
        this.playerAttackCount = playerAttackCount;
        this.enemyAttackCount = enemyAttackCount;
    }

    public static BattleResultVerifyRequest From(string battleId, BattleSubStage subStage)
    {
        return new BattleResultVerifyRequest(battleId, subStage.SubStageIndex, subStage.ResultEnum, subStage.PlayerHp,
            subStage.EnemyHp, subStage.Duration, subStage.PlayerAttackCount, subStage.EnemyAttackCount);
    }
}
