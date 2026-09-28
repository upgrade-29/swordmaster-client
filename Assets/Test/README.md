# 테스트 데이터 읽기

테스트 데이터는 `Resources.Load`로 파일을 불러온 다음, `JsonConvert`로 모델로 바꾸면 됩니다.

```csharp
// 게임 데이터
var gameDB = JsonConvert.DeserializeObject<GameDB>(
    Resources.Load<TextAsset>("TestData/game_db").text);

// User 시나리오 (rich, high_sword, full_artifacts, poor 중 하나)
var user = JsonConvert.DeserializeObject<User>(
    Resources.Load<TextAsset>("TestData/User/rich").text);
```
