using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 상단의 스테이지, 남은 시간, HP바 표시와 전투 시작 버튼
public class BattleHudView : MonoBehaviour
{
    private TMP_Text stageText;
    private TMP_Text timeText;
    private HpBarView playerHpBar;
    private HpBarView enemyHpBar;
    private Button startButton;

    public HpBarView PlayerHpBar => playerHpBar;
    public HpBarView EnemyHpBar => enemyHpBar;
    public Button StartButton => startButton;

    private void Awake()
    {
        stageText = GameUtil.Bind<TMP_Text>(gameObject, "StageText");
        timeText = GameUtil.Bind<TMP_Text>(gameObject, "TimeText");
        playerHpBar = GameUtil.Bind<HpBarView>(gameObject, "PlayerHpBar");
        enemyHpBar = GameUtil.Bind<HpBarView>(gameObject, "EnemyHpBar");
        startButton = GameUtil.Bind<Button>(gameObject, "StartButton");
    }

    // wave는 1부터 센다. 스테이지 1의 두 번째 적이면 "Stage 1-2"
    public void SetStage(int stage, int wave, bool isBoss)
    {
        stageText.text = isBoss ? $"Stage {stage}-{wave} BOSS" : $"Stage {stage}-{wave}";
    }

    public void SetRemainingTime(double seconds)
    {
        timeText.text = $"{System.Math.Max(0, seconds):0.0}s";
    }
}
