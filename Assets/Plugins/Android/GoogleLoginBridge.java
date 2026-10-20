package com.swordmaster.auth;

import android.util.Log;
import android.app.Activity;
import android.content.MutableContextWrapper;
import android.os.CancellationSignal;
import android.os.Handler;
import android.os.Looper;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executor;

public final class GoogleLoginBridge {

    private static final ConcurrentHashMap<String, CancellationSignal>
            requests = new ConcurrentHashMap<>();

    private GoogleLoginBridge() {
    }

    public static void signIn(
            Activity activity,
            String webClientId,
            String receiver
    ) {
        CancellationSignal signal = new CancellationSignal();

        if (requests.putIfAbsent(receiver, signal) != null) {
            return;
        }

        activity.runOnUiThread(() -> {
            if (signal.isCanceled()) {
                return;
            }

            try {
                CredentialManager manager =
                        CredentialManager.create(activity);

                GetSignInWithGoogleOption option =
                        new GetSignInWithGoogleOption.Builder(webClientId)
                                .build();

                GetCredentialRequest request =
                        new GetCredentialRequest.Builder()
                                .addCredentialOption(option)
                                .build();

                Handler handler = new Handler(Looper.getMainLooper());
                Executor executor = command -> handler.post(command);

                manager.getCredentialAsync(
                        activity,
                        request,
                        signal,
                        executor,
                        new CredentialManagerCallback<
                                GetCredentialResponse,
                                GetCredentialException>() {

                            @Override
                            public void onResult(
                                    GetCredentialResponse response
                            ) {
                                try {
                                    Credential credential =
                                            response.getCredential();

                                    if (!(credential
                                            instanceof CustomCredential)) {
                                        finish(
                                                receiver,
                                                "",
                                                "지원하지 않는 인증 응답입니다."
                                        );
                                        return;
                                    }

                                    CustomCredential custom =
                                            (CustomCredential) credential;

                                    String type = custom.getType();

                                    boolean isGoogle =
                                            GoogleIdTokenCredential
                                                    .TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
                                                    .equals(type)
                                            || GoogleIdTokenCredential
                                                    .TYPE_GOOGLE_ID_TOKEN_SIWG_CREDENTIAL
                                                    .equals(type);

                                    if (!isGoogle) {
                                        finish(
                                                receiver,
                                                "",
                                                "Google 인증 응답이 아닙니다."
                                        );
                                        return;
                                    }

                                    GoogleIdTokenCredential google =
                                            GoogleIdTokenCredential.createFrom(
                                                    custom.getData()
                                            );

                                    String idToken = google.getIdToken();

                                    if (idToken == null
                                            || idToken.trim().isEmpty()) {
                                        finish(
                                                receiver,
                                                "",
                                                "Google ID 토큰이 없습니다."
                                        );
                                        return;
                                    }

                                    finish(receiver, idToken, "");

                                } catch (Exception e) {
                                    finish(
                                            receiver,
                                            "",
                                            "Google 인증 결과 처리 실패: "
                                                    + e.getClass().getSimpleName()
                                    );
                                }
                            }

@Override
public void onError(GetCredentialException error) {

    Log.e(
        "GoogleLoginBridge",
        "Google authentication failed"
            + "\nException: " + error.getClass().getName()
            + "\nType: " + error.getType()
            + "\nMessage: " + error.getMessage(),
        error
    );

    String message;

    if (error instanceof GetCredentialCancellationException) {
        message = "Google 로그인 취소: "
                + error.getClass().getSimpleName();
    } else {
        message = "Google 인증 실패: "
                + error.getClass().getSimpleName();
    }

    // 빠져 있던 부분
    finish(receiver, "", message);
}
                }
        );

    } catch (Exception e) {
        Log.e("GoogleLoginBridge", "SignIn failed", e);

        finish(
                receiver,
                "",
                "Google 인증 실행 실패: "
                        + e.getClass().getSimpleName()
        );
    }
});
}

    public static void cancel(String receiver) {
        CancellationSignal signal = requests.remove(receiver);

        if (signal != null) {
            signal.cancel();
        }
    }

    private static void finish(
            String receiver,
            String idToken,
            String error
    ) {
        // 이미 취소됐거나 완료된 요청의 응답은 무시한다.
        if (requests.remove(receiver) == null) {
            return;
        }

        try {
            JSONObject result = new JSONObject();
            result.put("idToken", idToken);
            result.put("error", error);

            UnityPlayer.UnitySendMessage(
                    receiver,
                    "OnNativeGoogleResult",
                    result.toString()
            );

        } catch (Exception ignored) {
            UnityPlayer.UnitySendMessage(
                    receiver,
                    "OnNativeGoogleResult",
                    "{\"idToken\":\"\",\"error\":\"인증 응답 생성 실패\"}"
            );
        }
    }
}