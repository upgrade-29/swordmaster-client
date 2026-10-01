using System;
using System.Collections.Generic;
using UnityEngine;

// 보고 있는 스테이지를 정하고, 상태(클리어/도전 가능/잠김)를 계산해 뷰에 넘긴다
// 입장은 이벤트로만 알린다. 씬을 여는 일은 이 이벤트를 구독하는 쪽이 한다
public class StageSelectController : MonoBehaviour
{
    public event Action<int> StageEntered; // 입장하려는 스테이지 번호

    private StageSelectView view;

    private IReadOnlyList<StageData> stages;
    private StageProgress progress;
    private long myPower;
    private int index; // stages에서 지금 보고 있는 위치

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref view);

        view.PrevClicked += OnPrevClicked;
        view.NextClicked += OnNextClicked;
        view.StartClicked += OnStartClicked;
    }

    private void OnDestroy()
    {
        if (view == null)
            return;

        view.PrevClicked -= OnPrevClicked;
        view.NextClicked -= OnNextClicked;
        view.StartClicked -= OnStartClicked;
    }

    // stages는 스테이지 번호 순서로 정렬되어 있어야 한다
    public void Init(IReadOnlyList<StageData> stages, StageProgress progress, long myPower)
    {
        this.stages = stages;
        this.progress = progress;
        this.myPower = myPower;

        // 처음에는 도전할 스테이지를 보여준다. 전부 깼으면 마지막 스테이지다
        index = Mathf.Clamp(progress.NextStage - 1, 0, stages.Count - 1);
        Refresh();
    }

    private void Refresh()
    {
        StageData stage = stages[index];
        view.Show(stage, GetState(stage.stage), myPower);
        view.SetArrowsInteractable(index > 0, index < stages.Count - 1);
    }

    private StageState GetState(int stage)
    {
        if (stage <= progress.ClearedStage)
            return StageState.Cleared;

        return stage == progress.NextStage ? StageState.Available : StageState.Locked;
    }

    private void Move(int delta)
    {
        index = Mathf.Clamp(index + delta, 0, stages.Count - 1);
        Refresh();
    }

    private void OnPrevClicked() => Move(-1);
    private void OnNextClicked() => Move(1);

    private void OnStartClicked()
    {
        StageData stage = stages[index];
        if (GetState(stage.stage) == StageState.Locked)
            return;

        StageEntered?.Invoke(stage.stage);
    }
}
