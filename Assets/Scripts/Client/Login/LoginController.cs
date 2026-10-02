using UnityEngine;

// 로그인 요청과 로그인/회원가입 화면 전환을 처리한다. 서버 통신은 IAuthService에 맡긴다.
public class LoginController : MonoBehaviour
{
    [ReadOnly] [SerializeField] private LoginView view;
    [ReadOnly] [SerializeField] private SignupView signupView;

    [ReadOnly(true)] [SerializeField] private string baseUrl = "http://localhost:5000"; // 서버 주소 확정 전 가정값
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
            await authService.LoginAsync(id, password);
            isSuccess = true;
            message = "로그인 성공";
        }
        catch (AuthFailureException e)
        {
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

        view.SetInteractable(true);
        view.SetResult(isSuccess, message);
    }
}
