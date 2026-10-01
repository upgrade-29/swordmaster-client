// 로그인 요청 결과. 성공이면 AccessToken, 실패면 FailureKind와 Message를 채운다.
public class LoginResult
{
    public bool IsSuccess { get; }
    public string AccessToken { get; }
    public LoginFailureKind FailureKind { get; }
    public string Message { get; }

    private LoginResult(bool isSuccess, string accessToken, LoginFailureKind failureKind, string message)
    {
        IsSuccess = isSuccess;
        AccessToken = accessToken;
        FailureKind = failureKind;
        Message = message;
    }

    public static LoginResult Success(string accessToken)
    {
        return new LoginResult(true, accessToken, LoginFailureKind.None, string.Empty);
    }

    public static LoginResult Failure(LoginFailureKind failureKind, string message)
    {
        return new LoginResult(false, null, failureKind, message);
    }
}
