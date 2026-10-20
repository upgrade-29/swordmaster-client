using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 상점 상품 목록을 만들고 구매를 요청한다. 구매 성공 여부와 재화는 서버 응답을 그대로 따른다.
// 탭 안쪽 세로 스크롤과 바깥쪽 가로 탭 스와이프의 라우팅은 기반 클래스 LobbyTab이 담당한다.
public class ShopTab : LobbyTab
{
    [ReadOnly] [SerializeField] private GameObject objViewport;
    [ReadOnly] [SerializeField] private RectTransform contentTransform;
    [ReadOnly] [SerializeField] private GameObject objEmptyPanel;
    [ReadOnly] [SerializeField] private Image headerImage;
    [ReadOnly] [SerializeField] private GameObject objHeaderPlaceholderText; // 이미지가 없을 때 보여줄 "임시 이미지" 글자

    [SerializeField] private ShopSection sectionPrefab;
    [SerializeField] private ShopProductItem itemPrefab;

    [SerializeField] private Sprite spriteGold;
    [SerializeField] private Sprite spriteDiamond;
    [SerializeField] private Sprite spriteFallback; // 이미지가 정해지지 않은 상품, 재화, 보상에 쓴다

    // 얻은 재화가 탑UI 아이콘에 닿을 때 재생할 반짝임. 스프라이트 시트(gold_effect, diamond_effect)를 자른 순서대로 넣는다
    [SerializeField] private Sprite[] goldPopFrames;
    [SerializeField] private Sprite[] diamondPopFrames;

    private const string ArtifactGachaBoxCode = "ARTIFACT_BOX";
    private const string GoldGachaBoxCode = "GOLD_BOX";

    [ReadOnly] [SerializeField] private LobbyController lobbyController;

    private GameDB GameDB =>GameSession.Instance.GameDB;
    private User User => GameSession.Instance.User;
    private DialogPopup DialogPopup => Popup.Instance.Get<DialogPopup>();
    private GachaResultPopup GachaResultPopup => Popup.Instance.Get<GachaResultPopup>();

    // 구매 확인부터 결과를 받을 때까지의 상품. 한 번에 하나만 구매한다
    private ShopProductItem purchasingItem;
    // 재전송할 때도 같은 값을 보낸다. 서버는 같은 requestId를 다시 처리하지 않고 처음 응답을 돌려준다
    private Guid purchaseRequestId;
    private bool isWaitingRetry;
    // 가챠 결과 팝업에 보여 주는 중인 보상. 팝업을 닫으면 그중 재화를 보상 칸 자리에서 날린다
    private System.Collections.Generic.IReadOnlyList<RewardResponse> gachaRewards;

    protected override void Awake()
    {
        base.Awake();

        objViewport = GameUtil.Bind<RectTransform>(transform, "Viewport").gameObject;
        contentTransform = GameUtil.Bind<RectTransform>(transform, "Viewport/Content");
        objEmptyPanel = GameUtil.Bind<RectTransform>(transform, "EmptyPanel").gameObject;
        headerImage = GameUtil.Bind<Image>(transform, "Viewport/Content/ShopHeader");
        objHeaderPlaceholderText = GameUtil.Bind<RectTransform>(transform, "Viewport/Content/ShopHeader/Text (TMP)").gameObject;
        lobbyController = GetComponentInParent<LobbyController>();
    }

    private void Start()
    {
        InitHeaderImage();
        InitProductList();

        GachaResultPopup.OnCloseEvent += OnCloseGachaResult;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 씬을 닫을 때 Popup이 먼저 파괴됐으면 Instance로 새로 만들지 않는다. 팝업도 함께 파괴되므로 구독을 풀 필요가 없다
        if (Popup.IsInitialize())
        {
            GachaResultPopup.OnCloseEvent -= OnCloseGachaResult;
        }
    }

    // 미리 읽은 이미지가 없으면 회색 상자와 임시 글자를 그대로 둔다
    private void InitHeaderImage()
    {
        Sprite sprite = ResourceManager.Instance.GetSprite(GameConstants.AddressableAddresses.ShopHeader);
        if (sprite == null)
        {
            return;
        }

        headerImage.sprite = sprite;
        headerImage.color = Color.white;
        headerImage.preserveAspect = true;
        objHeaderPlaceholderText.SetActive(false);
    }

    // 섹션 순서는 상품 목록에서 각 카테고리가 처음 나오는 순서를 따른다.
    private void InitProductList()
    {
        objViewport.SetActive(GameDB.shopProducts.Count > 0);
        objEmptyPanel.SetActive(GameDB.shopProducts.Count == 0);

        foreach (var group in GameDB.shopProducts.GroupBy(x => x.category))
        {
            ShopSection section = Instantiate(sectionPrefab, contentTransform);
            section.SetHeader(GetCategoryLabel(group.Key));

            foreach (ShopProductData product in group)
            {
                ShopProductItem item = Instantiate(itemPrefab, section.ItemsContainer);
                ShopProductRewardData reward = GetRepresentativeReward(product);
                Sprite rewardIcon = reward == null
                    ? null
                    : GetRewardIcon(reward.rewardType);
                item.SetProduct(product, reward, GetProductIcon(reward), GetCurrencyIcon(product.priceType), rewardIcon);
                item.OnClickPurchaseEvent += OnClickPurchase;
            }
        }
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
        DialogPopup.Setup("구매 확인", $"{product.name}\n{product.price.ToString()} {GetPriceName(product.priceType)}로 구매하시겠습니까?",
            new DialogPopup.DialogPopupButtonInfo("확인", OnConfirmPurchase),
            new DialogPopup.DialogPopupButtonInfo("취소", OnCancelPurchase));
        DialogPopup.Open();
    }

    private void ShowRetryPopup()
    {
        DialogPopup.Setup("구매 결과 확인 필요", "구매 결과를 확인하지 못했습니다. 다시 시도하시겠습니까?",
            new DialogPopup.DialogPopupButtonInfo("확인", OnConfirmRetry),
            new DialogPopup.DialogPopupButtonInfo("취소", OnCancelRetry));
        DialogPopup.Open();
    }

    private void ShowAlert(string title, string message)
    {
        SoundManager.Instance.PlaySfx(GameConstants.Sounds.UiError);
        DialogPopup.Setup(title, message, new DialogPopup.DialogPopupButtonInfo("확인", OnCloseAlert));
        DialogPopup.Open();
    }

    private void OnClickPurchase(ShopProductItem item)
    {
        if (GachaResultPopup.IsOpened)
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

        // 무료 상품은 확인 없이 바로 받는다
        if (item.Product.priceType == PriceType.Free)
        {
            StartPurchase();
            return;
        }

        ShowPurchasePopup(item.Product);
    }

    private void OnConfirmPurchase()
    {
        DialogPopup.Close();

        StartPurchase();
    }

    private void StartPurchase()
    {
        purchaseRequestId = Guid.NewGuid();
        SendPurchase();
    }

    private void OnCancelPurchase()
    {
        DialogPopup.Close();

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
            ShopProductItem purchasedItem = purchasingItem;
            EndPurchase();
            ApplyPurchaseResult(response);

            HoldCurrencyRewards(response.rewards);
            if (purchasedItem.Product.category == ShopCategory.Gacha && response.rewards.Count > 0)
            {
                ShowGachaResult(response.rewards);
            }
            else
            {
                PlayCurrencyRewardFly(response.rewards, _ => purchasedItem.VisualPosition);
            }

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

        ShowAlert("구매 실패", GetPurchaseFailureMessage(error));
    }

    private void OnConfirmRetry()
    {
        DialogPopup.Close();

        SendPurchase();
    }

    // 구매 중 상태와 requestId는 그대로 둔다
    private void OnCancelRetry()
    {
        DialogPopup.Close();
    }

    private void OnCloseAlert()
    {
        DialogPopup.Close();
    }

    // 서버가 확정한 최종 재화와 유물 스냅샷을 연출 시작 전에 한 번만 반영한다.
    private void ApplyPurchaseResult(ShopApi.PurchaseResponse response)
    {
        User.Currencies.SyncFrom(response.currencies);
        ApplyChangedArtifacts(response.changedArtifacts);
        GameSession.Instance.NotifyChangeCurrencies();
        SoundManager.Instance.PlaySfx(GameConstants.Sounds.ShopPurchase);
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

            ownedArtifact.SyncFrom(changedArtifact.ToUserArtifact());
        }
    }

    // 재화는 ApplyPurchaseResult에서 이미 반영했다. 보상 중 골드와 다이아는 날아가서 닿을 때까지 탑UI 숫자에 더하지 않는다
    private void HoldCurrencyRewards(System.Collections.Generic.IReadOnlyList<RewardResponse> rewards)
    {
        foreach (RewardResponse reward in rewards)
        {
            switch (reward.rewardType)
            {
                case RewardType.Gold:
                    lobbyController.HoldCurrency(CurrencyType.Gold, reward.amount);
                    break;
                case RewardType.Diamond:
                    lobbyController.HoldCurrency(CurrencyType.Diamond, reward.amount);
                    break;
            }
        }
    }

    // HoldCurrencyRewards로 붙잡아 둔 골드와 다이아를 탑UI 재화 아이콘으로 날린다. 출발 위치는 보상 순서(index)로 받는다
    private void PlayCurrencyRewardFly(System.Collections.Generic.IReadOnlyList<RewardResponse> rewards, Func<int, Vector3> getFromPosition)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            RewardResponse reward = rewards[i];
            switch (reward.rewardType)
            {
                case RewardType.Gold:
                    lobbyController.PlayCurrencyFly(CurrencyType.Gold, reward.amount, getFromPosition(i), spriteGold, goldPopFrames);
                    break;
                case RewardType.Diamond:
                    lobbyController.PlayCurrencyFly(CurrencyType.Diamond, reward.amount, getFromPosition(i), spriteDiamond, diamondPopFrames);
                    break;
            }
        }
    }

    private void ShowGachaResult(System.Collections.Generic.IReadOnlyList<RewardResponse> rewards)
    {
        gachaRewards = rewards;
        GachaResultPopup.Setup(rewards, GameDB, GetGachaRewardIcon);
        GachaResultPopup.Open();
    }

    // 닫힘 연출이 시작되기 전에 불리므로 보상 칸이 아직 제자리에 있다
    private void OnCloseGachaResult(BasePopup popup)
    {
        if (gachaRewards == null)
        {
            return;
        }

        System.Collections.Generic.IReadOnlyList<RewardResponse> rewards = gachaRewards;
        gachaRewards = null;
        PlayCurrencyRewardFly(rewards, GachaResultPopup.GetRewardSlotPosition);
    }

    // 유물은 미리 읽은 유물 이미지를, 재화는 재화 아이콘을 쓴다. 유물 이미지가 없으면 기본 이미지를 쓴다
    private Sprite GetGachaRewardIcon(RewardResponse reward)
    {
        if (reward.rewardType == RewardType.Artifact)
        {
            Sprite sprite = ResourceManager.Instance.GetSprite(reward.rewardCode);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return GetRewardIcon(reward.rewardType);
    }

    // 상품 이미지는 대표 보상으로 고른다. 유물은 code 끝의 종류 이름(VITAL_ORB → ORB)으로 찾는다.
    // 골드 뽑기는 골드 상품과 같은 골드 주머니 이미지를 쓴다. 이미지가 정해지지 않았거나 미리 읽은 이미지가 없으면 기본 이미지를 쓴다
    private Sprite GetProductIcon(ShopProductRewardData reward)
    {
        string address = null;
        switch (reward?.rewardType)
        {
            case RewardType.GachaBox when reward.rewardCode == ArtifactGachaBoxCode:
                address = GameConstants.AddressableAddresses.ShopIconArtifactGacha;
                break;
            case RewardType.GachaBox when reward.rewardCode == GoldGachaBoxCode:
                address = GameConstants.AddressableAddresses.ShopIconGold;
                break;
            case RewardType.Gold:
                address = GameConstants.AddressableAddresses.ShopIconGold;
                break;
            case RewardType.Diamond:
                address = GameConstants.AddressableAddresses.ShopIconDiamond;
                break;
            case RewardType.Artifact:
                string kind = reward.rewardCode.Substring(reward.rewardCode.LastIndexOf('_') + 1);
                address = GameConstants.AddressableAddresses.ShopIconArtifactPrefix + kind;
                break;
        }

        Sprite sprite = ResourceManager.Instance.GetSprite(address);
        return sprite != null
            ? sprite
            : spriteFallback;
    }

    // 구매 확인 문구에 쓰는 재화 이름. 무료 상품은 확인 없이 사므로 쓰이지 않는다
    private static string GetPriceName(PriceType priceType)
    {
        string name = string.Empty;
        switch (priceType)
        {
            case PriceType.Gold:
                name = "골드";
                break;
            case PriceType.Diamond:
                name = "다이아";
                break;
        }

        return name;
    }

    // 무료 상품은 가격 아이콘 없이 FREE만 보여 주므로 null을 돌려준다
    private Sprite GetCurrencyIcon(PriceType priceType)
    {
        Sprite icon = spriteFallback;
        switch (priceType)
        {
            case PriceType.Free:
                icon = null;
                break;
            case PriceType.Gold:
                icon = spriteGold;
                break;
            case PriceType.Diamond:
                icon = spriteDiamond;
                break;
        }

        return icon;
    }

    private Sprite GetRewardIcon(RewardType rewardType)
    {
        Sprite icon = spriteFallback;
        switch (rewardType)
        {
            case RewardType.Gold:
                icon = spriteGold;
                break;
            case RewardType.Diamond:
                icon = spriteDiamond;
                break;
        }

        return icon;
    }

    // 정해진 이름이 없는 카테고리는 enum 이름을 그대로 쓴다
    private static string GetCategoryLabel(ShopCategory category)
    {
        string label = category.ToString();
        switch (category)
        {
            case ShopCategory.Gold:
                label = "골드";
                break;
            case ShopCategory.Diamond:
                label = "다이아";
                break;
            case ShopCategory.Gacha:
                label = "가챠";
                break;
            case ShopCategory.Artifact:
                label = "아티팩트";
                break;
        }

        return label;
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
