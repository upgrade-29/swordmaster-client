using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

// 씬이 바뀌어도 남도록 DontDestroyOnLoad로 두고, 읽은 이미지는 게임이 끝날 때까지 해제하지 않는다
public class ResourceManager : SingletonMonoBehaviour<ResourceManager>
{
    private readonly Dictionary<string, Sprite> dicSprite = new Dictionary<string, Sprite>();
    private readonly HashSet<string> setWarnedAddress = new HashSet<string>();
    private readonly List<AsyncOperationHandle<Sprite>> listPreloadHandle = new List<AsyncOperationHandle<Sprite>>();

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
    }

    /// <summary>
    /// preload 라벨 이미지를 모두 읽어 주소 이름으로 캐시에 넣는다. 이미 읽었으면 바로 끝낸다.
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

        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
            Addressables.LoadResourceLocationsAsync(GameConstants.AddressableLabels.Preload, typeof(Sprite));
        yield return locationsHandle;

        IList<IResourceLocation> locations = locationsHandle.Result;
        var listHandle = locations
            .Select(x => Addressables.LoadAssetAsync<Sprite>(x))
            .ToList();
        while (true)
        {
            int doneCount = listHandle.Count(x => x.IsDone);
            float progress = locations.Count == 0
                ? 1f
                : doneCount / (float)locations.Count;
            onProgress?.Invoke(progress);
            if (doneCount == listHandle.Count)
            {
                break;
            }

            yield return null;
        }

        for (int i = 0; i < locations.Count; i++)
        {
            AddPreloaded(locations[i].PrimaryKey, listHandle[i]);
        }

        Addressables.Release(locationsHandle);
        isPreloaded = true;
    }

    /// <summary>
    /// 미리 읽은 이미지를 주소로 꺼낸다. 미리 읽지 못한 주소면 경고를 한 번 남기고 null을 돌려준다.
    /// 호출하는 화면은 null이면 자리 표시(회색 상자 등)를 그대로 둔다.
    /// </summary>
    public Sprite GetSprite(string address)
    {
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

    private void AddPreloaded(string address, AsyncOperationHandle<Sprite> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[ResourceManager] 이미지를 읽지 못했습니다: {address}");
            Addressables.Release(handle);
            return;
        }

        dicSprite[address] = handle.Result;
        listPreloadHandle.Add(handle);
    }
}
