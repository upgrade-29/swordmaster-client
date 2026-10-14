using System;
using System.Linq;
using UnityEngine;

// 상점 상품 목록을 만들고 구매를 요청한다. 구매 성공 여부와 재화는 서버 응답을 그대로 따른다.
// 탭 안쪽 세로 스크롤과 바깥쪽 가로 탭 스와이프의 라우팅은 기반 클래스 LobbyTab이 담당한다.
public class ShopTab : LobbyTab
{
    private const int InternalServerErrorStatusCode = 500;
    private const int ServiceUnavailableStatusCode = 503;

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
    private GachaResultPresenter gachaResultPresenter;
    private GachaResultPopup gachaResultPopup;

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
        InitializeGachaResultPresenter();

        InitProductList();
    }

    private void OnDisable()
    {
        gachaResultPresenter?.Cancel();
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

        if (gachaResultPopup != null)
        {
            gachaResultPopup.OnNextRequestedEvent -= OnGachaNextRequested;
        }

        gachaResultPresenter?.Cancel();
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

    // 응답을 받지 못했거나 서버 내부 오류이면 서버가 구매를 처리했는지 알 수 없다.
    private static bool IsResultUnknown(HttpError error)
    {
        if (error.Type == HttpErrorType.Network)
        {
            return true;
        }

        return error.Type == HttpErrorType.Server
            && (error.StatusCode == InternalServerErrorStatusCode || error.StatusCode == ServiceUnavailableStatusCode);
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
        if (gachaResultPresenter?.IsPlaying == true)
        {
            return;
        }

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
            ShopProductData purchasedProduct = purchasingItem.Product;
            EndPurchase();
            ApplyPurchaseResult(response);

            if (purchasedProduct.category == ShopCategory.Gacha && response.rewards.Count > 0 &&
                (gachaResultPresenter == null || gachaResultPresenter.Play(response.rewards) == false))
            {
                Debug.LogWarning("GachaResultPopup이 없어 가챠 결과 연출을 시작하지 않습니다.");
            }

            return;
        }

        if (IsResultUnknown(error))
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

        ShowAlert("구매 실패", GetPurchaseFailureMessage(error));
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

    // 서버가 확정한 최종 재화와 유물 스냅샷을 연출 시작 전에 한 번만 반영한다.
    private void ApplyPurchaseResult(ShopApi.PurchaseResponse response)
    {
        User.Currencies.SyncFrom(response.currencies.ToUserCurrencies());
        ApplyChangedArtifacts(response.changedArtifacts);
        TestLobbyDataLoader.Instance.NotifyChangeCurrencies();
    }

    // null은 서버가 필드를 보내지 않은 경우이고 빈 배열은 변경 항목이 없음을 뜻한다. 둘 다 사용자 상태는 바꾸지 않는다.
    private void ApplyChangedArtifacts(System.Collections.Generic.IReadOnlyList<ArtifactResponse> changedArtifacts)
    {
        if (changedArtifacts == null || changedArtifacts.Count == 0)
        {
            return;
        }

        foreach (ArtifactResponse changedArtifact in changedArtifacts)
        {
            if (changedArtifact == null || string.IsNullOrEmpty(changedArtifact.code))
            {
                Debug.LogWarning("상점 구매 응답의 유물 스냅샷이 올바르지 않아 반영하지 않습니다.");
                continue;
            }

            UserArtifact ownedArtifact = User.GetArtifact(changedArtifact.code);
            if (ownedArtifact == null)
            {
                User.AddArtifact(changedArtifact.ToUserArtifact());
                continue;
            }

            ownedArtifact.SyncFromServerSnapshot(changedArtifact.level,
                changedArtifact.materialCount, changedArtifact.equipSlot);
        }
    }

    // GachaResultPopup Prefab은 별도 Editor 작업으로 추가되므로, 없으면 구매 결과만 반영한다.
    private void InitializeGachaResultPresenter()
    {
        gachaResultPopup = FindFirstObjectByType<GachaResultPopup>(FindObjectsInactive.Include);
        if (gachaResultPopup == null)
        {
            return;
        }

        gachaResultPresenter = new GachaResultPresenter(ShowGachaReward, gachaResultPopup.Close);
        gachaResultPopup.OnNextRequestedEvent += OnGachaNextRequested;
    }

    private void ShowGachaReward(ShopApi.RewardResponse reward, int index, int totalCount)
    {
        if (gachaResultPopup.IsOpened == false)
        {
            gachaResultPopup.Open();
        }

        gachaResultPopup.Show(reward, index, totalCount, visualCatalog, GameDB);
    }

    private void OnGachaNextRequested()
    {
        gachaResultPresenter?.Advance();
    }

    // 서버 오류 코드는 code를 우선하고, 이전 서버의 message 코드도 호환한다.
    private static string GetPurchaseFailureMessage(HttpError error)
    {
        string errorCode = string.IsNullOrEmpty(error.Code) == false ? error.Code : error.Message;
        switch (errorCode)
        {
            case "NOT_ENOUGH_GOLD":
                return "골드가 부족합니다.";
            case "NOT_ENOUGH_DIAMOND":
                return "다이아가 부족합니다.";
            case "REQUEST_EXPIRED":
                Debug.LogWarning("구매 결과 조회 기간이 만료되었습니다. 재화 재조회는 공용 HTTP 경계 준비 후 연결합니다.");
                return "구매 결과 조회 기간이 만료되었습니다. 현재 재화를 다시 확인해 주세요.";
            case "PRODUCT NOT FOUND":
                return "판매 중이 아닌 상품입니다.";
            case "Player NOT FOUND":
                return "플레이어 정보를 찾을 수 없습니다.";
            default:
                return "구매를 처리할 수 없습니다. 잠시 후 다시 시도해 주세요.";
        }
    }
}
