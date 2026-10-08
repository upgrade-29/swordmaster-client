using System;

// 이미 전투 중, 잠긴 스테이지, 검증 실패처럼 서버가 전투 요청을 거절했을 때 던진다
// Message는 유저에게 보여 줄 문구이고, 실제 거절 이유는 로그용으로 Reason에 남긴다
// 클라가 이유에 따라 다르게 처리해야 하면 ErrorEnum으로 구분한다
public class BattleRequestException : Exception
{
    private const string DefaultMessage = "현재 배틀을 시작할 수 없는 상태입니다.";
    private const string GameDataOutdatedMessage = "게임 데이터가 갱신되었습니다. 다시 접속해 주세요.";

    public BattleRequestErrorEnum ErrorEnum { get; }
    public string Reason { get; }

    public BattleRequestException(BattleRequestErrorEnum errorEnum, string reason)
        : base(errorEnum == BattleRequestErrorEnum.GameDataOutdated ? GameDataOutdatedMessage : DefaultMessage)
    {
        ErrorEnum = errorEnum;
        Reason = reason;
    }

    public override string ToString()
    {
        return $"{base.ToString()} Error: {ErrorEnum}, Reason: {Reason}";
    }
}
