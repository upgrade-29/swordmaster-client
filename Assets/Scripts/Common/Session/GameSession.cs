using System;
using Newtonsoft.Json;
using UnityEngine;

// 로그인 토큰, 로그인한 유저, 게임 데이터. 로비, 전투, 상점, 강화가 같은 값을 읽고 고친다
// 씬이 바뀌어도 남도록 DontDestroyOnLoad로 두고, 플레이를 멈추면 오브젝트와 함께 값도 사라진다
public class GameSession : SingletonMonoBehaviour<GameSession>
{
    private const string AuthorizationHeader = "Authorization";
    private const string GameDBResourcePath = "Data/game_db"; // 게임 데이터는 아직 서버에서 받지 않아 시트에서 뽑은 파일을 읽는다

    private string accessToken; // 로그인으로 받은 JWT. HttpConfig의 Authorization 공통 헤더에도 넣어 HttpRequest로 보내는 요청에 붙게 한다
    public string AccessToken => accessToken;
    public bool IsLoggedIn => string.IsNullOrEmpty(accessToken) == false;

    private bool isExpired; // 로그인한 상태에서 서버가 토큰을 거절해 로그인 씬으로 돌아가는 중인지. 로그인 화면이 안내 문구를 띄운 뒤 지운다
    public bool IsExpired => isExpired;

    private ITokenStorage tokenStorage; // 자동 로그인용 리프레시 토큰을 기기에 저장한다. 액세스 토큰은 저장하지 않는다
    public ITokenStorage TokenStorage => tokenStorage;

    private GameDB gameDB;
    public GameDB GameDB => gameDB;

    private User user;
    public User User => user;

    public event Action OnChangeCurrenciesEvent = delegate { };

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);
        tokenStorage = TokenStorageFactory.Create();
        gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>(GameDBResourcePath).text);
    }

    // 로그인이나 리프레시로 받은 토큰 쌍을 적용한다. 리프레시 토큰은 쓸 때마다 서버가 새로 주므로 매번 덮어쓴다
    public void SetLogin(string accessToken, string refreshToken)
    {
        this.accessToken = accessToken;
        isExpired = false;
        HttpConfig.SetHeader(AuthorizationHeader, $"Bearer {accessToken}");
        tokenStorage.Save(refreshToken);
    }

    // 로그인한 상태에서 서버가 401로 토큰을 거절했을 때. 토큰과 유저를 지우고, 같은 토큰으로 맺은 실시간 연결도 끊는다
    // 리프레시 토큰은 남겨 두어 타이틀에서 자동 로그인으로 다시 들어올 수 있게 한다
    public void Expire()
    {
        ClearLogin();
        isExpired = true;
    }

    // 사용자가 로그아웃했을 때. 리프레시 토큰까지 지워 다음 실행에서 자동 로그인하지 않게 한다
    public void Logout()
    {
        ClearLogin();
        isExpired = false;
        tokenStorage.Clear();
    }

    private void ClearLogin()
    {
        accessToken = null;
        user = null;
        HttpConfig.RemoveHeader(AuthorizationHeader);

        // 연결한 적이 없으면 StompClient를 새로 만들지 않는다
        if (StompClient.IsInitialize())
        {
            StompClient.Instance.Disconnect();
        }
    }

    public void ClearExpired()
    {
        isExpired = false;
    }

    public void SetUser(User user)
    {
        this.user = user;
    }

    // 재화를 바꾼 쪽이 호출해서 로비 상단의 재화 표시를 갱신하게 한다
    public void NotifyChangeCurrencies()
    {
        OnChangeCurrenciesEvent.Invoke();
    }
}
