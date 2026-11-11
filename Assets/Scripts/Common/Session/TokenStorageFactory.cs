using UnityEngine;

// 실행 중인 플랫폼에 맞는 토큰 저장소를 만든다
public static class TokenStorageFactory
{
    public static ITokenStorage Create()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new AndroidKeystoreTokenStorage();
#else
        return new PlayerPrefsTokenStorage();
#endif
    }
}

// 에디터와 PC에서 쓴다. Keystore 같은 보안 저장소가 없어 PlayerPrefs에 평문으로 저장한다
public class PlayerPrefsTokenStorage : ITokenStorage
{
    private const string PrefsKey = "RefreshToken";

    public void Save(string refreshToken)
    {
        PlayerPrefs.SetString(PrefsKey, refreshToken);
        PlayerPrefs.Save();
    }

    public bool TryLoad(out string refreshToken)
    {
        refreshToken = PlayerPrefs.GetString(PrefsKey, null);
        return string.IsNullOrEmpty(refreshToken) == false;
    }

    public void Clear()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
    }
}

// Android 기기에서 쓴다. Plugins/Android/TokenCipher.java가 Keystore 키로 암호화한 문자열만 PlayerPrefs에 저장한다
// 에디터에는 Keystore가 없어서 TokenStorageFactory가 Android 기기에서만 이 구현을 고른다
public class AndroidKeystoreTokenStorage : ITokenStorage
{
    private const string PrefsKey = "EncryptedRefreshToken";
    private const string CipherClassName = "com.swordmaster.auth.TokenCipher";

    public void Save(string refreshToken)
    {
        string encrypted;
        using (AndroidJavaClass cipher = new AndroidJavaClass(CipherClassName))
        {
            encrypted = cipher.CallStatic<string>("encrypt", refreshToken);
        }

        // 암호화에 실패하면 이전 토큰도 남기지 않는다. 다음 실행에서 수동 로그인으로 넘어간다
        if (encrypted == null)
        {
            Debug.LogWarning("[TokenStorage] Encrypt failed");
            Clear();
            return;
        }

        PlayerPrefs.SetString(PrefsKey, encrypted);
        PlayerPrefs.Save();
    }

    public bool TryLoad(out string refreshToken)
    {
        refreshToken = null;

        string encrypted = PlayerPrefs.GetString(PrefsKey, null);
        if (string.IsNullOrEmpty(encrypted))
        {
            return false;
        }

        using (AndroidJavaClass cipher = new AndroidJavaClass(CipherClassName))
        {
            refreshToken = cipher.CallStatic<string>("decrypt", encrypted);
        }

        // 재설치나 백업 복원으로 키가 없거나 데이터가 손상됐다. 다시 읽어도 실패하니 지운다
        if (refreshToken == null)
        {
            Debug.LogWarning("[TokenStorage] Decrypt failed");
            Clear();
            return false;
        }

        return true;
    }

    public void Clear()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();

        using (AndroidJavaClass cipher = new AndroidJavaClass(CipherClassName))
        {
            cipher.CallStatic<bool>("deleteKey");
        }
    }
}
