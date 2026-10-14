using System;
using System.Collections.Generic;

// 가챠 결과의 순서를 보존하고 사용자 입력마다 한 보상씩 표시하도록 제어한다.
public class GachaResultPresenter
{
    private readonly Action<ShopApi.RewardResponse, int, int> showReward;
    private readonly Action closePopup;

    private IReadOnlyList<ShopApi.RewardResponse> rewards;
    private Action completed;
    private int currentIndex;

    public bool IsPlaying => rewards != null;
    public int CurrentIndex => currentIndex;

    public GachaResultPresenter(Action<ShopApi.RewardResponse, int, int> showReward, Action closePopup)
    {
        this.showReward = showReward ?? throw new ArgumentNullException(nameof(showReward));
        this.closePopup = closePopup ?? throw new ArgumentNullException(nameof(closePopup));
    }

    // 빈 결과와 중복 시작은 거부하고, 서버가 준 배열의 첫 보상부터 그대로 표시한다.
    public bool Play(IReadOnlyList<ShopApi.RewardResponse> resultRewards, Action onCompleted = null)
    {
        if (IsPlaying == true || resultRewards == null || resultRewards.Count == 0)
        {
            return false;
        }

        rewards = resultRewards;
        completed = onCompleted;
        currentIndex = 0;
        ShowCurrentReward();
        return true;
    }

    // 사용자의 다음 입력으로만 인덱스를 전진시키고 마지막 보상 뒤에는 표시를 종료한다.
    public void Advance()
    {
        if (IsPlaying == false)
        {
            return;
        }

        currentIndex++;
        if (currentIndex >= rewards.Count)
        {
            Complete();
            return;
        }

        ShowCurrentReward();
    }

    // 화면 이탈 시 연출만 중단하며 이미 반영한 사용자 상태는 되돌리지 않는다.
    public void Cancel()
    {
        if (IsPlaying == false)
        {
            return;
        }

        rewards = null;
        completed = null;
        closePopup.Invoke();
    }

    private void ShowCurrentReward()
    {
        showReward.Invoke(rewards[currentIndex], currentIndex, rewards.Count);
    }

    private void Complete()
    {
        Action onCompleted = completed;
        rewards = null;
        completed = null;
        closePopup.Invoke();
        onCompleted?.Invoke();
    }
}
