using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// 서버가 없기 때문에 임시 코드. 전투 계산에 필요한 GameDB 데이터를 한 번만 준비해 둔다
// 목록은 키로 바로 찾을 수 있게 Dictionary로 만들고, config 값도 여기서 한 번만 읽는다
public class BattleDB
{
    // config에 값이 아직 없을 때 쓰는 기본값
    private const double DefaultCritMultiplier = 1.5;
    private const double DefaultHealRate = 0;
    private const double DefaultNormalTimeLimit = 30;
    private const double DefaultBossTimeLimit = 60;
    private const double DefaultCooldownBuffer = 2;

    public readonly double critMultiplier;
    public readonly double healRate;
    public readonly double normalTimeLimit;
    public readonly double bossTimeLimit;
    public readonly double cooldownBuffer;

    private readonly Dictionary<int, SwordData> swordsByLevel;
    private readonly Dictionary<int, StageData> stagesByNumber;
    private readonly Dictionary<string, ArtifactData> artifactsByCode;
    private readonly Dictionary<string, EnemyData> enemiesByCode;

    public BattleDB(GameDB gameDB)
    {
        // 키가 중복된 데이터가 있으면 여기서 ArgumentException이 난다
        swordsByLevel = gameDB.swords.ToDictionary(data => data.level);
        stagesByNumber = gameDB.stages.ToDictionary(data => data.stage);
        artifactsByCode = gameDB.artifacts.ToDictionary(data => data.code);
        enemiesByCode = gameDB.enemies.ToDictionary(data => data.code);

        Dictionary<string, string> configs = gameDB.config.ToDictionary(data => data.key, data => data.value);
        critMultiplier = GetConfig(configs, "CRIT_MULTIPLIER", DefaultCritMultiplier);
        healRate = GetConfig(configs, "HEAL_RATE", DefaultHealRate);
        normalTimeLimit = GetConfig(configs, "NORMAL_BATTLE_TIME_LIMIT", DefaultNormalTimeLimit);
        bossTimeLimit = GetConfig(configs, "BOSS_BATTLE_TIME_LIMIT", DefaultBossTimeLimit);
        cooldownBuffer = GetConfig(configs, "BATTLE_COOLDOWN_BUFFER", DefaultCooldownBuffer);
    }

    public SwordData GetSword(int level)
    {
        if (swordsByLevel.TryGetValue(level, out SwordData sword) == false)
            throw new ArgumentException($"Sword not found: level {level}", nameof(level));

        return sword;
    }

    public StageData GetStage(int stageNumber)
    {
        if (stagesByNumber.TryGetValue(stageNumber, out StageData stage) == false)
            throw new ArgumentException($"Stage not found: {stageNumber}", nameof(stageNumber));

        return stage;
    }

    public ArtifactData GetArtifact(string artifactCode)
    {
        if (artifactsByCode.TryGetValue(artifactCode, out ArtifactData artifact) == false)
            throw new ArgumentException($"Artifact not found: {artifactCode}", nameof(artifactCode));

        return artifact;
    }

    public EnemyData GetEnemy(string enemyCode)
    {
        if (enemiesByCode.TryGetValue(enemyCode, out EnemyData enemy) == false)
            throw new ArgumentException($"Enemy not found: {enemyCode}", nameof(enemyCode));

        return enemy;
    }

    private static double GetConfig(Dictionary<string, string> configs, string key, double defaultValue)
    {
        return configs.TryGetValue(key, out string value)
            ? double.Parse(value, CultureInfo.InvariantCulture)
            : defaultValue;
    }
}
