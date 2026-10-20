using System;
using TMPro;
using UnityEngine;

// 전투 화면의 스테이지, 남은 시간, HP바, 이번 전투에서 모은 골드, 시작 카운트다운을 표시한다
public class BattleHudView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStageName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtSubStage;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtTime;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCountdown;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGold;
    [ReadOnly] [SerializeField] private RectTransform rectGoldIcon;
    [ReadOnly] [SerializeField] private HpBarView objPlayerHpBar;
    [ReadOnly] [SerializeField] private HpBarView objEnemyHpBar;

    public HpBarView PlayerHpBar => objPlayerHpBar;

    public HpBarView EnemyHpBar => objEnemyHpBar;

    // 처치 골드 동전이 빨려 들어갈 자리. 골드 숫자 앞의 아이콘 가운데라 자릿수가 바뀌어도 움직이지 않는다
    public Vector3 GoldIconPosition => rectGoldIcon.position;

    private void Awake()
    {
        txtStageName = GameUtil.Bind<TextMeshProUGUI>(transform, "TopPanel/StageNameText");
        txtSubStage = GameUtil.Bind<TextMeshProUGUI>(transform, "TopPanel/SubStageText");
        txtTime = GameUtil.Bind<TextMeshProUGUI>(transform, "Timer/TimeText");
        txtCountdown = GameUtil.Bind<TextMeshProUGUI>(transform, "CountdownText");
        txtGold = GameUtil.Bind<TextMeshProUGUI>(transform, "GoldText");
        rectGoldIcon = GameUtil.Bind<RectTransform>(transform, "GoldIcon");
        objPlayerHpBar = GameUtil.Bind<HpBarView>(transform, "PlayerHpBar");
        objEnemyHpBar = GameUtil.Bind<HpBarView>(transform, "EnemyHpBar");
    }

    /// <summary>
    /// 스테이지 이름과 세부 스테이지 번호를 표시한다. subStageNumber는 1부터 센다.
    /// </summary>
    public void SetStage(string stageName, int stage, int subStageNumber, bool isBoss)
    {
        txtStageName.text = stageName;

        string subStage = $"{stage.ToString()}-{subStageNumber.ToString()}";
        txtSubStage.text = isBoss
            ? $"{subStage} BOSS"
            : subStage;
    }

    /// <summary>
    /// 전투 시작 전에 화면 가운데에 남은 초를 크게 띄운다. 소수는 올려서 3, 2, 1로 센다.
    /// </summary>
    public void SetStartCountdown(double seconds)
    {
        txtCountdown.gameObject.SetActive(true);
        txtCountdown.text = Math.Ceiling(seconds).ToString("0");
    }

    public void HideStartCountdown()
    {
        txtCountdown.gameObject.SetActive(false);
    }

    public void SetRemainingTime(double seconds)
    {
        txtTime.text = $"{Math.Max(0d, seconds).ToString("0.0")}s";
    }

    public void SetGold(long gold)
    {
        txtGold.text = gold.ToString(GameConstants.NumberFormats.Integer);
    }
}
