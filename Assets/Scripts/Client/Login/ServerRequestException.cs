using System;

// 전투 요청(HttpBattleSessionService)이 실패했을 때 던진다
// 서버가 에러 응답을 줬으면 StatusCode와 서버가 보낸 code/message가 담기고, 서버에 닿지 못했으면 StatusCode는 0이다
public class ServerRequestException : Exception
{
    public long StatusCode { get; }
    public string Code { get; } // 서버 에러 응답의 code. 없는 에러도 있다(입력값 오류, 동시 요청 등)

    public bool IsNetworkError => StatusCode == 0;
    public bool IsUnauthorized => StatusCode == 401;

    public ServerRequestException(long statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public override string ToString()
    {
        return $"{base.ToString()} Status: {StatusCode}, Code: {Code}";
    }
}
