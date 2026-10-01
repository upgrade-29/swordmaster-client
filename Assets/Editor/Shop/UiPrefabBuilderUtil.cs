using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Shop/Common UI Prefab을 코드로 조립하는 Editor 빌더들이 공유하는 GameObject/RectTransform/저장 헬퍼.
internal static class UiPrefabBuilderUtil
{
    public static GameObject CreateUiObject(Transform parent, string name, params Type[] components)
    {
        Type[] allComponents = new Type[components.Length + 1];
        allComponents[0] = typeof(RectTransform);
        Array.Copy(components, 0, allComponents, 1, components.Length);

        GameObject go = new GameObject(name, allComponents);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            go.layer = uiLayer;
        }

        return go;
    }

    public static void SetStretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    public static void SetSize(RectTransform rectTransform, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    // 대상 경로에 이미 프리팹이 있으면 덮어쓸지 물어본다. 취소하면 false를 반환해 해당 프리팹 생성을 건너뛴다.
    public static bool ConfirmOverwrite(string path)
    {
        if (File.Exists(path) == false)
        {
            return true;
        }

        return EditorUtility.DisplayDialog(
            "프리팹 덮어쓰기 확인",
            $"{path}\n이미 존재하는 프리팹입니다. 내용을 덮어쓸까요?\n(건너뛰기를 선택하면 기존 프리팹을 그대로 둡니다)",
            "덮어쓰기",
            "건너뛰기");
    }

    public static void SaveAndCleanup(GameObject root, string path)
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));

        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (success == false)
        {
            Debug.LogError($"프리팹 저장 실패: {path}");
        }

        UnityEngine.Object.DestroyImmediate(root);
    }

    public static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) == true || AssetDatabase.IsValidFolder(folderPath) == true)
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(parent) == false)
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
    }
}
