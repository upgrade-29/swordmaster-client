using TMPro;
using UnityEngine;

// 화면 상단의 스테이지, 남은 시간, HP바 표시
public class BattleHudView : MonoBehaviour
{
    private TMP_Text stageText;
    private TMP_Text timeText;
    private HpBarView playerHpBar;
    private HpBarView enemyHpBar;

    public HpBarView PlayerHpBar => playerHpBar;
    public HpBarView EnemyHpBar => enemyHpBar;

    private void Awake()
    {
        stageText = GameUtil.Bind<TMP_Text>(gameObject, "StageText");
        timeText = GameUtil.Bind<TMP_Text>(gameObject, "TimeText");
        playerHpBar = GameUtil.Bind<HpBarView>(gameObject, "PlayerHpBar");
        enemyHpBar = GameUtil.Bind<HpBarView>(gameObject, "EnemyHpBar");
    }

    // subStageNumber는 1부터 센다. 스테이지 1의 두 번째 적이면 "Stage 1-2"
    public void SetStage(int stage, int subStageNumber, bool isBoss)
    {
        stageText.text = isBoss ? $"Stage {stage}-{subStageNumber} BOSS" : $"Stage {stage}-{subStageNumber}";
    }

    // 전투 시작 전에 남은 시간 자리에 보여주는 안내. 소수는 올려서 "2s"부터 "1s"까지 센다
    public void SetStartCountdown(double seconds)
    {
        timeText.text = $"{System.Math.Ceiling(seconds):0}s 뒤에 전투가 시작됩니다!";
    }

    public void SetRemainingTime(double seconds)
    {
        timeText.text = $"{System.Math.Max(0, seconds):0.0}s";
    }
}
