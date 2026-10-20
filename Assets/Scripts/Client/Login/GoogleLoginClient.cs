using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public sealed class GoogleLoginClient : MonoBehaviour
{
    private const string WebClientId =
        "108416504166-s6e69s8oo6fh27s58dko1t2aajj5fevt.apps.googleusercontent.com";

    private const string BridgeClass =
        "com.swordmaster.auth.GoogleLoginBridge";

    private Action<string> onSuccess;
    private Action<string> onFailure;

    private bool isSigningIn;
    private Coroutine timeoutRoutine;

    [Serializable]
    private sealed class NativeResult
    {
        public string idToken;
        public string error;
    }

    public void SignIn(
        Action<string> onSuccess,
        Action<string> onFailure
    )
    {
        if (isSigningIn)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        this.onSuccess = onSuccess;
        this.onFailure = onFailure;
        isSigningIn = true;

        try
        {
            using var unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            using var activity =
                unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity"
                );

            using var bridge = new AndroidJavaClass(BridgeClass);

            bridge.CallStatic(
                "signIn",
                activity,
                WebClientId,
                gameObject.name
            );

            timeoutRoutine = StartCoroutine(WaitForTimeout());
        }
        catch (Exception)
        {
            CancelNativeRequest();

            Complete(
                null,
                "Google 인증 기능을 실행할 수 없습니다. Android 설정을 확인하세요."
            );
        }
#else
        onFailure?.Invoke(
            "Google 로그인은 Android 기기 빌드에서 실행해 주세요."
        );
#endif
    }

    // Java의 UnitySendMessage로 호출된다.
    [Preserve]
    public void OnNativeGoogleResult(string json)
    {
        if (!isSigningIn)
        {
            return;
        }

        NativeResult result;

        try
        {
            result = JsonUtility.FromJson<NativeResult>(json);
        }
        catch (Exception)
        {
            Complete(null, "Google 인증 응답을 읽을 수 없습니다.");
            return;
        }

        if (result == null)
        {
            Complete(null, "Google 인증 응답이 없습니다.");
            return;
        }

        if (!string.IsNullOrEmpty(result.error))
        {
            Complete(null, result.error);
            return;
        }

        if (string.IsNullOrWhiteSpace(result.idToken))
        {
            Complete(null, "Google ID 토큰이 없습니다.");
            return;
        }

        Complete(result.idToken, null);
    }

    private IEnumerator WaitForTimeout()
    {
        yield return new WaitForSecondsRealtime(180f);

        timeoutRoutine = null;

        if (!isSigningIn)
        {
            yield break;
        }

        CancelNativeRequest();
        Complete(null, "Google 인증 시간이 초과되었습니다.");
    }

    private void Complete(string idToken, string error)
    {
        if (!isSigningIn)
        {
            return;
        }

        isSigningIn = false;

        if (timeoutRoutine != null)
        {
            StopCoroutine(timeoutRoutine);
            timeoutRoutine = null;
        }

        Action<string> success = onSuccess;
        Action<string> failure = onFailure;

        onSuccess = null;
        onFailure = null;

        if (!string.IsNullOrEmpty(error))
        {
            failure?.Invoke(error);
            return;
        }

        success?.Invoke(idToken);
    }

    private void CancelNativeRequest()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var bridge = new AndroidJavaClass(BridgeClass);

            bridge.CallStatic("cancel", gameObject.name);
        }
        catch (Exception)
        {
            // 화면 종료나 초기화 실패 중에는 취소 호출이 실패할 수 있다.
        }
#endif
    }

    private void OnDestroy()
    {
        if (isSigningIn)
        {
            CancelNativeRequest();
        }

        isSigningIn = false;
        onSuccess = null;
        onFailure = null;
    }
}