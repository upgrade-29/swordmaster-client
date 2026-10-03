using System;
using System.Collections.Generic;
using System.Linq;

// 서버가 없기 때문에 임시 코드. 전투 결과를 계산하고 유저에게 보상반영.
public class BattleProcessor
{
    private readonly BattleDB battleDB;
    private readonly BattleSimulator simulator;

    public BattleProcessor(GameDB gameDB, Random random)
    {
        battleDB = new BattleDB(gameDB);
        simulator = new BattleSimulator(battleDB, random);
    }

    // 도전할 수 없으면 InvalidOperationException을 던진다
    public BattleResult Process(User user, int stageNumber, DateTime utcNow)
    {
        StageProgress progress = user.StageProgress;
        if (progress.CanBattle(utcNow) == false)
            throw new InvalidOperationException($"Battle is on cooldown until {progress.NextBattleAvailableAt:o}");

        // 이미 깬 스테이지는 다시 도전할 수 있고, 아직 열리지 않은 스테이지는 도전할 수 없다
        if (stageNumber < 1 || stageNumber > progress.NextStage)
            throw new InvalidOperationException($"Stage {stageNumber} is locked. Next stage is {progress.NextStage}");

        StageData stage = battleDB.GetStage(stageNumber);
        SwordData sword = battleDB.GetSword(user.Sword.Level);
        IEnumerable<(ArtifactData data, int level)> equippedArtifacts = user.EquippedArtifacts
            .Select(artifact => (battleDB.GetArtifact(artifact.ArtifactCode), artifact.Level));
        CombatStat playerStat = StatCalculator.CalculatePlayer(sword, equippedArtifacts);
        (IReadOnlyList<BattleWave> waves, bool isVictory) = simulator.SimulateStage(playerStat, stage);

        // 패배해도 그때까지 처치한 적의 골드는 준다 (처치하지 못한 웨이브의 gold는 0)
        var gold = waves.Sum(wave => wave.gold);
        user.Currencies.Add(CurrencyType.Gold, gold);

        if (isVictory && stageNumber > progress.ClearedStage)
            progress.SetCleared(stageNumber);

        // 연출이 끝나기 전에 다시 요청하는 것을 막는다 (API 연속 호출로 골드를 복사하는 것 방지)
        var battleTime = waves.Sum(wave => wave.duration);
        progress.SetNextBattleAvailableAt(utcNow.AddSeconds(battleTime + battleDB.cooldownBuffer));

        // TODO: 아티팩트 드랍 시점이 정해지면 여기서 드랍하고, 바뀐 아티팩트를 changedArtifacts에 담는다
        var droppedArtifactCodes = new List<string>();
        var changedArtifacts = new List<UserArtifact>();

        // 응답은 유저 객체와 따로 존재해야 하므로 복사해서 담는다 (서버에서는 JSON으로 직렬화되는 부분)
        var currencies = new UserCurrencies(user.Currencies.Gold, user.Currencies.Diamond);
        var stageProgress = new StageProgress(progress.ClearedStage, progress.NextBattleAvailableAt);

        return new BattleResult(stageNumber, isVictory, playerStat, waves,
            new BattleRewards(gold, droppedArtifactCodes), currencies, stageProgress, changedArtifacts, utcNow);
    }
}
