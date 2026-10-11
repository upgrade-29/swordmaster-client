using System;

// 로그인/회원가입 요청이 실패했을 때 던진다. Message는 사용자에게 그대로 보여줄 문구다
public class AuthFailureException : Exception
{
    public AuthFailureReason Reason { get; }

    public AuthFailureException(AuthFailureReason reason, string message) : base(message)
    {
        Reason = reason;
    }
}
