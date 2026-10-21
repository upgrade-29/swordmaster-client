using System.Collections.Generic;
using UnityEngine;

// 처음 호출될 때 생겨서 씬이 바뀌어도 남는다. 클립은 타이틀에서 ResourceManager가 미리 읽어 둔 것을 쓴다
public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    private const float SameSfxInterval = 0.05f; // 같은 효과음이 이 시간 안에 다시 오면 겹쳐 커지지 않게 건너뛴다

    private AudioSource audioBgm;
    private AudioSource audioSfx;

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
    public void PlayBgm(string address)
    {
        AudioClip clip = ResourceManager.Instance.GetAudioClip(address);
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

    public void PlaySfx(string address)
    {
        AudioClip clip = ResourceManager.Instance.GetAudioClip(address);
        if (clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (dicLastSfxTime.TryGetValue(address, out float lastTime) && now - lastTime < SameSfxInterval)
        {
            return;
        }

        dicLastSfxTime[address] = now;
        audioSfx.PlayOneShot(clip);
    }

    public void PlayRandomSfx(IReadOnlyList<string> addresses)
    {
        PlaySfx(addresses[Random.Range(0, addresses.Count)]);
    }
}
