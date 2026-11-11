using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로딩 씬 화면. SceneLoader가 넘겨 주는 진행률로 진행 바와 문구를 갱신한다
// 지금 강화 단계의 검을 띄워 둥실거리게 한다. 로그인하지 않았으면(로그아웃, 세션 만료) 1단계 검을 보여 준다
public class LoadingView : MonoBehaviour
{
    // 로딩할 때마다 하나를 고른다. 수치는 지금 게임 데이터 기준이라 데이터가 바뀌면 함께 고친다
    private static readonly string[] Tips =
    {
        "강화에 실패하면 검이 파괴되고 망가진 나무 검부터 다시 시작합니다.",
        "강화 단계가 오를수록 성공 확률은 낮아지고 비용은 늘어납니다.",
        "위험한 단계에 오르기 전에 검을 팔아 골드를 챙기는 것도 방법입니다.",
        "검을 팔면 판매 가격만큼 골드를 받고 망가진 나무 검으로 돌아갑니다.",
        "7강 이상 강화에 성공하면 모든 플레이어에게 소식이 전해집니다.",
        "적을 쓰러뜨릴 때마다 최대 체력의 10%를 회복합니다.",
        "제한 시간 안에 적을 쓰러뜨리지 못하면 전투에서 패배합니다.",
        "스테이지의 마지막에는 보스가 기다리고 있습니다.",
        "추천 전투력보다 낮다면 검을 강화하거나 아티팩트를 장착해 보세요.",
        "치명타가 터지면 1.5배의 피해를 줍니다.",
        "전투 도중에 나가도 다음에 들어올 때 이어서 진행할 수 있습니다.",
        "아티팩트는 최대 3개까지 장착할 수 있습니다.",
        "같은 아티팩트를 또 얻으면 강화 재료로 쌓입니다.",
        "스테이지에서 승리하면 일정 확률로 아티팩트를 얻습니다.",
        "상점의 무료 다이아를 잊지 말고 받아 가세요.",
        "랭킹에서 검 강화 단계와 전투력 순위를 확인할 수 있습니다.",
    };

    private static int lastTipIndex = -1; // 바로 전 로딩에서 보여 준 팁. 로딩 씬은 매번 새로 열리므로 static으로 기억한다

    [ReadOnly] [SerializeField] private RectTransform rectSword;
    [ReadOnly] [SerializeField] private Image imgSword;
    [ReadOnly] [SerializeField] private Image imgProgressFill;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtProgress;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtTip;

    private void Awake()
    {
        rectSword = GameUtil.Bind<RectTransform>(transform, "Sword");
        imgSword = GameUtil.Bind<Image>(transform, "Sword");
        imgProgressFill = GameUtil.Bind<Image>(transform, "ProgressBar/Fill");
        txtProgress = GameUtil.Bind<TextMeshProUGUI>(transform, "ProgressText");
        txtTip = GameUtil.Bind<TextMeshProUGUI>(transform, "TipText");
    }

    // 로딩 중에는 BGM을 멈춘다. 다음 씬이 시작할 때 자기 BGM을 처음부터 튼다
    private void Start()
    {
        SoundManager.Instance.StopBgm();
        ShowRandomTip();
        SetSwordSprite();
        PlaySwordBob();
    }

    public void SetProgress(float progress)
    {
        imgProgressFill.fillAmount = progress;
        txtProgress.text = $"불러오는 중 {Mathf.FloorToInt(progress * 100f).ToString()}%";
    }

    // 바로 전 팁을 뺀 나머지에서 고른다. 전 팁보다 뒤 번호가 나오면 한 칸 밀어서 전 팁을 건너뛴다
    private void ShowRandomTip()
    {
        int index;
        if (lastTipIndex < 0)
        {
            index = Random.Range(0, Tips.Length);
        }
        else
        {
            index = Random.Range(0, Tips.Length - 1);
            if (index >= lastTipIndex)
            {
                index++;
            }
        }

        lastTipIndex = index;
        txtTip.text = Tips[index];
    }

    private void SetSwordSprite()
    {
        GameSession session = GameSession.Instance;
        int level = session.User?.Sword.Level ?? session.GameDB.swords.Min(x => x.level);
        SwordData sword = session.GameDB.swords.First(x => x.level == level);

        Sprite sprite = ResourceManager.Instance.GetSprite(sword.code);
        imgSword.sprite = sprite;
        imgSword.enabled = sprite != null;
    }

    // 놓인 자리를 가운데로 위아래 같은 높이만큼 오간다
    private void PlaySwordBob()
    {
        float baseY = rectSword.anchoredPosition.y;
        DOVirtual.Float(0f, Mathf.PI * 2f, GameConstants.LoadingSword.BobPeriod, phase =>
            {
                rectSword.anchoredPosition = new Vector2(rectSword.anchoredPosition.x,
                    baseY + Mathf.Sin(phase) * GameConstants.LoadingSword.BobHeight);
            })
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
