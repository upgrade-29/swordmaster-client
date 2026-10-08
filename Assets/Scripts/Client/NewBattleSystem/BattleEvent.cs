// 공격 한 번의 결과. BattleSubStage가 공격할 때마다 만들어 View들에게 넘긴다
public class BattleEvent
{
    public readonly double time; // 세부스테이지 시작부터 흐른 시간(초)
    public readonly BattleSide attacker;
    public readonly double damage;
    public readonly bool isCrit;
    public readonly double lifesteal; // 공격자가 회복한 체력 (최대 체력을 넘은 부분은 제외)
    public readonly double playerHp; // 이 공격이 끝난 뒤의 체력
    public readonly double enemyHp;

    public BattleEvent(double time, BattleSide attacker, double damage, bool isCrit, double lifesteal,
        double playerHp, double enemyHp)
    {
        this.time = time;
        this.attacker = attacker;
        this.damage = damage;
        this.isCrit = isCrit;
        this.lifesteal = lifesteal;
        this.playerHp = playerHp;
        this.enemyHp = enemyHp;
    }
}
