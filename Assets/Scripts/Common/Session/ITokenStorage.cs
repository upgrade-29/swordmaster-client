// 자동 로그인에 쓰는 리프레시 토큰을 기기에 저장한다. 플랫폼마다 저장 방식이 달라서 TokenStorageFactory가 구현을 고른다
// 액세스 토큰은 여기 저장하지 않고 GameSession 메모리에만 둔다
public interface ITokenStorage
{
    // 로그인이나 리프레시로 받은 토큰을 저장한다. 이전 토큰은 덮어쓴다
    void Save(string refreshToken);

    // 저장된 토큰이 없거나 읽을 수 없으면 false를 돌려준다
    bool TryLoad(out string refreshToken);

    // 로그아웃하거나 리프레시가 실패했을 때 저장된 토큰을 지운다
    void Clear();
}
