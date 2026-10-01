using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상품 컨테이너와 화면 상태(Loading/Ready/Empty/Error) 표시만 담당한다. 가격 판정이나 구매 트랜잭션을 갖지 않는다.
public class ShopScreenView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private GameObject objLoadingPanel;
    [ReadOnly] [SerializeField] private GameObject objEmptyPanel;
    [ReadOnly] [SerializeField] private GameObject objErrorPanel;
    [ReadOnly] [SerializeField] private NestedScrollRect nestedScrollRect;
    [ReadOnly] [SerializeField] private RectTransform contentTransform;
    [ReadOnly] [SerializeField] private TextMeshProUGUI errorMessageText;
    [ReadOnly] [SerializeField] private ActionButtonView retryButton;
    [ReadOnly] [SerializeField] private Transform comingSoonPanel;

    [SerializeField] private ShopProductItemView itemPrefab;
    [SerializeField] private ShopSectionView sectionPrefab;

    private readonly List<ShopSectionView> sections = new List<ShopSectionView>();
    private readonly List<ShopProductItemView> items = new List<ShopProductItemView>();

    public event Action<string> OnPurchaseRequestedEvent = delegate { };
    public event Action OnRetryRequestedEvent = delegate { };

    private bool isBound;

    private void Awake()
    {
        EnsureBound();
    }

    // ShopScreenController 등 같은 GameObject의 다른 컴포넌트가 Awake 호출 순서에 의존하지 않고
    // View를 안전하게 사용할 수 있도록, 바인딩이 끝나지 않았으면 즉시 바인딩한다.
    public void EnsureBound()
    {
        if (isBound == true)
        {
            return;
        }

        objLoadingPanel = GameUtil.Bind<RectTransform>(transform, "LoadingPanel").gameObject;
        objEmptyPanel = GameUtil.Bind<RectTransform>(transform, "EmptyPanel").gameObject;
        objErrorPanel = GameUtil.Bind<RectTransform>(transform, "ErrorPanel").gameObject;
        nestedScrollRect = GameUtil.Bind(gameObject, ref nestedScrollRect);
        contentTransform = nestedScrollRect.content;
        errorMessageText = GameUtil.Bind<TextMeshProUGUI>(transform, "ErrorPanel/Message");
        retryButton = GameUtil.Bind<ActionButtonView>(transform, "ErrorPanel/RetryButton");
        comingSoonPanel = GameUtil.Bind<RectTransform>(transform, "Viewport/Content/ComingSoonPanel");

        retryButton.OnClickEvent += OnClickRetry;
        isBound = true;
    }

    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.OnClickEvent -= OnClickRetry;
        }

        ClearSections();
    }

    public void SetLoading(bool loading)
    {
        if (loading == true)
        {
            ShowOnly(objLoadingPanel);
            return;
        }

        objLoadingPanel.SetActive(false);
    }

    public void ShowProducts(IReadOnlyList<ShopProductSectionViewData> productSections)
    {
        ShowOnly(nestedScrollRect.viewport.gameObject);
        RebuildSections(productSections);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);
        nestedScrollRect.StopMovement();
        nestedScrollRect.verticalNormalizedPosition = 1f;
    }

    public void ShowEmpty()
    {
        ShowOnly(objEmptyPanel);
    }

    // 특정 상품 하나의 표시 상태만 갱신한다 (구매 시작/완료/실패 반영용).
    public void SetItemPurchaseState(string productCode, ShopProductPurchaseState state)
    {
        ShopProductItemView item = items.FirstOrDefault(i => i.ProductCode == productCode);
        if (item != null)
        {
            item.SetPurchaseState(state);
        }
    }

    // isRecoverable이 false면(BlockingError) 재시도 버튼을 숨긴다.
    public void ShowError(string message, bool isRecoverable)
    {
        ShowOnly(objErrorPanel);
        errorMessageText.text = message;
        retryButton.gameObject.SetActive(isRecoverable);
    }

    private void ShowOnly(GameObject panelToShow)
    {
        objLoadingPanel.SetActive(panelToShow == objLoadingPanel);
        nestedScrollRect.viewport.gameObject.SetActive(panelToShow == nestedScrollRect.viewport.gameObject);
        objEmptyPanel.SetActive(panelToShow == objEmptyPanel);
        objErrorPanel.SetActive(panelToShow == objErrorPanel);
    }

    // 기존 섹션/상품 항목을 모두 정리한 뒤 전달받은 섹션 목록으로 새로 Instantiate해 채운다.
    // 섹션 하나당 ShopSectionView를 생성하고, 그 안의 ItemsContainer에 해당 섹션 상품들을 생성한다.
    private void RebuildSections(IReadOnlyList<ShopProductSectionViewData> productSections)
    {
        ClearSections();

        foreach (ShopProductSectionViewData section in productSections)
        {
            ShopSectionView sectionView = Instantiate(sectionPrefab, contentTransform);
            sectionView.SetHeader(section.CategoryDisplayName);
            sections.Add(sectionView);

            foreach (ShopProductViewData product in section.Products)
            {
                ShopProductItemView item = Instantiate(itemPrefab, sectionView.ItemsContainer);
                item.Bind(product);
                item.OnPurchaseRequestedEvent += OnItemPurchaseRequested;
                items.Add(item);
            }
        }

        // Instantiate는 항상 부모의 마지막 자식으로 추가되므로, 매번 다시 생성되는 섹션들보다
        // ComingSoonPanel이 항상 아래에 보이도록 매 Rebuild마다 마지막 Sibling으로 되돌린다.
        comingSoonPanel.SetAsLastSibling();
    }

    private void ClearSections()
    {
        foreach (ShopProductItemView item in items)
        {
            if (item == null)
            {
                continue;
            }

            item.OnPurchaseRequestedEvent -= OnItemPurchaseRequested;
        }

        items.Clear();

        foreach (ShopSectionView section in sections)
        {
            if (section == null)
            {
                continue;
            }

            Destroy(section.gameObject);
        }

        sections.Clear();
    }

    private void OnItemPurchaseRequested(string productCode)
    {
        OnPurchaseRequestedEvent.Invoke(productCode);
    }

    private void OnClickRetry()
    {
        OnRetryRequestedEvent.Invoke();
    }
}
