using Newtonsoft.Json;

// 서버가 전투 요청을 거절한 이유
[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum BattleRequestErrorEnum
{
    None = 0,
    AlreadyInProgress, // 진행 중인 세션이 있는데 새로 시작하려 함
    StageLocked,
    NoActiveBattle,
    BattleIdMismatch,
    VerifyFailed, // 보고가 서버 계산과 다름. 서버는 이미 패배로 끝냈다
    GameDataOutdated, // 클라 게임 데이터 버전이 서버와 다름. 세션은 그대로 남아 있어 데이터를 다시 받으면 이어서 할 수 있다
}
