using DG.Tweening;
using UnityEngine;

// 게임 전체에서 같은 의미로 쓰는 상수. 한 화면의 연출 세부값은 각 파일에 둔다
public static class GameConstants
{
    public static class Scenes
    {
        public const string Title = "TitleScene";
        public const string Lobby = "LobbyScene";
        public const string Battle = "BattleScene";
    }

    // 팝업과 능력치 패널이 같이 쓰는 열림/닫힘 연출
    public static class PopupAnimation
    {
        public const float OpenDuration = 0.3f;      // 열림 연출 시간(초)
        public const float OpenStartScale = 0.8f;    // 열릴 때 시작 크기(원래 크기에 대한 비율)
        public const Ease OpenScaleEase = Ease.OutBack; // 열릴 때 크기 변화. 페이드는 DOTween 기본 Ease를 쓴다
        public const float CloseDuration = 0.15f;    // 닫힘 연출 시간(초)
        public const float CloseEndScale = 0.9f;     // 닫힐 때 끝 크기(원래 크기에 대한 비율)
        public const Ease CloseEase = Ease.InQuad;   // 닫힐 때 페이드와 크기 변화
    }

    // 아트 스타일 가이드의 픽셀 규격. 이미지는 원본의 정수 배율로만 키운다
    public static class ArtPixel
    {
        public const int UiScale = 4; // 아트 픽셀 1칸 = UI 4유닛(캔버스 1080x1920)
    }

    public static class Rules
    {
        public const int ArtifactSlotCount = 3;
        public const float MaxBattleSpeed = 2f; // 서버 시간 검증과 같은 배속 상한
    }

    // 데미지 숫자 크기와 체력바 흔들림이 함께 쓰는 전투 연출 기준
    public static class BattleEffect
    {
        public const float StrongHitDamageRatio = 0.5f; // 최대 체력 대비 이 비율 이상을 잃으면 가장 세게 맞은 것으로 본다
    }

    // 날아온 재화가 재화 표시에 닿을 때 아이콘과 숫자가 한 번 튀는 연출. 전투 골드와 로비 상단 재화가 함께 쓴다
    public static class CurrencyArrive
    {
        public const float IconPunchScale = 1.15f;
        public const float TextPunchScale = 1.1f;
        public const float PunchDuration = 0.15f; // 1배속 기준
    }

    public static class NumberFormats
    {
        public const string Integer = "N0";     // 1,234
        public const string Decimal = "#,0.##"; // 1,234.5
    }

    public static class ResourcePaths
    {
        public const string GameDB = "Data/game_db"; // 게임 데이터는 아직 서버에서 받지 않아 시트에서 뽑은 파일을 읽는다
        public const string DamageFont = "TMP/Galmuri11-Bold_Outline SDF";
    }

    // Resources/Sound 아래 클립 경로
    public static class Sounds
    {
        public const string BgmLobby = "Sound/BGM/lobby";
        public const string BgmBattle = "Sound/BGM/battle";

        public static readonly string[] Swings =
        {
            "Sound/SFX/sfx_battle_swing_01",
            "Sound/SFX/sfx_battle_swing_02",
            "Sound/SFX/sfx_battle_swing_03",
        };
        public static readonly string[] Hits =
        {
            "Sound/SFX/sfx_battle_hit_01",
            "Sound/SFX/sfx_battle_hit_02",
            "Sound/SFX/sfx_battle_hit_03",
        };
        public const string HitCrit = "Sound/SFX/sfx_battle_hit_crit";
        public const string Knockout = "Sound/SFX/sfx_battle_knockout";
        public const string Heal = "Sound/SFX/sfx_battle_heal";
        public const string Countdown = "Sound/SFX/sfx_battle_countdown";
        public const string TimeWarning = "Sound/SFX/sfx_battle_countdown"; // TODO: [김우진] 남은 시간 경고 전용 효과음이 생기면 경로를 바꾼다
        public const string BattleStart = "Sound/SFX/sfx_battle_start";
        public const string BossWarning = "Sound/SFX/sfx_battle_boss_warning";
        public const string EnemyEntrance = "Sound/SFX/sfx_battle_enemy_entrance";
        public const string Coin = "Sound/SFX/sfx_battle_coin";

        public const string EnhanceCharge = "Sound/SFX/sfx_enhance_charge";
        public const string EnhanceSuccess = "Sound/SFX/sfx_enhance_success";
        public const string EnhanceFail = "Sound/SFX/sfx_enhance_fail";
        public const string EnhanceSell = "Sound/SFX/sfx_enhance_sell";
        public const string ArtifactEquip = "Sound/SFX/sfx_artifact_equip";

        public const string ShopPurchase = "Sound/SFX/sfx_shop_purchase";
        public const string GachaReveal = "Sound/SFX/sfx_shop_gacha_reveal";
        public const string GachaRevealRare = "Sound/SFX/sfx_shop_gacha_reveal_rare";

        public const string UiClick = "Sound/SFX/sfx_ui_click";
        public const string UiPopupOpen = "Sound/SFX/sfx_ui_popup_open";
        public const string UiPopupClose = "Sound/SFX/sfx_ui_popup_close";
        public const string UiError = "Sound/SFX/sfx_ui_error";
        public const string TitleTouch = "Sound/SFX/sfx_title_touch";
    }

    public static class AddressableLabels
    {
        public const string Preload = "preload"; // 시작할 때 타이틀 화면에서 한 번에 읽어 두는 리소스
    }

    // 게임 데이터 code로 찾지 않는 고정 이미지의 Addressables 주소
    public static class AddressableAddresses
    {
        public const string ShopHeader = "SHOP_HEADER";

        // 상점 상품 카드 이미지. 유물 이미지는 유물 code 끝의 종류 이름(ORB 등)을 붙여 찾는다
        public const string ShopIconGold = "SHOP_ICON_GOLD";
        public const string ShopIconDiamond = "SHOP_ICON_DIAMOND";
        public const string ShopIconArtifactPrefix = "SHOP_ICON_ARTIFACT_";
        public const string ShopIconArtifactGacha = "SHOP_ICON_ARTIFACT_GACHA";

        // 전투 배경 이미지. 스테이지 이미지 code 뒤에 붙이고 층 이름을 단다(STAGE_01_BG_FAR)
        public const string StageBackgroundInfix = "_BG_";
        public const string StageBackgroundFar = "FAR";
        public const string StageBackgroundMid = "MID";
        public const string StageBackgroundGround = "GROUND";
    }

    // Color는 const가 될 수 없어서 static readonly로 둔다
    public static class Colors
    {
        public static readonly Color TextPrimary = new Color32(0xE8, 0xE6, 0xDC, 0xFF);
        public static readonly Color TextSecondary = new Color32(0xB4, 0xB2, 0xA8, 0xFF);

        public static readonly Color EnhanceSuccess = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
        public static readonly Color EnhanceFail = new Color32(0xFF, 0x66, 0x66, 0xFF);
        public static readonly Color EnhanceFailGlow = new Color32(0x60, 0x60, 0x60, 0x00);
        public static readonly Color EnhanceFailSword = new Color32(0x60, 0x60, 0x60, 0xFF);

        public static readonly Color BattleVictory = new Color(0.75f, 0.5f, 0f);
        public static readonly Color BattleDefeat = new Color(0.75f, 0.15f, 0.15f);

        public static readonly Color EnemyDamage = Color.white;
        public static readonly Color PlayerDamage = new Color(1f, 0.4f, 0.4f);
        public static readonly Color CritDamage = new Color(1f, 0.85f, 0.2f);
        public static readonly Color CritLabel = new Color32(0xFF, 0x8A, 0x3D, 0xFF); // 치명타 숫자 위의 CRITICAL 글자
        public static readonly Color UnitHit = new Color(1f, 0.35f, 0.35f);
        public static readonly Color Heal = new Color(0.4f, 1f, 0.5f); // 회복 깜빡임과 생명력 흡수 숫자

        public static readonly Color StatIncrease = new Color32(0x5B, 0xE3, 0x7A, 0xFF); // 강화 탭에서 전투력과 능력치가 오를 때
        public static readonly Color StatDecrease = new Color32(0xFF, 0x5A, 0x5A, 0xFF); // 강화 탭에서 전투력과 능력치가 내릴 때
    }

    // 검 강화 단계 글자(+3)의 재질 구간별 색. TMP 리치 텍스트 태그에 넣는 16진수 값이다
    public static class SwordLevelColors
    {
        public const string Wood = "E8E6DC";       // 0~4 나무 검
        public const string Iron = "7CD67C";       // 5~10 철 검, 기사의 검
        public const string NobleMetal = "5AB4FF"; // 11~13 플래티넘, 묵금, 룬
        public const string Flame = "C47CFF";      // 14~18 불꽃, 마그마
        public const string Frost = "FFA94D";      // 19~24 냉기, 심해, 일식
        public const string Celestial = "FF5A5A";  // 25~30 운석, 은하, 성검
    }
}
