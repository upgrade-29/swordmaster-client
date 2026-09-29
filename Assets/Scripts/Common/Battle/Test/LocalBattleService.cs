using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

// 서버가 없기 때문에 임시 코드. 서버 대신 클라이언트 안에서 BattleProcessor를 실행한다
public class LocalBattleService : IBattleService
{
    private readonly User user;
    private readonly BattleProcessor processor;

    // user는 서버 DB에 있는 유저 역할이다. 전투 결과가 이 객체에 반영된다
    public LocalBattleService(GameDB gameDB, User user, Random random)
    {
        this.user = user;
        processor = new BattleProcessor(gameDB, random);
    }

    public async Task<BattleResult> RequestBattleAsync(int stageNumber)
    {
        // 진짜 통신처럼 요청한 즉시가 아니라 나중에 결과가 오게 한다
        await Task.Yield();

        BattleResult result;
        try
        {
            result = processor.Process(user, stageNumber, DateTime.UtcNow);
        }
        catch (InvalidOperationException e)
        {
            throw new BattleRequestException(e.Message);
        }

        // 서버 응답처럼 JSON을 거쳐서 넘긴다. 서버 쪽 객체와 공유하는 부분이 남지 않는다
        string json = JsonConvert.SerializeObject(result);
        return JsonConvert.DeserializeObject<BattleResult>(json);
    }
}
