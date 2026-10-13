using Newtonsoft.Json;

//에러 메시지 응답.
public class ErrorMessageResponse
{
    public readonly string message;

    [JsonConstructor]
    public ErrorMessageResponse(string message)
    {
        this.message = message;
    }
}
