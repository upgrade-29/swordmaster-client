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

        gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);
        user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);
    }

    public void NotifyChangeCurrencies()
    {
        OnChangeCurrenciesEvent.Invoke();
    }
}
