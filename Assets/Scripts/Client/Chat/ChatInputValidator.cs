// 서버 검증 전 사용자 입력을 빠르게 안내하기 위한 공개 채팅 UX 제약을 제공한다.
public static class ChatInputValidator
{
    public const int MaxContentLength = 500;

    // 서버 계약의 길이·줄바꿈 제약을 미리 검사하고 표시할 오류 문구를 반환한다.
    public static bool TryValidate(string content, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            errorMessage = "채팅 내용을 입력해주세요.";
            return false;
        }

        if (content.IndexOf('\r') >= 0 || content.IndexOf('\n') >= 0)
        {
            errorMessage = "줄바꿈은 입력할 수 없습니다.";
            return false;
        }

        if (content.Length > MaxContentLength)
        {
            errorMessage = "채팅은 최대 500자까지 입력할 수 있습니다.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
