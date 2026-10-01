using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// shop-ui-prefab.md 2~5절 계약에 따라 공통 UI Prefab(ActionButton/CurrencyAmountView/ConfirmPopup)을 코드로 조립해
// Assets/Prefabs/UI/Common/ 아래에 저장한다. 상점 로직은 포함하지 않는다.
public static class CommonUiPrefabBuilder
{
    private const string ButtonPrefabPath = "Assets/Prefabs/UI/Common/Button/ActionButton.prefab";
    private const string CurrencyPrefabPath = "Assets/Prefabs/UI/Common/CurrencyAmountView.prefab";
    private const string ConfirmPopupPrefabPath = "Assets/Prefabs/UI/Common/ConfirmPopup.prefab";

    [MenuItem("Swordmaster/Shop/Build Common UI Prefabs/All")]
    public static void BuildAll()
    {
        BuildActionButton();
        BuildCurrencyAmountView();
        BuildConfirmPopup();
    }

    [MenuItem("Swordmaster/Shop/Build Common UI Prefabs/Action Button")]
    public static void BuildActionButton()
    {
        if (UiPrefabBuilderUtil.ConfirmOverwrite(ButtonPrefabPath) == false)
        {
            return;
        }

        GameObject root = CreateActionButtonHierarchy("ActionButton");
        UiPrefabBuilderUtil.SaveAndCleanup(root, ButtonPrefabPath);
    }

    [MenuItem("Swordmaster/Shop/Build Common UI Prefabs/Currency Amount View")]
    public static void BuildCurrencyAmountView()
    {
        if (UiPrefabBuilderUtil.ConfirmOverwrite(CurrencyPrefabPath) == false)
        {
            return;
        }

        GameObject root = UiPrefabBuilderUtil.CreateUiObject(null, "CurrencyAmountView", typeof(Image));
        UiPrefabBuilderUtil.SetSize((RectTransform)root.transform, new Vector2(160f, 60f));
        Image background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.35f);

        HorizontalLayoutGroup layoutGroup = root.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.spacing = 8f;
        layoutGroup.padding = new RectOffset(8, 8, 4, 4);
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;

        GameObject icon = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Icon", typeof(Image));
        UiPrefabBuilderUtil.SetSize((RectTransform)icon.transform, new Vector2(40f, 40f));

        GameObject amountText = UiPrefabBuilderUtil.CreateUiObject(root.transform, "AmountText", typeof(TextMeshProUGUI));
        UiPrefabBuilderUtil.SetSize((RectTransform)amountText.transform, new Vector2(100f, 40f));
        TextMeshProUGUI amountLabel = amountText.GetComponent<TextMeshProUGUI>();
        amountLabel.alignment = TextAlignmentOptions.MidlineLeft;
        amountLabel.text = "0";

        root.AddComponent<CurrencyAmountView>();

        UiPrefabBuilderUtil.SaveAndCleanup(root, CurrencyPrefabPath);
    }

    [MenuItem("Swordmaster/Shop/Build Common UI Prefabs/Confirm Popup")]
    public static void BuildConfirmPopup()
    {
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
        {
            EditorUtility.DisplayDialog("ActionButton 프리팹 없음",
                $"{ButtonPrefabPath}가 없습니다. ConfirmPopup은 ActionButton을 재사용하므로 먼저 Action Button을 빌드해 주세요.",
                "확인");
            return;
        }

        if (UiPrefabBuilderUtil.ConfirmOverwrite(ConfirmPopupPrefabPath) == false)
        {
            return;
        }

        GameObject root = UiPrefabBuilderUtil.CreateUiObject(null, "ConfirmPopup");
        UiPrefabBuilderUtil.SetStretch((RectTransform)root.transform);

        GameObject popupRoot = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Root");
        UiPrefabBuilderUtil.SetStretch((RectTransform)popupRoot.transform);

        GameObject dimmer = UiPrefabBuilderUtil.CreateUiObject(popupRoot.transform, "Dimmer", typeof(Image));
        UiPrefabBuilderUtil.SetStretch((RectTransform)dimmer.transform);
        dimmer.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject panel = UiPrefabBuilderUtil.CreateUiObject(popupRoot.transform, "Panel", typeof(Image));
        UiPrefabBuilderUtil.SetSize((RectTransform)panel.transform, new Vector2(600f, 400f));
        panel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject title = UiPrefabBuilderUtil.CreateUiObject(panel.transform, "Title", typeof(TextMeshProUGUI));
        RectTransform titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 80f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        TextMeshProUGUI titleLabel = title.GetComponent<TextMeshProUGUI>();
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.fontSize = 32f;
        titleLabel.text = "제목";

        GameObject message = UiPrefabBuilderUtil.CreateUiObject(panel.transform, "Message", typeof(TextMeshProUGUI));
        RectTransform messageRect = (RectTransform)message.transform;
        messageRect.anchorMin = new Vector2(0f, 0f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.offsetMin = new Vector2(24f, 100f);
        messageRect.offsetMax = new Vector2(-24f, -120f);
        TextMeshProUGUI messageLabel = message.GetComponent<TextMeshProUGUI>();
        messageLabel.alignment = TextAlignmentOptions.Center;
        messageLabel.text = "내용";

        GameObject buttons = UiPrefabBuilderUtil.CreateUiObject(panel.transform, "Buttons");
        RectTransform buttonsRect = (RectTransform)buttons.transform;
        buttonsRect.anchorMin = new Vector2(0f, 0f);
        buttonsRect.anchorMax = new Vector2(1f, 0f);
        buttonsRect.pivot = new Vector2(0.5f, 0f);
        buttonsRect.sizeDelta = new Vector2(0f, 100f);
        buttonsRect.anchoredPosition = new Vector2(0f, 20f);
        HorizontalLayoutGroup buttonsLayout = buttons.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.spacing = 24f;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = false;

        GameObject cancelButton = InstantiateActionButton(buttonPrefab, buttons.transform, "CancelButton", "취소");
        GameObject confirmButton = InstantiateActionButton(buttonPrefab, buttons.transform, "ConfirmButton", "확인");
        // 이 순서(취소 → 확인)는 조회 편의를 위한 배치일 뿐 우선순위/기본 선택을 의미하지 않는다.
        cancelButton.transform.SetAsFirstSibling();
        confirmButton.transform.SetAsLastSibling();

        root.AddComponent<ConfirmPopupView>();

        // 자식 ActionButton 인스턴스의 Awake 바인딩이 끝난 뒤에 기본 숨김 상태로 전환한다.
        popupRoot.SetActive(false);

        UiPrefabBuilderUtil.SaveAndCleanup(root, ConfirmPopupPrefabPath);
    }

    private static GameObject InstantiateActionButton(GameObject buttonPrefab, Transform parent, string name, string label)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
        instance.name = name;
        instance.GetComponent<ActionButtonView>().SetLabel(label);
        return instance;
    }

    private static GameObject CreateActionButtonHierarchy(string name)
    {
        GameObject root = UiPrefabBuilderUtil.CreateUiObject(null, name, typeof(Image));
        UiPrefabBuilderUtil.SetSize((RectTransform)root.transform, new Vector2(280f, 120f));
        LayoutElement layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 120f;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;

        GameObject icon = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Icon", typeof(Image));
        RectTransform iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(70f, 70f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        icon.SetActive(false);

        GameObject label = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Label", typeof(TextMeshProUGUI));
        UiPrefabBuilderUtil.SetStretch((RectTransform)label.transform);
        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.text = "Action";

        GameObject loading = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Loading", typeof(Image));
        UiPrefabBuilderUtil.SetSize((RectTransform)loading.transform, new Vector2(40f, 40f));
        loading.GetComponent<Image>().color = Color.white;
        loading.SetActive(false);

        root.AddComponent<ActionButtonView>();
        return root;
    }

}
