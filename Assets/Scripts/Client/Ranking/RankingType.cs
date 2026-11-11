using Newtonsoft.Json;

// 서버 RankingType과 같은 순서와 이름. 서버에는 UPPER_SNAKE로 보낸다
[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum RankingType
{
    SwordLevel,
    StageCleared,
    CombatPower,
}
