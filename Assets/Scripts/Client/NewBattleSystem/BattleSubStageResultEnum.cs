using Newtonsoft.Json;

[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum BattleSubStageResultEnum
{
    None = 0, // 아직 진행 중
    EnemyDead,
    PlayerDead,
    TimeOver, // 제한 시간 안에 적을 처치하지 못함 (패배)
}
