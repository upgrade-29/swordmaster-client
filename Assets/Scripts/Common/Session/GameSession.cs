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

    private GameDB gameDB;
    public GameDB GameDB => gameDB;

    private User user;
    public User User => user;

    public event Action OnChangeCurrenciesEvent = delegate { };

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);
        gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>(GameDBResourcePath).text);
    }

    public void SetLogin(string accessToken)
    {
        this.accessToken = accessToken;
        isExpired = false;
        HttpConfig.SetHeader(AuthorizationHeader, $"Bearer {accessToken}");
    }

    // 로그인한 상태에서 서버가 401로 토큰을 거절했을 때. 토큰과 유저를 지운다
    public void Expire()
    {
        accessToken = null;
        user = null;
        isExpired = true;
        HttpConfig.RemoveHeader(AuthorizationHeader);
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
