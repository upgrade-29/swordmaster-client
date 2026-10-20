using System;
using System.Collections.Generic;
using UnityEngine;

// 보고 있는 스테이지를 정하고, 상태(클리어/도전 가능/잠김)를 계산해 뷰에 넘긴다
// 입장은 이벤트로만 알린다. 씬을 여는 일은 이 이벤트를 구독하는 쪽이 한다
public class StageSelectController : MonoBehaviour
{
    public event Action<int> StageEntered; // 입장하려는 스테이지 번호

    // 전투 씬에 다녀오거나 게임을 다시 켜도 마지막으로 고른 스테이지를 보여주려고 PlayerPrefs에 둔다. 없으면 0이다
    private const string LastStagePrefsKey = "LastStage";
    // 지난번에 본 클리어 진행도. 이보다 늘었으면 새로 해금된 스테이지를 먼저 보여준다
    private const string LastClearedStagePrefsKey = "LastClearedStage";

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

        // 새로 해금된 스테이지가 있으면 그것을, 아니면 마지막으로 고른 스테이지를, 둘 다 없으면 도전할 스테이지를 보여준다
        // 도전할 스테이지는 전부 깼으면 마지막 스테이지다
        int nextIndex = Mathf.Clamp(progress.NextStage - 1, 0, stages.Count - 1);
        int lastIndex = FindIndex(PlayerPrefs.GetInt(LastStagePrefsKey, 0));
        if (IsNewlyUnlocked() || lastIndex < 0)
        {
            index = nextIndex;
            RememberStage(stages[index].stage);
        }
        else
        {
            index = lastIndex;
        }

        PlayerPrefs.SetInt(LastClearedStagePrefsKey, progress.ClearedStage);
        PlayerPrefs.Save();
        Refresh();
    }

    // 지난번에 본 뒤로 클리어 진행도가 늘었는지 본다. 저장된 값이 없으면 비교할 수 없어 새로 해금된 걸로 치지 않는다
    private bool IsNewlyUnlocked()
    {
        if (PlayerPrefs.HasKey(LastClearedStagePrefsKey) == false)
            return false;

        return progress.ClearedStage > PlayerPrefs.GetInt(LastClearedStagePrefsKey);
    }

    // 실제로 들어간 스테이지를 기억한다. 이어하기로 다른 스테이지에 들어갈 수도 있어 입장하는 쪽이 알려준다
    // 모바일에서는 앱이 종료 콜백 없이 죽을 수 있어 고를 때마다 바로 디스크에 쓴다
    public static void RememberStage(int stage)
    {
        PlayerPrefs.SetInt(LastStagePrefsKey, stage);
        PlayerPrefs.Save();
    }

    private int FindIndex(int stage)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stage == stage)
                return i;
        }

        return -1;
    }

    // 보고 있는 스테이지는 그대로 두고 내 전투력만 바꾼다
    public void SetMyPower(long myPower)
    {
        this.myPower = myPower;
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
        RememberStage(stages[index].stage);
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
