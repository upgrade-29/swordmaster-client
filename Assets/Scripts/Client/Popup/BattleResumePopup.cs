using System;
using TMPro;
using UnityEngine;

// 진행 중이던 전투가 있을 때 전투에 들어가기 전에 뜨는 팝업. 받은 요약을 보여주고 버튼 입력을 이벤트로 알리기만 한다
// ConfirmPopup의 확인/취소를 이어하기/새로 시작으로 쓴다
public class BattleResumePopup : ConfirmPopup
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI continueLabelText;
    [ReadOnly] [SerializeField] private TextMeshProUGUI restartLabelText;

    public event Action ContinueClicked;
    public event Action RestartClicked;
    public event Action CancelClicked; // 바깥을 눌러 닫았을 때. 아무것도 고르지 않고 로비에 남는다

    protected override void Awake()
    {
        base.Awake();

        continueLabelText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Footer/ConfirmButton/Label");
        restartLabelText = GameUtil.Bind<TextMeshProUGUI>(transform, "Panel/Footer/CancelButton/Label");
    }

    // subStageNumber는 1부터 센다
    public void Show(int stage, int subStageNumber, int subStageCount, double playerHp, double maxHp, long gold)
    {
        var message = "진행 중인 전투가 있습니다. 이어하시겠습니까?\n\n" +
                      $"스테이지 {stage} ({subStageNumber}/{subStageCount})\n" +
                      $"체력 {playerHp:#,0} / {maxHp:#,0}\n" +
                      $"모은 골드 {gold:N0}\n\n" +
                      "새로 시작하면 진행 중이던 전투는 포기(패배) 처리되고 모은 골드만 받습니다.";

        Setup("진행 중인 전투", message, OnClickContinue, OnClickRestart);
        Open();
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        continueLabelText.text = "이어하기";
        restartLabelText.text = "새로 시작";
    }

    protected override void OnClickDimmed()
    {
        base.OnClickDimmed();

        if (IsOpened == false)
        {
            CancelClicked?.Invoke();
        }
    }

    private void OnClickContinue() => ContinueClicked?.Invoke();
    private void OnClickRestart() => RestartClicked?.Invoke();
}
