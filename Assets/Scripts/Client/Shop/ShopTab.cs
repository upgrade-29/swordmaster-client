using System;
using System.Linq;
using UnityEngine;

// 상점 상품 목록을 만들고 구매를 요청한다. 구매 성공 여부와 재화는 서버 응답을 그대로 따른다.
// 탭 안쪽 세로 스크롤과 바깥쪽 가로 탭 스와이프의 라우팅은 기반 클래스 LobbyTab이 담당한다.
public class ShopTab : LobbyTab
{
    [ReadOnly] [SerializeField] private GameObject objViewport;
    [ReadOnly] [SerializeField] private RectTransform contentTransform;
    [ReadOnly] [SerializeField] private Transform comingSoonPanel;
    [ReadOnly] [SerializeField] private GameObject objEmptyPanel;
    [ReadOnly] [SerializeField] private ConfirmPopupView confirmPopup;

    [SerializeField] private ShopVisualCatalogSO visualCatalog;
    [SerializeField] private ShopSection sectionPrefab;
    [SerializeField] private ShopProductItem itemPrefab;

    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

    // 구매 확인부터 결과를 받을 때까지의 상품. 한 번에 하나만 구매한다
    private ShopProductItem purchasingItem;
    // 재전송할 때도 같은 값을 보낸다. 서버는 같은 requestId를 다시 처리하지 않고 처음 응답을 돌려준다
    private Guid purchaseRequestId;
    private bool isWaitingRetry;

    protected override void Awake()
    {
        base.Awake();

        objViewport = GameUtil.Bind<RectTransform>(transform, "Viewport").gameObject;
        contentTransform = GameUtil.Bind<RectTransform>(transform, "Viewport/Content");
        comingSoonPanel = GameUtil.Bind<RectTransform>(transform, "Viewport/Content/ComingSoonPanel");
        objEmptyPanel = GameUtil.Bind<RectTransform>(transform, "EmptyPanel").gameObject;
    }

    private void Start()
    {
        confirmPopup = LobbyPopupUI.Instance.Get<ConfirmPopupView>();

        InitProductList();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (confirmPopup != null)
        {
            UnsubscribePurchasePopup();
            UnsubscribeRetryPopup();
            UnsubscribeAlertPopup();
        }
    }

    // 섹션 순서는 상품 목록에서 각 카테고리가 처음 나오는 순서를 따른다.
    private void InitProductList()
    {
        objViewport.SetActive(GameDB.shopProducts.Count > 0);
        objEmptyPanel.SetActive(GameDB.shopProducts.Count == 0);

        foreach (var group in GameDB.shopProducts.GroupBy(x => x.category))
        {
            ShopSection section = Instantiate(sectionPrefab, contentTransform);
            section.SetHeader(visualCatalog.GetCategoryLabel(group.Key));

            foreach (ShopProductData product in group)
            {
                ShopProductItem item = Instantiate(itemPrefab, section.ItemsContainer);
                item.SetProduct(product, GetRepresentativeReward(product), visualCatalog);
                item.OnClickPurchaseEvent += OnClickPurchase;
            }
        }

        // Instantiate한 섹션은 마지막 자식으로 붙으므로, ComingSoonPanel을 다시 맨 아래로 보낸다.
        comingSoonPanel.SetAsLastSibling();
    }

    // 상품 카드에는 첫 번째 보상만 표시한다.
    private ShopProductRewardData GetRepresentativeReward(ShopProductData product)
    {
        return GameDB.shopProductRewards.FirstOrDefault(x => x.productCode == product.productCode);
    }

    private void SendPurchase()
    {
        isWaitingRetry = false;
        purchasingItem.SetPurchaseState(ShopProductPurchaseState.Purchasing);

        HttpRequest.Request(ShopApi.Purchase(purchaseRequestId, purchasingItem.Product.productCode), OnPurchaseResponse);
    }

    private void EndPurchase()
    {
        purchasingItem.SetPurchaseState(ShopProductPurchaseState.Ready);
        purchasingItem = null;
    }

    private void ShowPurchasePopup(ShopProductData product)
    {
        confirmPopup.Open();
        confirmPopup.SetTitle("구매 확인");
        confirmPopup.SetMessage($"{product.name}\n{product.price.ToString()} {product.priceType.ToString()}로 구매하시겠습니까?");
        confirmPopup.OnConfirmEvent += OnConfirmPurchase;
        confirmPopup.OnCancelEvent += OnCancelPurchase;
    }

    private void UnsubscribePurchasePopup()
    {
        confirmPopup.OnConfirmEvent -= OnConfirmPurchase;
        confirmPopup.OnCancelEvent -= OnCancelPurchase;
    }

    private void ShowRetryPopup()
    {
        confirmPopup.Open();
        confirmPopup.SetTitle("구매 결과 확인 필요");
        confirmPopup.SetMessage("구매 결과를 확인하지 못했습니다. 다시 시도하시겠습니까?");
        confirmPopup.OnConfirmEvent += OnConfirmRetry;
        confirmPopup.OnCancelEvent += OnCancelRetry;
    }

    private void UnsubscribeRetryPopup()
    {
        confirmPopup.OnConfirmEvent -= OnConfirmRetry;
        confirmPopup.OnCancelEvent -= OnCancelRetry;
    }

    private void ShowAlert(string title, string message)
    {
        confirmPopup.Open();
        confirmPopup.SetTitle(title);
        confirmPopup.SetMessage(message);
        confirmPopup.OnConfirmEvent += OnCloseAlert;
        confirmPopup.OnCancelEvent += OnCloseAlert;
    }

    private void UnsubscribeAlertPopup()
    {
        confirmPopup.OnConfirmEvent -= OnCloseAlert;
        confirmPopup.OnCancelEvent -= OnCloseAlert;
    }

    private void OnClickPurchase(ShopProductItem item)
    {
        // 재전송을 기다리는 동안에는 그 상품의 버튼만 눌리며, 누르면 재전송 팝업을 다시 연다
        if (isWaitingRetry)
        {
            if (item == purchasingItem)
            {
                ShowRetryPopup();
            }

            return;
        }

        if (purchasingItem != null)
        {
            return;
        }

        purchasingItem = item;
        ShowPurchasePopup(item.Product);
    }

    private void OnConfirmPurchase()
    {
        UnsubscribePurchasePopup();
        confirmPopup.Close();

        purchaseRequestId = Guid.NewGuid();
        SendPurchase();
    }

    private void OnCancelPurchase()
    {
        UnsubscribePurchasePopup();
        confirmPopup.Close();

        purchasingItem = null;
    }

    private void OnPurchaseResponse(HttpError error, ShopApi.PurchaseResponse response)
    {
        // 응답을 기다리는 동안 씬이 바뀌어 파괴됐을 수 있다
        if (this == null)
        {
            return;
        }

        if (error == null)
        {
            EndPurchase();
            User.Currencies.SyncFrom(response.currencies.ToUserCurrencies());
            TestLobbyDataLoader.Instance.NotifyChangeCurrencies();
            return;
        }

        if (HttpErrorUtil.IsResultUnknown(error))
        {
            isWaitingRetry = true;
            purchasingItem.SetPurchaseState(ShopProductPurchaseState.ResultUnknown);
            ShowRetryPopup();
            return;
        }

        EndPurchase();

        // 로그인 만료처럼 HttpErrorHandler가 이미 처리한 에러는 알림을 띄우지 않는다
        if (error.IsHandled)
        {
            return;
        }

        ShowAlert("구매 실패", error.Message);
    }

    private void OnConfirmRetry()
    {
        UnsubscribeRetryPopup();
        confirmPopup.Close();

        SendPurchase();
    }

    // 구매 중 상태와 requestId는 그대로 둔다
    private void OnCancelRetry()
    {
        UnsubscribeRetryPopup();
        confirmPopup.Close();
    }

    private void OnCloseAlert()
    {
        UnsubscribeAlertPopup();
        confirmPopup.Close();
    }
}
