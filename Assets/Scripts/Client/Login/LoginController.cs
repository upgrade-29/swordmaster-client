using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// 로그인 요청과 로그인/회원가입 화면 전환을 처리한다. 서버 통신은 IAuthService에 맡긴다.
// 로그인에 성공하면 서버의 플레이어 정보를 받아(없으면 만들고) 로비에 넘기고 로비 씬으로 넘어간다
public class LoginController : MonoBehaviour
{
    private const string LobbySceneName = "LobbyScene";
    private const string PlayerMePath = "api/players/me";
    private const string PlayersPath = "api/players";

    [ReadOnly] [SerializeField] private LoginView view;
    [ReadOnly] [SerializeField] private SignupView signupView;

    [ReadOnly(true)] [SerializeField] private string baseUrl = "http://localhost:8080"; // 임시 값. 로그인하면 AuthJwtToken에 같이 저장된다
    [ReadOnly(true)] [SerializeField] private int timeoutSeconds = 10;

    private IAuthService authService;
    private bool isRequesting;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref view);
        GameUtil.Bind(gameObject, ref signupView);
        authService = new HttpAuthService(baseUrl, timeoutSeconds);

        view.OnClickLoginEvent += OnClickLogin;
        view.OnClickSignupEvent += OnClickOpenSignup;
        signupView.OnClickSignupEvent += OnClickSignup;
        signupView.OnClickBackEvent += OnClickSignupBack;
    }

    private void Start()
    {
        view.Show();
        signupView.Hide();
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

    private async void OnClickSignup(string id, string password, string passwordConfirm)
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

        bool isSuccess;
        string message;
        try
        {
            await authService.SignupAsync(id, password);
            isSuccess = true;
            message = string.Empty;
        }
        catch (AuthFailureException e)
        {
            isSuccess = false;
            message = $"회원가입 실패: {e.Message}";
        }
        finally
        {
            isRequesting = false;
        }

        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        signupView.SetInteractable(true);

        if (isSuccess == false)
        {
            signupView.SetResult(false, message);
            return;
        }

        // 가입한 아이디를 채운 채로 로그인 화면으로 돌아간다
        signupView.Hide();
        view.Show();
        view.SetLoginId(id);
        view.SetResult(true, "회원가입 완료. 로그인해 주세요.");
    }

    private async void OnClickLogin(string id, string password)
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

        bool isSuccess;
        string message;
        try
        {
            LoginResponse response = await authService.LoginAsync(id, password);
            AuthJwtToken.Set(response.accessToken, baseUrl);
            PlayerResponse player = await LoadPlayerAsync();
            LoginEntry.Set(player.ToUser());
            isSuccess = true;
            message = string.Empty;
        }
        catch (AuthFailureException e)
        {
            isSuccess = false;
            message = $"로그인 실패: {e.Message}";
        }
        catch (ServerRequestException e)
        {
            Debug.LogWarning($"[Login] Loading player failed: {e}");
            isSuccess = false;
            message = $"로그인 실패: {e.Message}";
        }
        finally
        {
            isRequesting = false;
        }

        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (isSuccess)
        {
            SceneManager.LoadScene(LobbySceneName);
            return;
        }

        view.SetInteractable(true);
        view.SetResult(false, message);
    }

    // 로그인한 계정의 플레이어 정보를 받는다
    // 서버는 회원가입 때 계정만 만들고 게임 플레이어는 만들지 않는다. 플레이어가 없으면 전투, 강화 등이 막히므로
    // 없으면(404) 만든다. 동시에 다른 곳에서 만들어 이미 있으면(409) 다시 받는다
    private static async Task<PlayerResponse> LoadPlayerAsync()
    {
        var client = new AuthorizedServerClient();
        try
        {
            return await client.GetAsync<PlayerResponse>(PlayerMePath);
        }
        catch (ServerRequestException e) when (e.StatusCode == 404)
        {
        }

        try
        {
            PlayerResponse created = await client.PostAsync<PlayerResponse>(PlayersPath, null);
            Debug.Log("[Login] Player created");
            return created;
        }
        catch (ServerRequestException e) when (e.StatusCode == 409)
        {
            return await client.GetAsync<PlayerResponse>(PlayerMePath);
        }
    }
}
