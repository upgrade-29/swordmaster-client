using System.Threading.Tasks;

// 클라이언트가 로그인/회원가입을 요청하는 창구. HTTP 구현(HttpAuthService)이 이 인터페이스 뒤에서 서버와 통신한다
public interface IAuthService
{
    // 실패하면 AuthFailureException을 던진다
    Task<LoginResponse> LoginAsync(string loginId, string password);

    // 성공하면 응답 본문 없이 끝난다. 실패하면 AuthFailureException을 던진다
    Task SignupAsync(string loginId, string password);
}
