using System;

// 이미 전투 중, 잠긴 스테이지, 검증 실패처럼 서버가 전투 요청을 거절했을 때 던진다
// Message는 유저에게 보여 줄 문구이고, 실제 거절 이유는 로그용으로 Reason에 남긴다
public class BattleRequestException : Exception
{
    private const string DefaultMessage = "현재 배틀을 시작할 수 없는 상태입니다.";

    public string Reason { get; }

    public BattleRequestException(string reason) : base(DefaultMessage)
    {
        Reason = reason;
    }

    public override string ToString()
    {
        return $"{base.ToString()} Reason: {Reason}";
    }
}
