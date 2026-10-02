using Newtonsoft.Json;
using UnityEngine;

public class TestLobbyDataLoader : SingletonMonoBehaviour<TestLobbyDataLoader>
{
    [ReadOnly(true)] [SerializeField] private string userFile = "rich"; // rich, high_sword, full_artifacts, poor

    private GameDB gameDB;
    public GameDB GameDB => gameDB;

    private User user;
    public User User => user;

    protected override void OnAwakeSingleton()
    {
        gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);
        user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);

        FindAnyObjectByType<EnhanceTab>().Init(gameDB, user);
    }
}
