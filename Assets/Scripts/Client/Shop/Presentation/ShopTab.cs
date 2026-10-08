using System.Collections.Generic;
using UnityEngine;

// 상점 탭 진입, 상품 목록 바인딩, 구매 시작, 결과 반영, View 상태 전환을 조정한다.
// 탭 안쪽 세로 스크롤과 바깥쪽 가로 탭 스와이프의 라우팅은 기반 클래스 LobbyTab이 담당한다.
// 서버 응답을 최종값으로 사용하며, 클라이언트가 구매 성공/가격/보상을 독자적으로 확정하지 않는다.
public class ShopTab : LobbyTab
{
    [ReadOnly] [SerializeField] private ShopScreenView view;
    [ReadOnly] [SerializeField] private ConfirmPopupView confirmPopup;

    [SerializeField] private ShopVisualCatalogSO visualCatalog;

    private GameDB GameDB => TestLobbyDataLoader.Instance.GameDB;
    private User User => TestLobbyDataLoader.Instance.User;

    private ShopCatalog catalog;
    private ShopPurchaseCoordinator purchaseCoordinator;
    private ShopPurchaseSuccessApplier purchaseSuccessApplier;

    private bool isViewActive;
    private string pendingPurchaseProductCode;
    private string pendingReconcileProductCode;

    protected override void Awake()
    {
        base.Awake();

        GameUtil.Bind(gameObject, ref view);
        // 같은 GameObject의 두 컴포넌트 간 Awake/OnEnable 호출 순서를 Unity가 보장하지 않으므로,
        // view를 사용하기 전에 View 자신의 바인딩을 직접 보장한다.
        view.EnsureBound();

        view.OnPurchaseRequestedEvent += OnPurchaseRequested;
        view.OnRetryRequestedEvent += OnCatalogRetryRequested;
    }

    private void Start()
    {
        confirmPopup = LobbyPopupUI.Instance.Get<ConfirmPopupView>();

        catalog = new ShopCatalog(GameDB);

        purchaseCoordinator = new ShopPurchaseCoordinator(ShopPurchaseServiceComposition.CreateRuntime());
        purchaseCoordinator.OnPurchaseSucceededEvent += OnPurchaseSucceeded;
        purchaseCoordinator.OnPurchaseFailedEvent += OnPurchaseFailed;
        purchaseCoordinator.OnAuthenticationRequiredEvent += OnAuthenticationRequired;
        purchaseCoordinator.OnPurchaseResultUnknownEvent += OnPurchaseResultUnknown;
        purchaseSuccessApplier = new ShopPurchaseSuccessApplier(User.Currencies, view.SetItemPurchaseState);

        RenderCurrentCatalog();
    }

    private void OnEnable()
    {
        isViewActive = true;
        // 화면 재진입 시 최신 상태를 다시 렌더링한다.
        RenderCurrentCatalog();
        RestoreReconcilePresentation();
    }

    private void OnDisable()
    {
        isViewActive = false;

        if (pendingReconcileProductCode != null)
        {
            UnsubscribeReconcileAlert();
            confirmPopup.Close();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

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
            purchaseCoordinator.OnAuthenticationRequiredEvent -= OnAuthenticationRequired;
            purchaseCoordinator.OnPurchaseResultUnknownEvent -= OnPurchaseResultUnknown;
        }
    }

    // 현재 탭 활성 상태와 카탈로그 내용에 맞는 화면 상태를 다시 그린다.
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

    // 구매 확인 팝업을 먼저 거친 뒤 진행한다.
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
        confirmPopup.Open();
        confirmPopup.SetTitle("구매 확인");
        confirmPopup.SetMessage($"{product.Name}\n{product.Price} {product.PriceType}로 구매하시겠습니까?");
        confirmPopup.OnConfirmEvent += OnPurchaseConfirmed;
        confirmPopup.OnCancelEvent += OnPurchaseCancelled;
    }

    // 사용자가 구매를 확정하면 팝업을 닫고 실제 구매 요청을 시작한다.
    private void OnPurchaseConfirmed()
    {
        UnsubscribeConfirmPopup();
        confirmPopup.Close();

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
        confirmPopup.Close();
        pendingPurchaseProductCode = null;
    }

    private void UnsubscribeConfirmPopup()
    {
        confirmPopup.OnConfirmEvent -= OnPurchaseConfirmed;
        confirmPopup.OnCancelEvent -= OnPurchaseCancelled;
    }

    // 결과를 먼저 장수명 사용자 상태에 반영하고 비활성 화면은 직접 갱신하지 않는다.
    private void OnPurchaseSucceeded(string productCode, PurchaseResult result)
    {
        purchaseSuccessApplier.Apply(productCode, result, isViewActive);
        pendingReconcileProductCode = null;
    }

    // 명시적 실패 뒤 상품 입력을 복원하고 서버 안내를 사용자에게 표시한다.
    private void OnPurchaseFailed(string productCode, string message)
    {
        pendingReconcileProductCode = null;

        if (isViewActive == true)
        {
            view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Ready);
        }

        ShowAlert("구매 실패", message);
    }

    // 공용 인증 처리가 필요하다는 신호 뒤에는 Shop이 자체 재전송이나 토큰 갱신을 하지 않는다.
    private void OnAuthenticationRequired(string productCode)
    {
        pendingReconcileProductCode = null;

        if (isViewActive == true)
        {
            view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Ready);
        }
    }

    // 결과 불명 상태에서는 사용자가 재전송을 선택할 때까지 구매 중 표시를 유지한다.
    private void OnPurchaseResultUnknown(string productCode)
    {
        pendingReconcileProductCode = productCode;

        if (isViewActive == false)
        {
            return;
        }

        view.SetItemPurchaseState(productCode, ShopProductPurchaseState.Purchasing);
        ShowReconcileAlert();
    }

    // 결과 불명 거래가 남아 있으면 탭 재진입 뒤에도 구매 중 표시와 재전송 경로를 복원한다.
    private void RestoreReconcilePresentation()
    {
        if (purchaseCoordinator == null ||
            purchaseCoordinator.State != ShopPurchaseCoordinatorState.Reconciling ||
            pendingReconcileProductCode == null)
        {
            return;
        }

        view.SetItemPurchaseState(pendingReconcileProductCode, ShopProductPurchaseState.Purchasing);
        ShowReconcileAlert();
    }

    // 결과 불명 거래의 사용자 선택 UI를 표시하고 재진입 시 중복 구독을 막는다.
    private void ShowReconcileAlert()
    {
        UnsubscribeReconcileAlert();
        confirmPopup.Open();
        confirmPopup.SetTitle("구매 결과 확인 필요");
        confirmPopup.SetMessage("구매 결과를 확인하지 못했습니다. 다시 시도하시겠습니까?");
        confirmPopup.OnConfirmEvent += OnReconcileRetryConfirmed;
        confirmPopup.OnCancelEvent += OnReconcileRetryCancelled;
    }

    // 사용자가 확인한 결과 불명 거래만 같은 UUID로 재전송한다.
    private void OnReconcileRetryConfirmed()
    {
        UnsubscribeReconcileAlert();
        confirmPopup.Close();

        purchaseCoordinator.TryRetry();
    }

    // 취소는 재전송 UI만 닫으며 결과 불명 거래의 상태나 UUID를 변경하지 않는다.
    private void OnReconcileRetryCancelled()
    {
        UnsubscribeReconcileAlert();
        confirmPopup.Close();
        purchaseCoordinator.DismissReconcile();
    }

    private void UnsubscribeReconcileAlert()
    {
        confirmPopup.OnConfirmEvent -= OnReconcileRetryConfirmed;
        confirmPopup.OnCancelEvent -= OnReconcileRetryCancelled;
    }

    // 카탈로그 표시 오류의 재시도 요청은 현재 데이터로 화면만 다시 구성한다.
    private void OnCatalogRetryRequested()
    {
        RenderCurrentCatalog();
    }

    // 확인만 필요한 안내를 기존 확인 팝업으로 표시하고 닫기 입력을 한 곳에서 처리한다.
    private void ShowAlert(string title, string message)
    {
        confirmPopup.Open();
        confirmPopup.SetTitle(title);
        confirmPopup.SetMessage(message);
        confirmPopup.OnConfirmEvent += OnAlertClosed;
        confirmPopup.OnCancelEvent += OnAlertClosed;
    }

    private void OnAlertClosed()
    {
        confirmPopup.OnConfirmEvent -= OnAlertClosed;
        confirmPopup.OnCancelEvent -= OnAlertClosed;
        confirmPopup.Close();
    }
}
