using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

// 씬이 바뀌어도 남도록 DontDestroyOnLoad로 두고, 읽은 이미지와 오디오는 게임이 끝날 때까지 해제하지 않는다
public class ResourceManager : SingletonMonoBehaviour<ResourceManager>
{
    private readonly Dictionary<string, Sprite> dicSprite = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, AudioClip> dicAudioClip = new Dictionary<string, AudioClip>();
    private readonly HashSet<string> setWarnedAddress = new HashSet<string>();
    private readonly List<AsyncOperationHandle> listPreloadHandle = new List<AsyncOperationHandle>();

    private bool isPreloaded;
    public bool IsPreloaded => isPreloaded;

    protected override void OnAwakeSingleton()
    {
        DontDestroyOnLoad(gameObject);
    }

    protected override void OnDestroySingleton()
    {
        foreach (var handle in listPreloadHandle)
        {
            Addressables.Release(handle);
        }

        listPreloadHandle.Clear();
        dicSprite.Clear();
        dicAudioClip.Clear();
    }

    /// <summary>
    /// preload 라벨 이미지와 오디오를 모두 읽어 주소 이름으로 캐시에 넣는다. 이미 읽었으면 바로 끝낸다.
    /// onProgress에는 읽기를 마친 비율(0~1)을 넘긴다.
    /// </summary>
    public IEnumerator PreloadRoutine(Action<float> onProgress)
    {
        if (isPreloaded)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        yield return Addressables.InitializeAsync();

        AsyncOperationHandle<IList<IResourceLocation>> spriteLocationsHandle =
            Addressables.LoadResourceLocationsAsync(GameConstants.AddressableLabels.Preload, typeof(Sprite));
        AsyncOperationHandle<IList<IResourceLocation>> audioLocationsHandle =
            Addressables.LoadResourceLocationsAsync(GameConstants.AddressableLabels.Preload, typeof(AudioClip));
        yield return spriteLocationsHandle;
        yield return audioLocationsHandle;

        IList<IResourceLocation> spriteLocations = spriteLocationsHandle.Result;
        IList<IResourceLocation> audioLocations = audioLocationsHandle.Result;
        var listSpriteHandle = spriteLocations
            .Select(x => Addressables.LoadAssetAsync<Sprite>(x))
            .ToList();
        var listAudioHandle = audioLocations
            .Select(x => Addressables.LoadAssetAsync<AudioClip>(x))
            .ToList();
        int totalCount = listSpriteHandle.Count + listAudioHandle.Count;
        while (true)
        {
            int doneCount = listSpriteHandle.Count(x => x.IsDone) + listAudioHandle.Count(x => x.IsDone);
            float progress = totalCount == 0
                ? 1f
                : doneCount / (float)totalCount;
            onProgress?.Invoke(progress);
            if (doneCount == totalCount)
            {
                break;
            }

            yield return null;
        }

        for (int i = 0; i < spriteLocations.Count; i++)
        {
            AddPreloaded(spriteLocations[i].PrimaryKey, listSpriteHandle[i], dicSprite);
        }

        for (int i = 0; i < audioLocations.Count; i++)
        {
            AddPreloaded(audioLocations[i].PrimaryKey, listAudioHandle[i], dicAudioClip);
        }

        Addressables.Release(spriteLocationsHandle);
        Addressables.Release(audioLocationsHandle);
        isPreloaded = true;
    }

    /// <summary>
    /// 미리 읽은 이미지를 주소로 꺼낸다. 미리 읽지 못한 주소면 경고를 한 번 남기고 null을 돌려준다.
    /// 호출하는 화면은 null이면 자리 표시(회색 상자 등)를 그대로 둔다. 빈 슬롯처럼 주소가 없으면 경고 없이 null을 돌려준다.
    /// </summary>
    public Sprite GetSprite(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return null;
        }

        if (dicSprite.TryGetValue(address, out Sprite sprite))
        {
            return sprite;
        }

        if (setWarnedAddress.Add(address))
        {
            Debug.LogWarning($"[ResourceManager] 미리 읽은 이미지가 없습니다: {address}");
        }

        return null;
    }

    /// <summary>
    /// 이미지가 있을 수도 없을 수도 있는 주소에 쓴다. 없으면 경고 없이 false를 돌려준다.
    /// </summary>
    public bool TryGetSprite(string address, out Sprite sprite)
    {
        sprite = null;
        return string.IsNullOrEmpty(address) == false && dicSprite.TryGetValue(address, out sprite);
    }

    /// <summary>
    /// 미리 읽은 오디오를 주소로 꺼낸다. 미리 읽지 못한 주소면 경고를 한 번 남기고 null을 돌려준다.
    /// </summary>
    public AudioClip GetAudioClip(string address)
    {
        if (dicAudioClip.TryGetValue(address, out AudioClip clip))
        {
            return clip;
        }

        if (setWarnedAddress.Add(address))
        {
            Debug.LogWarning($"[ResourceManager] 미리 읽은 오디오가 없습니다: {address}");
        }

        return null;
    }

    private void AddPreloaded<T>(string address, AsyncOperationHandle<T> handle, Dictionary<string, T> dicAsset)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[ResourceManager] 리소스를 읽지 못했습니다: {address}");
            Addressables.Release(handle);
            return;
        }

        dicAsset[address] = handle.Result;
        listPreloadHandle.Add(handle);
    }
}
