using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로딩 씬 화면. SceneLoader가 넘겨 주는 진행률로 진행 바와 문구를 갱신한다
// 지금 강화 단계의 검을 띄워 둥실거리게 한다. 로그인하지 않았으면(로그아웃, 세션 만료) 1단계 검을 보여 준다
public class LoadingView : MonoBehaviour
{
    // TODO: [김우진] 팁 문구가 정해지면 여러 개 중에서 고르게 바꾼다
    private const string TipText = "팁 문구 자리입니다. 실제 문구는 나중에 정합니다.";

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
        txtTip.text = TipText;
        SetSwordSprite();
        PlaySwordBob();
    }

    public void SetProgress(float progress)
    {
        imgProgressFill.fillAmount = progress;
        txtProgress.text = $"불러오는 중 {Mathf.FloorToInt(progress * 100f).ToString()}%";
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
