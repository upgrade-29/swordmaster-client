using Newtonsoft.Json;
using UnityEngine;

public class TestLobbyDataLoader : MonoBehaviour
{
    [ReadOnly(true)] [SerializeField] private string userFile = "rich"; // rich, high_sword, full_artifacts, poor

    private void Awake()
    {
        var gameDB = JsonConvert.DeserializeObject<GameDB>(Resources.Load<TextAsset>("TestData/game_db").text);
        var user = JsonConvert.DeserializeObject<User>(Resources.Load<TextAsset>($"TestData/User/{userFile}").text);

        FindAnyObjectByType<EnhanceTab>().Init(gameDB, user);
    }
}
