using System.Collections.Generic;
using UnityEngine;

// 상점 탭 진입, 상품 목록 바인딩, 구매 시작, 결과 반영, View 상태 전환을 조정한다.
// 탭 안쪽 세로 스크롤과 바깥쪽 가로 탭 스와이프의 라우팅은 ShopTab의 NestedScrollRect/LobbyTab이 담당한다.
// 서버 응답을 최종값으로 사용하며, 클라이언트가 구매 성공/가격/보상을 독자적으로 확정하지 않는다.
public class ShopScreenController : MonoBehaviour
{
    [ReadOnly] [SerializeField] private ShopScreenView view;

    [SerializeField] private ShopVisualCatalogSO visualCatalog;
    [SerializeField] private ConfirmPopupView confirmPopup;

    private User user;
    private ShopCatalog catalog;
    private ShopPurchaseCoordinator purchaseCoordinator;

    private bool isViewActive;
    private string pendingPurchaseProductCode;
    private string pendingReconcileProductCode;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref view);
        // 같은 GameObject의 두 컴포넌트 간 Awake/OnEnable 호출 순서를 Unity가 보장하지 않으므로,
        // view를 사용하기 전에 View 자신의 바인딩을 직접 보장한다.
        view.EnsureBound();

        view.OnPurchaseRequestedEvent += OnPurchaseRequested;
        view.OnRetryRequestedEvent += OnCatalogRetryRequested;
    }

    private void OnEnable()
    {
        isViewActive = true;
        // 화면 재진입 시 최신 상태를 다시 렌더링한다.
        RenderCurrentCatalog();
    }

    private void OnDisable()
    {
        isViewActive = false;
    }

    private void OnDestroy()
    {
        if (view != null)
        {
            view.OnPurchaseRequestedEvent -= OnPurchaseRequested;
            view.OnRetryRequestedEvent -= OnCatalogRetryRequested;
        }

        if (pendingPurchaseProductCode != null)
        {
            UnsubscribeConfirmPopup();
        }

        if (pendingReconcileProductCode != null)
        {
            UnsubscribeReconcileAlert();
        }

        if (purchaseCoordinator != null)
        {
            purchaseCoordinator.OnPurchaseSucceededEvent -= OnPurchaseSucceeded;
            purchaseCoordinator.OnPurchaseFailedEvent -= OnPurchaseFailed;
            purchaseCoordinator.OnPurchaseResultUnknownEvent -= OnPurchaseResultUnknown;
        }
    }

    // 상점 화면이 필요로 하는 런타임 의존성을 외부(장면 조립/세션 계층)에서 주입한다.
    // GameDB/User/구매 서비스를 실제로 어디서 가져오는지는 이 클래스의 책임이 아니다 (기존 Network Layer 부재, Phase 0 참고).
    public void Initialize(GameDB gameDB, User user, IShopPurchaseService purchaseService)
    {
        this.user = user;
        catalog = new ShopCatalog(gameDB);

        purchaseCoordinator = new ShopPurchaseCoordinator(purchaseService);
        purchaseCoordinator.OnPurchaseSucceededEvent += OnPurchaseSucceeded;
        purchaseCoordinator.OnPurchaseFailedEvent += OnPurchaseFailed;
        purchaseCoordinator.OnPurchaseResultUnknownEvent += OnPurchaseResultUnknown;

        RenderCurrentCatalog();
    }

    private void RenderCurrentCatalog()
    {
        if (isViewActive == false)
        {
            return;
        }

        if (catalog == null)
        {
            view.SetLoading(true);
            return;
        }

        if (catalog.Products.Count == 0)
        {
            view.ShowEmpty();
            return;
        }

        IReadOnlyList<ShopProductSectionViewData> sections =
            ShopDataMapper.ToSectionViewDataList(catalog.Products, visualCatalog);
        view.ShowProducts(sections);
    }

    // 구매 확인 팝업을 먼저 거친 뒤 진행한다 (shop-system.md 7절, 초기 정책: 모든 상품 공통 적용).
    private void OnPurchaseRequested(string productCode)
    {
        if (purchaseCoordinator == null || purchaseCoordinator.IsPurchasing == true)
        {
            return;
        }

        ShopProductModel product = catalog?.GetProduct(productCode);
        if (product == null)
        {
            return;
        }

        pendingPurchaseProductCode = productCode;
        confirmPopup.SetTitle("구매 확인");
        confirmPopup.SetMessage($"{product.Name}\n{product.Price} {product.PriceType}로 구매하시겠습니까?");
        confirmPopup.OnConfirmEvent += OnPurchaseConfirmed;
        confirmPopup.OnCancelEvent += OnPurchaseCancelled;
        confirmPopup.Show();
    }

    // 사용자가 구매를 확정하면 팝업을 닫고 실제 구매 요청을 시작한다.
    private void OnPurchaseConfirmed()
    {
        UnsubscribeConfirmPopup();
        confirmPopup.Hide();

        string productCode = pendingPurchaseProductCode;
        pendingPurchaseProductCode = null;

        bool started = purchaseCoordinator.TryStartPurchase(productCode);
        if (started == true && isViewActive == true)
        {
            view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Purchasing);
        }
    }

    private void OnPurchaseCancelled()
    {
        UnsubscribeConfirmPopup();
        confirmPopup.Hide();
        pendingPurchaseProductCode = null;
    }

    private void UnsubscribeConfirmPopup()
    {
        confirmPopup.OnConfirmEvent -= OnPurchaseConfirmed;
        confirmPopup.OnCancelEvent -= OnPurchaseCancelled;
    }

    // 결과를 먼저 장수명 사용자 상태(User)에 반영한 뒤, 화면이 활성 상태일 때만 View를 갱신한다 (shop-network-flow.md 10절).
    private void OnPurchaseSucceeded(string productCode, PurchaseResult result)
    {
        user.Currencies.SyncFrom(result.currencies);

        if (isViewActive == false)
        {
            return;
        }

        view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Ready);
    }

    private void OnPurchaseFailed(string productCode, string message)
    {
        if (isViewActive == true)
        {
            view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Ready);
        }

        ShowAlert("구매 실패", message);
    }

    // Timeout/연결 끊김으로 서버 처리 여부를 알 수 없는 상태(Reconciling). 재시도 전까지는 구매 중 표시를 유지한다.
    private void OnPurchaseResultUnknown(string productCode)
    {
        pendingReconcileProductCode = productCode;

        if (isViewActive == true)
        {
            view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Purchasing);
        }

        confirmPopup.SetTitle("구매 결과 확인 필요");
        confirmPopup.SetMessage("구매 결과를 확인하지 못했습니다. 다시 시도하시겠습니까?");
        confirmPopup.OnConfirmEvent += OnReconcileRetryConfirmed;
        confirmPopup.OnCancelEvent += OnReconcileRetryCancelled;
        confirmPopup.Show();
    }

    // 재시도를 시작할 수 있으면 구매 중 표시를 유지한 채 맡기고, 시작할 수 없으면(이미 Idle 등) 바로 포기 처리로 넘어간다.
    private void OnReconcileRetryConfirmed()
    {
        UnsubscribeReconcileAlert();
        confirmPopup.Hide();

        if (purchaseCoordinator.TryRetry() == true)
        {
            return;
        }

        purchaseCoordinator.CompleteReconcile();
        RestoreItemAfterReconcile();
    }

    // 실제 재조회(GET /api/users/me 등)는 기존 Network Layer 도입 이후 구현 대상이다.
    // 그 전까지는 재시도를 포기하면 Idle로 되돌리고, 화면 재진입 시 사용자 상태가 다시 반영되는 것에 맡긴다.
    private void OnReconcileRetryCancelled()
    {
        UnsubscribeReconcileAlert();
        confirmPopup.Hide();

        purchaseCoordinator.CompleteReconcile();
        RestoreItemAfterReconcile();
    }

    private void RestoreItemAfterReconcile()
    {
        string productCode = pendingReconcileProductCode;
        pendingReconcileProductCode = null;

        if (isViewActive == true && productCode != null)
        {
            view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Ready);
        }
    }

    private void UnsubscribeReconcileAlert()
    {
        confirmPopup.OnConfirmEvent -= OnReconcileRetryConfirmed;
        confirmPopup.OnCancelEvent -= OnReconcileRetryCancelled;
    }

    private void OnCatalogRetryRequested()
    {
        RenderCurrentCatalog();
    }

    private void ShowAlert(string title, string message)
    {
        confirmPopup.SetTitle(title);
        confirmPopup.SetMessage(message);
        confirmPopup.OnConfirmEvent += OnAlertClosed;
        confirmPopup.OnCancelEvent += OnAlertClosed;
        confirmPopup.Show();
    }

    private void OnAlertClosed()
    {
        confirmPopup.OnConfirmEvent -= OnAlertClosed;
        confirmPopup.OnCancelEvent -= OnAlertClosed;
        confirmPopup.Hide();
    }
}
