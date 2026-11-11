public enum HttpErrorType
{
    Network,    // 서버에 연결하지 못함, 타임아웃
    Server,     // 서버가 4xx/5xx 상태 코드로 응답함
    Parse,      // 성공 응답의 본문을 역직렬화하지 못함
    Client,     // 요청을 만드는 중에 클라이언트에서 예외가 발생함(BaseUrl 미설정, 허용되지 않는 헤더 등)
}

public sealed class HttpError
{
    private readonly HttpErrorType type;
    public HttpErrorType Type => type;

    // HTTP 상태 코드입니다. 응답을 받지 못했으면 0입니다.
    private readonly int statusCode;
    public int StatusCode => statusCode;

    // 서버 에러 응답의 code입니다. 이유에 따라 다르게 처리해야 하는 에러에만 있고, 나머지는 null입니다.
    private readonly string code;
    public string Code => code;

    // 사용자에게 그대로 보여 줄 수 있는 문구입니다.
    private readonly string message;
    public string Message => message;

    // HttpErrorHandler가 이미 처리했는지 여부입니다. true라면 호출부에서 에러 팝업을 띄우지 않습니다.
    private bool isHandled;
    public bool IsHandled => isHandled;

    public HttpError(HttpErrorType type, int statusCode, string code, string message)
    {
        this.type = type;
        this.statusCode = statusCode;
        this.code = code;
        this.message = message;
    }

    internal void SetHandled(bool isHandled)
    {
        this.isHandled = isHandled;
    }

    public override string ToString()
    {
        return $"{type.ToString()}({statusCode.ToString()}, {code}): {message}";
    }
}

/// <summary>
/// 성공해도 본문이 없는 API(회원가입 등)의 응답 타입입니다. 콜백의 result는 null로 전달됩니다.
/// </summary>
public class EmptyResponse
{
}
