using System.Collections.Generic;
using UnityEngine;

// 처음 호출될 때 생겨서 씬이 바뀌어도 남는다. 클립은 Resources에서 읽어 게임이 끝날 때까지 들고 있는다
public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    private const float SameSfxInterval = 0.05f; // 같은 효과음이 이 시간 안에 다시 오면 겹쳐 커지지 않게 건너뛴다

    private AudioSource audioBgm;
    private AudioSource audioSfx;

    private readonly Dictionary<string, AudioClip> dicClip = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> dicLastSfxTime = new Dictionary<string, float>();

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);

        audioBgm = gameObject.AddComponent<AudioSource>();
        audioBgm.loop = true;
        audioBgm.playOnAwake = false;

        audioSfx = gameObject.AddComponent<AudioSource>();
        audioSfx.playOnAwake = false;
    }

    /// <summary>
    /// 같은 BGM이 이미 나오고 있으면 처음부터 다시 틀지 않는다.
    /// </summary>
    public void PlayBgm(string path)
    {
        AudioClip clip = LoadClip(path);
        if (clip == null)
        {
            return;
        }

        if (audioBgm.clip == clip && audioBgm.isPlaying)
        {
            return;
        }

        audioBgm.clip = clip;
        audioBgm.Play();
    }

    public void StopBgm()
    {
        audioBgm.Stop();
        audioBgm.clip = null;
    }

    public void PlaySfx(string path)
    {
        AudioClip clip = LoadClip(path);
        if (clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (dicLastSfxTime.TryGetValue(path, out float lastTime) && now - lastTime < SameSfxInterval)
        {
            return;
        }

        dicLastSfxTime[path] = now;
        audioSfx.PlayOneShot(clip);
    }

    public void PlayRandomSfx(IReadOnlyList<string> paths)
    {
        PlaySfx(paths[Random.Range(0, paths.Count)]);
    }

    private AudioClip LoadClip(string path)
    {
        if (dicClip.TryGetValue(path, out AudioClip clip))
        {
            return clip;
        }

        clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] 사운드를 찾지 못했습니다: {path}");
        }

        dicClip[path] = clip;
        return clip;
    }
}
