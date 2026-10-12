using System;
using Newtonsoft.Json;
using UnityEngine;

public class TestLobbyDataLoader : SingletonMonoBehaviour<TestLobbyDataLoader>
{
    [ReadOnly(true)] [SerializeField] private string userFile = "rich"; // rich, high_sword, full_artifacts, poor

    private GameDB gameDB;
    public GameDB GameDB => gameDB;

    private User user;
    public User User => user;

    public event Action OnChangeCurrenciesEvent = delegate { };

    protected override void OnAwakeSingleton()
    {
        // 전투 씬에서 돌아왔으면 전투 결과가 반영된 유저를 쓴다
        if (StageSelectEntry.TryConsume(out gameDB, out user))
        {
            return;
        }

        // 게임 데이터는 아직 서버에서 받지 않아 테스트 데이터를 쓴다
        gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);

        // 로그인했으면 서버에서 받은 플레이어를 쓰고, 로그인 없이 로비를 열었으면 테스트 유저를 쓴다
        if (LoginEntry.TryConsume(out user))
        {
            return;
        }

        user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);
    }

    public void NotifyChangeCurrencies()
    {
        OnChangeCurrenciesEvent.Invoke();
    }
}
