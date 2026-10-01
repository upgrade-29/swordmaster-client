using UnityEngine;

// 로그인 요청을 처리하고 결과를 뷰에 전달한다. 서버 통신은 ILoginService에 맡긴다.
public class LoginController : MonoBehaviour
{
    [ReadOnly] [SerializeField] private LoginView view;

    [ReadOnly(true)] [SerializeField] private string baseUrl = "http://localhost:5000"; // 서버 주소 확정 전 가정값
    [ReadOnly(true)] [SerializeField] private int timeoutSeconds = 10;

    private ILoginService loginService;
    private bool isRequesting;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref view);
        loginService = new HttpLoginService(baseUrl, timeoutSeconds);

        view.OnClickLoginEvent += OnClickLogin;
    }

    private void OnDestroy()
    {
        if (view != null)
        {
            view.OnClickLoginEvent -= OnClickLogin;
        }
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

        LoginResult result;
        try
        {
            result = await loginService.LoginAsync(id, password);
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
        view.SetResult(result.IsSuccess, result.IsSuccess ? "로그인 성공" : $"로그인 실패: {result.Message}");
    }
}
