using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// shop-ui-prefab.md 2~4절 계약에 따라 상점 전용 Prefab(ShopRewardItem/ShopProductItem/ShopScreen)을 코드로
// 조립해 Assets/Prefabs/UI/Shop/ 아래에 저장한다. 수직 스크롤/Anchor/Layout Group 기준, 상품 수 고정 레이아웃 없음.
public static class ShopPrefabBuilder
{
    private const string CommonButtonPrefabPath = "Assets/Prefabs/UI/Common/Button/ActionButton.prefab";
    private const string RewardItemPrefabPath = "Assets/Prefabs/UI/Shop/ShopRewardItem.prefab";
    private const string ProductItemPrefabPath = "Assets/Prefabs/UI/Shop/ShopProductItem.prefab";
    private const string ScreenPrefabPath = "Assets/Prefabs/UI/Shop/ShopScreen.prefab";

    [MenuItem("Swordmaster/Shop/Build Shop Prefabs/All")]
    public static void BuildAll()
    {
        BuildShopRewardItem();
        BuildShopProductItem();
        BuildShopScreen();
    }

    [MenuItem("Swordmaster/Shop/Build Shop Prefabs/Shop Reward Item")]
    public static void BuildShopRewardItem()
    {
        if (UiPrefabBuilderUtil.ConfirmOverwrite(RewardItemPrefabPath) == false)
        {
            return;
        }

        GameObject root = CreateShopRewardItemHierarchy();
        UiPrefabBuilderUtil.SaveAndCleanup(root, RewardItemPrefabPath);
    }

    [MenuItem("Swordmaster/Shop/Build Shop Prefabs/Shop Product Item")]
    public static void BuildShopProductItem()
    {
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonButtonPrefabPath);
        GameObject rewardItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardItemPrefabPath);
        if (buttonPrefab == null || rewardItemPrefab == null)
        {
            EditorUtility.DisplayDialog("의존 프리팹 없음",
                $"{CommonButtonPrefabPath}와 {RewardItemPrefabPath}가 모두 있어야 합니다. 먼저 Action Button과 Shop Reward Item을 빌드해 주세요.",
                "확인");
            return;
        }

        if (UiPrefabBuilderUtil.ConfirmOverwrite(ProductItemPrefabPath) == false)
        {
            return;
        }

        GameObject root = CreateShopProductItemHierarchy(buttonPrefab, rewardItemPrefab);
        UiPrefabBuilderUtil.SaveAndCleanup(root, ProductItemPrefabPath);
    }

    [MenuItem("Swordmaster/Shop/Build Shop Prefabs/Shop Screen")]
    public static void BuildShopScreen()
    {
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonButtonPrefabPath);
        GameObject productItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProductItemPrefabPath);
        if (buttonPrefab == null || productItemPrefab == null)
        {
            EditorUtility.DisplayDialog("의존 프리팹 없음",
                $"{CommonButtonPrefabPath}와 {ProductItemPrefabPath}가 모두 있어야 합니다. 먼저 Action Button과 Shop Product Item을 빌드해 주세요.",
                "확인");
            return;
        }

        if (UiPrefabBuilderUtil.ConfirmOverwrite(ScreenPrefabPath) == false)
        {
            return;
        }

        GameObject root = CreateShopScreenHierarchy(buttonPrefab, productItemPrefab);
        UiPrefabBuilderUtil.SaveAndCleanup(root, ScreenPrefabPath);
    }

    private static GameObject CreateShopRewardItemHierarchy()
    {
        GameObject root = UiPrefabBuilderUtil.CreateUiObject(null, "ShopRewardItem");
        UiPrefabBuilderUtil.SetSize((RectTransform)root.transform, new Vector2(140f, 40f));

        HorizontalLayoutGroup layoutGroup = root.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.spacing = 6f;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;

        GameObject icon = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Icon", typeof(Image));
        UiPrefabBuilderUtil.SetSize((RectTransform)icon.transform, new Vector2(32f, 32f));

        GameObject amountText = UiPrefabBuilderUtil.CreateUiObject(root.transform, "AmountText", typeof(TextMeshProUGUI));
        UiPrefabBuilderUtil.SetSize((RectTransform)amountText.transform, new Vector2(96f, 32f));
        TextMeshProUGUI amountLabel = amountText.GetComponent<TextMeshProUGUI>();
        amountLabel.alignment = TextAlignmentOptions.MidlineLeft;
        amountLabel.text = "0";

        root.AddComponent<ShopRewardItemView>();
        return root;
    }

    private static GameObject CreateShopProductItemHierarchy(GameObject buttonPrefab, GameObject rewardItemPrefab)
    {
        GameObject root = UiPrefabBuilderUtil.CreateUiObject(null, "ShopProductItem", typeof(Image));
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        LayoutElement rootLayoutElement = root.AddComponent<LayoutElement>();
        rootLayoutElement.preferredHeight = 180f;

        HorizontalLayoutGroup layoutGroup = root.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.spacing = 16f;
        layoutGroup.padding = new RectOffset(16, 16, 16, 16);
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = true;

        GameObject productVisual = UiPrefabBuilderUtil.CreateUiObject(root.transform, "ProductVisual", typeof(Image));
        LayoutElement visualLayoutElement = productVisual.AddComponent<LayoutElement>();
        visualLayoutElement.preferredWidth = 120f;
        visualLayoutElement.preferredHeight = 120f;

        GameObject info = UiPrefabBuilderUtil.CreateUiObject(root.transform, "Info");
        LayoutElement infoLayoutElement = info.AddComponent<LayoutElement>();
        infoLayoutElement.flexibleWidth = 1f;
        VerticalLayoutGroup infoLayoutGroup = info.AddComponent<VerticalLayoutGroup>();
        infoLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        infoLayoutGroup.spacing = 8f;
        infoLayoutGroup.childForceExpandWidth = true;
        infoLayoutGroup.childForceExpandHeight = false;
        infoLayoutGroup.childControlWidth = true;
        infoLayoutGroup.childControlHeight = false;

        GameObject productName = UiPrefabBuilderUtil.CreateUiObject(info.transform, "ProductName", typeof(TextMeshProUGUI));
        LayoutElement nameLayoutElement = productName.AddComponent<LayoutElement>();
        nameLayoutElement.preferredHeight = 48f;
        TextMeshProUGUI nameLabel = productName.GetComponent<TextMeshProUGUI>();
        nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
        nameLabel.fontSize = 28f;
        nameLabel.text = "상품명";

        GameObject rewardItemInstance = (GameObject)PrefabUtility.InstantiatePrefab(rewardItemPrefab, info.transform);
        rewardItemInstance.name = "ShopRewardItem";
        LayoutElement rewardLayoutElement = rewardItemInstance.AddComponent<LayoutElement>();
        rewardLayoutElement.preferredHeight = 40f;

        GameObject actionButtonInstance = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, root.transform);
        actionButtonInstance.name = "ActionButton";
        LayoutElement buttonLayoutElement = actionButtonInstance.GetComponent<LayoutElement>();
        buttonLayoutElement.preferredWidth = 180f;

        root.AddComponent<ShopProductItemView>();
        return root;
    }

    private static GameObject CreateShopScreenHierarchy(GameObject buttonPrefab, GameObject productItemPrefab)
    {
        GameObject root = UiPrefabBuilderUtil.CreateUiObject(null, "ShopScreen");
        UiPrefabBuilderUtil.SetStretch((RectTransform)root.transform);

        GameObject scrollView = UiPrefabBuilderUtil.CreateUiObject(root.transform, "ScrollView", typeof(ScrollRect));
        UiPrefabBuilderUtil.SetStretch((RectTransform)scrollView.transform);

        GameObject viewport = UiPrefabBuilderUtil.CreateUiObject(scrollView.transform, "Viewport", typeof(RectMask2D));
        UiPrefabBuilderUtil.SetStretch((RectTransform)viewport.transform);

        GameObject content = UiPrefabBuilderUtil.CreateUiObject(viewport.transform, "Content");
        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        contentRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup contentLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
        contentLayoutGroup.spacing = 16f;
        contentLayoutGroup.padding = new RectOffset(16, 16, 16, 16);
        contentLayoutGroup.childForceExpandWidth = true;
        contentLayoutGroup.childForceExpandHeight = false;
        contentLayoutGroup.childControlWidth = true;
        contentLayoutGroup.childControlHeight = false;
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject loadingPanel = CreateStatePanel(root.transform, "LoadingPanel", "로딩 중");
        GameObject emptyPanel = CreateStatePanel(root.transform, "EmptyPanel", "표시할 상품이 없습니다");
        GameObject errorPanel = CreateErrorPanel(root.transform, buttonPrefab);

        // ScrollView/Loading/Empty/Error 자식 인스턴스들의 Awake 바인딩이 끝난 뒤 기본 표시 상태(Loading만 보임)로 전환한다.
        scrollView.SetActive(false);
        emptyPanel.SetActive(false);
        errorPanel.SetActive(false);

        ShopScreenView screenView = root.AddComponent<ShopScreenView>();
        SerializedObject serializedScreenView = new SerializedObject(screenView);
        serializedScreenView.FindProperty("itemPrefab").objectReferenceValue =
            productItemPrefab.GetComponent<ShopProductItemView>();
        serializedScreenView.ApplyModifiedProperties();

        return root;
    }

    private static GameObject CreateStatePanel(Transform parent, string name, string message)
    {
        GameObject panel = UiPrefabBuilderUtil.CreateUiObject(parent, name, typeof(Image));
        UiPrefabBuilderUtil.SetStretch((RectTransform)panel.transform);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);

        GameObject text = UiPrefabBuilderUtil.CreateUiObject(panel.transform, "Message", typeof(TextMeshProUGUI));
        UiPrefabBuilderUtil.SetStretch((RectTransform)text.transform);
        TextMeshProUGUI label = text.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.text = message;

        return panel;
    }

    private static GameObject CreateErrorPanel(Transform parent, GameObject buttonPrefab)
    {
        GameObject panel = UiPrefabBuilderUtil.CreateUiObject(parent, "ErrorPanel", typeof(Image));
        UiPrefabBuilderUtil.SetStretch((RectTransform)panel.transform);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);

        GameObject message = UiPrefabBuilderUtil.CreateUiObject(panel.transform, "Message", typeof(TextMeshProUGUI));
        RectTransform messageRect = (RectTransform)message.transform;
        messageRect.anchorMin = new Vector2(0f, 0.5f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.offsetMin = new Vector2(24f, 0f);
        messageRect.offsetMax = new Vector2(-24f, -24f);
        TextMeshProUGUI messageLabel = message.GetComponent<TextMeshProUGUI>();
        messageLabel.alignment = TextAlignmentOptions.Center;
        messageLabel.text = "오류가 발생했습니다";

        GameObject retryButtonInstance = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, panel.transform);
        retryButtonInstance.name = "RetryButton";
        RectTransform retryRect = (RectTransform)retryButtonInstance.transform;
        retryRect.anchorMin = new Vector2(0.5f, 0f);
        retryRect.anchorMax = new Vector2(0.5f, 0.5f);
        retryRect.pivot = new Vector2(0.5f, 0.5f);
        retryRect.anchoredPosition = new Vector2(0f, 60f);
        retryButtonInstance.GetComponent<ActionButtonView>().SetLabel("재시도");

        return panel;
    }
}
