using System.Threading.Tasks;

// 클라이언트가 로그인을 요청하는 창구. HTTP 구현(HttpLoginService)이 이 인터페이스 뒤에서 서버와 통신한다
public interface ILoginService
{
    // 실패해도 예외를 던지지 않고 LoginResult.Failure로 돌려준다
    Task<LoginResult> LoginAsync(string loginId, string password);
}
