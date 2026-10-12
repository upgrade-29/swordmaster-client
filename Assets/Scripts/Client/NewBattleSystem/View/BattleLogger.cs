using System;
using UnityEngine;

// BattleManager가 알려주는 전투 진행을 문장으로 만들어서 로그 창에 쓴다. 진행 타이밍이나 HP바는 모른다
public class BattleLogger : IDisposable
{
    private readonly BattleManager manager;
    private readonly BattleLogView view;
    private readonly string playerName;
    private readonly bool logToConsole; // 화면의 HP 변화와 비교할 수 있게 Console에도 찍는다

    public BattleLogger(BattleManager manager, BattleLogView view, string playerName, bool logToConsole)
    {
        this.manager = manager;
        this.view = view;
        this.playerName = playerName;
        this.logToConsole = logToConsole;

        manager.CountdownStarted += OnCountdownStarted;
        manager.BattleStarted += OnBattleStarted;
        manager.SubStageStarted += OnSubStageStarted;
        manager.AttackApplied += OnAttackApplied;
        manager.SubStageVerified += OnSubStageVerified;
        manager.BattleEnded += OnBattleEnded;
        manager.RequestRejected += OnRequestRejected;
    }

    public void Dispose()
    {
        manager.CountdownStarted -= OnCountdownStarted;
        manager.BattleStarted -= OnBattleStarted;
        manager.SubStageStarted -= OnSubStageStarted;
        manager.AttackApplied -= OnAttackApplied;
        manager.SubStageVerified -= OnSubStageVerified;
        manager.BattleEnded -= OnBattleEnded;
        manager.RequestRejected -= OnRequestRejected;
    }

    // 로그 창에는 이번 전투 내용만 남긴다. 재도전하면 시작 대기에 들어갈 때 지난 전투 로그를 지운다
    private void OnCountdownStarted(BattleSubStage subStage)
    {
        view.Clear();
    }

    private void OnBattleStarted(BattleSessionInfo session)
    {
        Log($"스테이지 {session.stage} 전투 시작!",
            $" (battle {session.battleId}, seed {session.battleSeed}, subStages {manager.SubStageCount}, from {session.subStageIndex + 1})");
    }

    private void OnSubStageStarted(BattleSubStage subStage)
    {
        var bossText = subStage.IsBoss ? "보스 " : "";
        Log($"{manager.Session.stage}-{subStage.SubStageIndex + 1} {bossText}{AddParticle(subStage.Enemy.name, "이", "가")} 나타났다!",
            $" (Player HP {subStage.PlayerHp:0.##}, Enemy HP {subStage.EnemyHp:0.##})");
    }

    // 예: "테스트유저의 치명적인 공격!! 녹슨 단검이 치명적인 공격을 받아 37.5의 데미지를 입었다."
    private void OnAttackApplied(BattleSubStage subStage, BattleEvent battleEvent)
    {
        string enemyName = subStage.Enemy.name;
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        var attackerName = isPlayerAttack ? playerName : enemyName;
        var targetName = isPlayerAttack ? enemyName : playerName;
        var attackText = battleEvent.isCrit ? "치명적인 공격" : "공격";

        Log($"{attackerName}의 {attackText}!! {AddParticle(targetName, "이", "가")} {attackText}을 받아 " +
            $"{battleEvent.damage:#,0.##}의 데미지를 입었다.",
            $" (t={battleEvent.time:0.00}, Player {battleEvent.playerHp:0.##} / Enemy {battleEvent.enemyHp:0.##})");

        // 예: "테스트유저가 체력을 3.75 흡혈했다." 체력이 가득 차서 회복하지 못했으면 쓰지 않는다
        if (battleEvent.lifesteal > 0)
            Log($"{AddParticle(attackerName, "이", "가")} 체력을 {battleEvent.lifesteal:#,0.##} 흡혈했다.");
    }

    // 서버 검증을 통과한 뒤에 결과를 쓴다
    private void OnSubStageVerified(BattleSubStage subStage, BattleResultVerifyResponse response)
    {
        Log(GetSubStageEndText(subStage), $" ({response.resultEnum} at {subStage.Duration:0.00}s, verified)");
    }

    private void OnBattleEnded(BattleEndResponse result)
    {
        var resultText = result.isVictory ? "클리어!" : "실패...";
        Log($"스테이지 {result.stage} {resultText} 골드 +{result.rewards.gold:N0} (보유 {result.currencies.Gold:N0})");
    }

    private void OnRequestRejected(string message)
    {
        Log(message);
    }

    private string GetSubStageEndText(BattleSubStage subStage)
    {
        switch (subStage.ResultEnum)
        {
            case BattleSubStageResultEnum.EnemyDead:
                // 체력이 가득 차 있어서 회복하지 않았으면 회복 문구는 뺀다
                var healText = subStage.HealOnKill > 0 ? $"체력을 {subStage.HealOnKill:#,0.##} 회복하고 " : "";
                return $"{AddParticle(subStage.Enemy.name, "을", "를")} 처치했다! {healText}{subStage.KillGold:N0} 골드를 얻었다.";
            case BattleSubStageResultEnum.PlayerDead:
                return $"{AddParticle(playerName, "이", "가")} 쓰러졌다...";
            case BattleSubStageResultEnum.TimeOver:
                return "제한 시간이 지났다...";
            default:
                throw new ArgumentOutOfRangeException(nameof(subStage.ResultEnum), subStage.ResultEnum, null);
        }
    }

    // consoleDetail은 Console에만 붙인다
    private void Log(string message, string consoleDetail = "")
    {
        view.AddLine(message);

        if (logToConsole)
            Debug.Log($"[Battle] {message}{consoleDetail}");
    }

    // 받침이 있으면 withFinal("이", "을"), 없으면 withoutFinal("가", "를")을 붙인다
    // 마지막 글자가 한글이 아니면 받침을 알 수 없으므로 "이(가)"처럼 둘 다 붙인다
    private static string AddParticle(string word, string withFinal, string withoutFinal)
    {
        var last = word[word.Length - 1];
        if (last < '가' || last > '힣')
            return $"{word}{withFinal}({withoutFinal})";

        var hasFinalConsonant = (last - '가') % 28 != 0;
        return hasFinalConsonant ? $"{word}{withFinal}" : $"{word}{withoutFinal}";
    }
}
