using UnityEngine;
using UnityEngine.SceneManagement;

// 로그인 요청과 로그인/회원가입 화면 전환을 처리한다. 서버 요청은 AuthApi, PlayerApi로 만들어 HttpRequest로 보낸다.
// 로그인에 성공하면 서버의 플레이어 정보를 받아 GameSession에 넣고 로비 씬으로 넘어간다.
// 플레이어가 없으면 닉네임 팝업을 띄워 닉네임을 받아 만든다
public class LoginController : MonoBehaviour
{
    private const string LobbySceneName = "LobbyScene";
    private const string SessionExpiredMessage = "로그인이 만료되었습니다. 다시 로그인해 주세요.";

    [ReadOnly] [SerializeField] private LoginView view;
    [ReadOnly] [SerializeField] private SignupView signupView;
    [ReadOnly] [SerializeField] private NicknamePopup nicknamePopup;

    private bool isRequesting;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref view);
        GameUtil.Bind(gameObject, ref signupView);
        nicknamePopup = GameUtil.Bind<NicknamePopup>(transform, "NicknamePopup");

        view.OnClickLoginEvent += OnClickLogin;
        view.OnClickSignupEvent += OnClickOpenSignup;
        signupView.OnClickSignupEvent += OnClickSignup;
        signupView.OnClickBackEvent += OnClickSignupBack;
        nicknamePopup.OnClickConfirmEvent += OnClickNicknameConfirm;
    }

    private void Start()
    {
        view.Show();
        signupView.Hide();

        // 로그인한 상태에서 서버가 토큰을 거절해 이 씬으로 돌아온 경우
        if (GameSession.Instance.IsExpired)
        {
            GameSession.Instance.ClearExpired();
            view.SetResult(false, SessionExpiredMessage);
        }
    }

    private void OnDestroy()
    {
        if (view != null)
        {
            view.OnClickLoginEvent -= OnClickLogin;
            view.OnClickSignupEvent -= OnClickOpenSignup;
        }

        if (signupView != null)
        {
            signupView.OnClickSignupEvent -= OnClickSignup;
            signupView.OnClickBackEvent -= OnClickSignupBack;
        }

        if (nicknamePopup != null)
        {
            nicknamePopup.OnClickConfirmEvent -= OnClickNicknameConfirm;
        }
    }

    private void OnClickOpenSignup()
    {
        view.Hide();
        signupView.Show();
    }

    private void OnClickSignupBack()
    {
        signupView.Hide();
        view.Show();
    }

    private void OnClickSignup(string id, string password, string passwordConfirm)
    {
        if (isRequesting)
        {
            return;
        }

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordConfirm))
        {
            signupView.SetResult(false, "모든 항목을 입력하세요.");
            return;
        }

        if (password != passwordConfirm)
        {
            signupView.SetResult(false, "비밀번호가 일치하지 않습니다.");
            return;
        }

        isRequesting = true;
        signupView.SetInteractable(false);
        signupView.SetMessage("회원가입 중...");

        HttpRequest.Request(AuthApi.Signup(id, password), (error, result) => OnSignupResponse(id, error));
    }

    private void OnSignupResponse(string id, HttpError error)
    {
        isRequesting = false;

        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        signupView.SetInteractable(true);

        if (error != null)
        {
            signupView.SetResult(false, $"회원가입 실패: {error.Message}");
            return;
        }

        // 가입한 아이디를 채운 채로 로그인 화면으로 돌아간다
        signupView.Hide();
        view.Show();
        view.SetLoginId(id);
        view.SetResult(true, "회원가입 완료. 로그인해 주세요.");
    }

    private void OnClickLogin(string id, string password)
    {
        if (isRequesting)
        {
            return;
        }

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            view.SetResult(false, "아이디와 비밀번호를 입력하세요.");
            return;
        }

        isRequesting = true;
        view.SetInteractable(false);
        view.SetMessage("로그인 중...");

        HttpRequest.Request(AuthApi.Login(id, password), OnLoginResponse);
    }

    private void OnLoginResponse(HttpError error, AuthApi.LoginResponse response)
    {
        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (error != null)
        {
            FailLogin(error.Message);
            return;
        }

        if (string.IsNullOrEmpty(response?.accessToken))
        {
            FailLogin("서버 응답을 읽을 수 없습니다.");
            return;
        }

        GameSession.Instance.SetLogin(response.accessToken);
        HttpRequest.Request(PlayerApi.GetMe(), OnLoadPlayerResponse);
    }

    // 플레이어가 없으면(404) 닉네임 팝업을 띄워 닉네임을 받아 만든 뒤 로비로 간다
    private void OnLoadPlayerResponse(HttpError error, PlayerApi.PlayerResponse player)
    {
        if (this == null)
        {
            return;
        }

        bool isPlayerNotFound = error?.StatusCode == PlayerApi.PlayerNotFoundStatusCode;
        if (error != null && isPlayerNotFound == false)
        {
            FailLogin(error.Message);
            return;
        }

        isRequesting = false;

        if (player == null)
        {
            view.SetMessage(string.Empty);
            nicknamePopup.Open();
            nicknamePopup.Focus();
            return;
        }

        EnterLobby(player);
    }

    private void FailLogin(string message)
    {
        isRequesting = false;
        view.SetInteractable(true);
        view.SetResult(false, $"로그인 실패: {message}");
    }

    private void OnClickNicknameConfirm(string nickname)
    {
        if (isRequesting)
        {
            return;
        }

        isRequesting = true;
        nicknamePopup.SetInteractable(false);

        HttpRequest.Request(PlayerApi.Create(nickname), OnCreatePlayerResponse);
    }

    private void OnCreatePlayerResponse(HttpError error, PlayerApi.PlayerResponse player)
    {
        isRequesting = false;

        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (error != null || player == null)
        {
            nicknamePopup.SetInteractable(true);
            nicknamePopup.SetError(error?.Message ?? string.Empty);
            nicknamePopup.Focus();
            return;
        }

        Debug.Log("[Login] Player created");
        EnterLobby(player);
    }

    private static void EnterLobby(PlayerApi.PlayerResponse player)
    {
        GameSession.Instance.SetUser(player.ToUser());
        SceneManager.LoadScene(LobbySceneName);
    }
}
