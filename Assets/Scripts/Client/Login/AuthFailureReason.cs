// 인증(로그인/회원가입) 실패 원인. 원인마다 사용자에게 보여줄 문구가 다르다.
public enum AuthFailureReason
{
    InvalidCredentials, // 아이디 또는 비밀번호가 틀림 (401)
    DuplicateId,        // 이미 사용 중인 아이디 (409, 가정값)
    NetworkError,       // 서버에 연결할 수 없음
    Timeout,            // 응답 시간 초과
    ServerError,        // 그 밖의 서버 오류 응답
}
