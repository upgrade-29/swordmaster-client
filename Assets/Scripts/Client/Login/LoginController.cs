using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 로그인 요청과 로그인/회원가입 화면 전환을 처리한다. 서버 요청은 AuthApi, PlayerApi로 만들어 HttpRequest로 보낸다.
// 로그인에 성공하면 서버의 플레이어 정보를 받아 GameSession에 넣고 로비 씬으로 넘어간다.
// 플레이어가 없으면 닉네임 팝업을 띄워 닉네임을 받아 만든다
public class LoginController : MonoBehaviour
{
    private const string SessionExpiredMessage = "로그인이 만료되었습니다. 다시 로그인해 주세요.";

    [ReadOnly] [SerializeField] private TitleView titleView;
    [ReadOnly] [SerializeField] private LoginView view;
    [ReadOnly] [SerializeField] private SignupView signupView;
    [ReadOnly] [SerializeField] private NicknamePopup nicknamePopup;

    private GoogleLoginClient googleLoginClient;
    private bool isRequesting;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref titleView);
        GameUtil.Bind(gameObject, ref view);
        GameUtil.Bind(gameObject, ref signupView);
        nicknamePopup = GameUtil.Bind<NicknamePopup>(transform, "NicknamePopup");

        // Java가 결과를 전달할 전용 GameObject.
        // 다른 객체와 이름이 겹치지 않게 만든다.
        var callbackObject = new GameObject(
            "GoogleLoginCallback-" + System.Guid.NewGuid().ToString("N")
        );

        callbackObject.transform.SetParent(transform, false);

        googleLoginClient =
            callbackObject.AddComponent<GoogleLoginClient>();

        titleView.OnClickTouchEvent += OnClickTitle;
        view.OnClickLoginEvent += OnClickLogin;
        view.OnClickSignupEvent += OnClickOpenSignup;
        signupView.OnClickSignupEvent += OnClickSignup;
        signupView.OnClickBackEvent += OnClickSignupBack;
        view.OnClickGoogleEvent += OnClickGoogle;
        nicknamePopup.OnClickConfirmEvent += OnClickNicknameConfirm;
    }

    private void Start()
    {
        signupView.Hide();

        // 로그인한 상태에서 서버가 토큰을 거절해 이 씬으로 돌아온 경우. 타이틀 터치 없이 바로 로그인 화면을 띄운다
        if (GameSession.Instance.IsExpired)
        {
            GameSession.Instance.ClearExpired();
            ShowLoginPanel();
            view.SetResult(false, SessionExpiredMessage);
            return;
        }

        view.Hide();
        if (ResourceManager.Instance.IsPreloaded)
        {
            titleView.Show();
            return;
        }

        titleView.ShowLoading();
        StartCoroutine(PreloadResourcesRoutine());
    }

    private IEnumerator PreloadResourcesRoutine()
    {
        yield return ResourceManager.Instance.PreloadRoutine(titleView.SetLoadingProgress);
        titleView.Show();
    }

    private void OnDestroy()
    {
        if (titleView != null)
        {
            titleView.OnClickTouchEvent -= OnClickTitle;
        }

        if (view != null)
        {
            view.OnClickLoginEvent -= OnClickLogin;
            view.OnClickSignupEvent -= OnClickOpenSignup;
            view.OnClickGoogleEvent -= OnClickGoogle;
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

    // 저장된 리프레시 토큰이 있으면 자동 로그인을 시도하고, 없으면 로그인 화면을 띄운다
    private void OnClickTitle()
    {
        if (isRequesting)
        {
            return;
        }

        if (GameSession.Instance.TokenStorage.TryLoad(out string refreshToken) == false)
        {
            ShowLoginPanel();
            return;
        }

        isRequesting = true;
        titleView.SetInteractable(false);

        HttpRequest.Request(AuthApi.Refresh(refreshToken), OnRefreshResponse);
    }

    private void OnRefreshResponse(HttpError error, AuthApi.TokenResponse response)
    {
        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (error != null)
        {
            isRequesting = false;

            // 토큰이 만료됐거나 무효다. 다시 보내도 실패하니 지운다. 네트워크 오류면 다음에 다시 시도하도록 남긴다
            if (error.StatusCode == AuthApi.UnauthorizedStatusCode)
            {
                GameSession.Instance.TokenStorage.Clear();
                ShowLoginPanel();
                return;
            }

            ShowLoginPanel();
            view.SetResult(false, $"자동 로그인 실패: {error.Message}");
            return;
        }

        if (string.IsNullOrEmpty(response?.accessToken) || string.IsNullOrEmpty(response.refreshToken))
        {
            isRequesting = false;
            ShowLoginPanel();
            view.SetResult(false, "자동 로그인 실패: 서버 응답을 읽을 수 없습니다.");
            return;
        }

        // 받은 리프레시 토큰으로 저장된 토큰을 덮어쓴다. 이후는 직접 로그인했을 때와 같다
        GameSession.Instance.SetLogin(response.accessToken, response.refreshToken);
        HttpRequest.Request(PlayerApi.GetMe(), OnLoadPlayerResponse);
    }

    private void ShowLoginPanel()
    {
        titleView.ShowDimmed();
        view.Show();
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

    private void OnLoginResponse(HttpError error, AuthApi.TokenResponse response)
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

        if (string.IsNullOrEmpty(response?.accessToken) || string.IsNullOrEmpty(response.refreshToken))
        {
            FailLogin("서버 응답을 읽을 수 없습니다.");
            return;
        }

        GameSession.Instance.SetLogin(response.accessToken, response.refreshToken);
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

    // 자동 로그인 중에 실패하면 로그인 화면이 아직 없으므로 함께 띄운다
    private void FailLogin(string message)
    {
        isRequesting = false;
        ShowLoginPanel();
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
        SceneManager.LoadScene(GameConstants.Scenes.Lobby);
    }

    private void OnClickGoogle()
    {
        if (isRequesting)
        {
            return;
        }

        isRequesting = true;
        view.SetInteractable(false);
        view.SetMessage("Google 인증 중...");

        googleLoginClient.SignIn(
            onSuccess: idToken =>
            {
                // 인증을 기다리는 동안 로그인 화면이 사라졌을 수 있다.
                if (this == null)
                {
                    return;
                }

                view.SetMessage("게임 서버 로그인 중...");

                HttpRequest.Request(
                    AuthApi.GoogleLogin(idToken),
                    OnLoginResponse
                );
            },
            onFailure: message =>
            {
                if (this == null)
                {
                    return;
                }

                FailLogin(message);
            }
        );
    }
}
