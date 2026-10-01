using Newtonsoft.Json;
using UnityEngine;

// 테스트/디버그 전용: 실제 Network Layer 없이 ShopTab을 Play Mode에서 수동으로 구동하기 위한 부트스트랩.
// 프로덕션 LobbyScene.unity에는 추가하지 않는다.
public class ShopPlayModeBootstrap : MonoBehaviour
{
    private enum UserScenario
    {
        Rich,           // 골드 100만/다이아 1만 보유, 검 레벨 3. 재화가 충분해 모든 상점 상품을 구매할 수 있는 상황.
        HighSword,      // 골드 10만/다이아 1천 보유, 검 레벨 9로 높게 강화된 상황.
        FullArtifacts,  // 골드 20만/다이아 1천 보유, 검 레벨 5, 아티팩트 8종을 다양한 레벨/장착 상태로 보유한 상황.
        Poor,           // 골드 50/다이아 0, 검 레벨 0. 재화가 거의 없어 구매 실패(재화 부족) 흐름을 확인하기 위한 상황.
    }

    [SerializeField] private ShopTab shopTab;
    [SerializeField] private UserScenario userScenario = UserScenario.Rich;
    [SerializeField] private FakeShopPurchaseService.Scenario nextPurchaseScenario = FakeShopPurchaseService.Scenario.Success;

    private FakeShopPurchaseService purchaseService;

    private void Start()
    {
        GameDB gameDB = JsonConvert.DeserializeObject<GameDB>(
            Resources.Load<TextAsset>("TestData/game_db").text);

        User user = JsonConvert.DeserializeObject<User>(
            Resources.Load<TextAsset>($"TestData/User/{GetUserScenarioFileName()}").text);

        purchaseService = new FakeShopPurchaseService
        {
            NextScenario = nextPurchaseScenario,
        };

        if (shopTab == null)
        {
            shopTab = FindFirstObjectByType<ShopTab>();
        }

        shopTab.Initialize(gameDB, user, purchaseService);
    }

    private void Update()
    {
        // Inspector에서 값을 바꾸면 다음 구매 요청부터 즉시 반영되도록 매 프레임 동기화한다.
        if (purchaseService != null)
        {
            purchaseService.NextScenario = nextPurchaseScenario;
        }
    }

    private string GetUserScenarioFileName()
    {
        switch (userScenario)
        {
            case UserScenario.Rich: return "rich";
            case UserScenario.HighSword: return "high_sword";
            case UserScenario.FullArtifacts: return "full_artifacts";
            case UserScenario.Poor: return "poor";
            default: throw new System.ArgumentOutOfRangeException();
        }
    }
}
