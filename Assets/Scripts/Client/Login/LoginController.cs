using UnityEngine;

// 로그인 요청을 처리하고 결과를 뷰에 전달한다. 서버 연동 전까지는 테스트 계정과 비교한다.
public class LoginController : MonoBehaviour
{
    [ReadOnly] [SerializeField] private LoginView view;

    [ReadOnly(true)] [SerializeField] private string testId = "test";
    [ReadOnly(true)] [SerializeField] private string testPassword = "1234";

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref view);
        view.OnClickLoginEvent += OnClickLogin;
    }

    private void OnDestroy()
    {
        if (view != null)
        {
            view.OnClickLoginEvent -= OnClickLogin;
        }
    }

    private void OnClickLogin(string id, string password)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            view.SetResult(false, "아이디와 비밀번호를 입력하세요.");
            return;
        }

        bool isSuccess = id == testId && password == testPassword;
        view.SetResult(isSuccess, isSuccess ? "로그인 성공" : "로그인 실패: 아이디 또는 비밀번호가 틀렸습니다.");
    }
}
