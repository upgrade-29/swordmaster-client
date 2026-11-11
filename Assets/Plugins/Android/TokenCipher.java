package com.swordmaster.auth;

import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.util.Log;

import java.nio.charset.StandardCharsets;
import java.security.InvalidKeyException;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

// 리프레시 토큰을 Android Keystore의 AES 키로 암호화/복호화한다. 키는 Keystore 밖으로 나오지 않고, 암호문만 C#이 PlayerPrefs에 저장한다
// C#에서는 AndroidJavaClass로 정적 메서드를 호출한다. 실패하면 예외 대신 null/false를 돌려줘 C#이 수동 로그인으로 넘어가게 한다
public final class TokenCipher {
    private static final String TAG = "TokenCipher";
    private static final String KEYSTORE_PROVIDER = "AndroidKeyStore";
    private static final String KEY_ALIAS = "swordmaster_refresh_token_key";
    private static final String TRANSFORMATION = "AES/GCM/NoPadding";
    private static final int KEY_SIZE_BITS = 256;
    private static final int GCM_TAG_BITS = 128;
    private static final String SEPARATOR = ":"; // Base64 문자에 없는 문자로 IV와 암호문을 구분한다

    private TokenCipher() {
    }

    // "IV:암호문" 형태의 Base64 문자열을 돌려준다. IV는 Keystore가 매번 새로 만든다
    public static String encrypt(String plainText) {
        try {
            return encryptWithKey(plainText, getOrCreateKey());
        } catch (InvalidKeyException e) {
            // 기기 보안 설정이 바뀌어 키가 무효화됐다. 키를 새로 만들어 한 번만 다시 시도한다
            Log.w(TAG, "Key invalidated, recreating", e);
            deleteKey();
            try {
                return encryptWithKey(plainText, getOrCreateKey());
            } catch (Exception retryError) {
                Log.e(TAG, "Encrypt failed after recreating key", retryError);
                return null;
            }
        } catch (Exception e) {
            Log.e(TAG, "Encrypt failed", e);
            return null;
        }
    }

    // 키가 없거나(재설치, 백업 복원) 데이터가 손상됐으면 null을 돌려준다
    public static String decrypt(String encrypted) {
        try {
            String[] parts = encrypted.split(SEPARATOR);
            if (parts.length != 2) {
                return null;
            }

            SecretKey key = getKey();
            if (key == null) {
                return null;
            }

            byte[] iv = Base64.decode(parts[0], Base64.NO_WRAP);
            byte[] cipherBytes = Base64.decode(parts[1], Base64.NO_WRAP);

            Cipher cipher = Cipher.getInstance(TRANSFORMATION);
            cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(GCM_TAG_BITS, iv));
            return new String(cipher.doFinal(cipherBytes), StandardCharsets.UTF_8);
        } catch (Exception e) {
            Log.e(TAG, "Decrypt failed", e);
            return null;
        }
    }

    // 로그아웃하거나 복호화에 실패했을 때 키까지 지운다. 다음 암호화 때 새 키를 만든다
    public static boolean deleteKey() {
        try {
            KeyStore keyStore = loadKeyStore();
            if (keyStore.containsAlias(KEY_ALIAS)) {
                keyStore.deleteEntry(KEY_ALIAS);
            }
            return true;
        } catch (Exception e) {
            Log.e(TAG, "Delete key failed", e);
            return false;
        }
    }

    private static String encryptWithKey(String plainText, SecretKey key) throws Exception {
        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.ENCRYPT_MODE, key);
        byte[] cipherBytes = cipher.doFinal(plainText.getBytes(StandardCharsets.UTF_8));

        String iv = Base64.encodeToString(cipher.getIV(), Base64.NO_WRAP);
        String body = Base64.encodeToString(cipherBytes, Base64.NO_WRAP);
        return iv + SEPARATOR + body;
    }

    private static KeyStore loadKeyStore() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(KEYSTORE_PROVIDER);
        keyStore.load(null);
        return keyStore;
    }

    private static SecretKey getKey() throws Exception {
        return (SecretKey) loadKeyStore().getKey(KEY_ALIAS, null);
    }

    private static SecretKey getOrCreateKey() throws Exception {
        SecretKey key = getKey();
        if (key != null) {
            return key;
        }

        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, KEYSTORE_PROVIDER);
        generator.init(new KeyGenParameterSpec.Builder(KEY_ALIAS, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(KEY_SIZE_BITS)
                .build());
        return generator.generateKey();
    }
}
