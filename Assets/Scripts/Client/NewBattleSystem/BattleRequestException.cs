using System;

// 쿨타임, 잠긴 스테이지처럼 서버가 전투 요청을 거절했을 때 던진다
public class BattleRequestException : Exception
{
    public BattleRequestException(string message) : base(message)
    {
        message = "현재 배틀을 시작할 수 없는 상태입니다.";
    }
}
