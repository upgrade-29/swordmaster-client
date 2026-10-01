using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BattleDirector가 알려주는 전투 진행을 문장으로 만들어서 로그 창에 쓴다. 재생 타이밍이나 HP바는 모른다
public class BattleLogger : IDisposable
{
    private readonly BattleDirector director;
    private readonly BattleLogView view;
    private readonly string playerName;
    private readonly Dictionary<string, string> enemyNamesByCode; // 응답에는 enemyCode만 오므로 표시할 이름은 GameDB에서 찾는다
    private readonly bool logToConsole; // 화면의 HP 변화와 비교할 수 있게 Console에도 찍는다

    public BattleLogger(BattleDirector director, BattleLogView view, string playerName, GameDB gameDB, bool logToConsole)
    {
        this.director = director;
        this.view = view;
        this.playerName = playerName;
        enemyNamesByCode = gameDB.enemies.ToDictionary(data => data.enemyCode, data => data.name);
        this.logToConsole = logToConsole;

        director.CountdownStarted += OnCountdownStarted;
        director.BattleStarted += OnBattleStarted;
        director.WaveStarted += OnWaveStarted;
        director.AttackApplied += OnAttackApplied;
        director.WaveEnded += OnWaveEnded;
        director.BattleEnded += OnBattleEnded;
        director.RequestRejected += OnRequestRejected;
    }

    public void Dispose()
    {
        director.CountdownStarted -= OnCountdownStarted;
        director.BattleStarted -= OnBattleStarted;
        director.WaveStarted -= OnWaveStarted;
        director.AttackApplied -= OnAttackApplied;
        director.WaveEnded -= OnWaveEnded;
        director.BattleEnded -= OnBattleEnded;
        director.RequestRejected -= OnRequestRejected;
    }

    // 로그 창에는 이번 전투 내용만 남긴다. 재도전하면 시작 대기에 들어갈 때 지난 전투 로그를 지운다
    private void OnCountdownStarted()
    {
        view.Clear();
    }

    private void OnBattleStarted(BattleResult result)
    {
        Log($"스테이지 {result.stage} 전투 시작!", $" (waves {result.waves.Count})");
    }

    private void OnWaveStarted(BattleResult result, BattleWave wave, int waveNumber)
    {
        var bossText = wave.isBoss ? "보스 " : "";
        Log($"{result.stage}-{waveNumber} {bossText}{AddParticle(GetEnemyName(wave), "이", "가")} 나타났다!",
            $" (Player HP {wave.playerStartHp:0.##}, Enemy HP {wave.enemyStat.maxHp:0.##})");
    }

    // 예: "테스트유저의 치명적인 공격!! 녹슨 단검이 치명적인 공격을 받아 37.5의 데미지를 입었다."
    private void OnAttackApplied(BattleWave wave, BattleEvent battleEvent)
    {
        string enemyName = GetEnemyName(wave);
        var isPlayerAttack = battleEvent.attacker == BattleSide.Player;
        var attackerName = isPlayerAttack ? playerName : enemyName;
        var targetName = isPlayerAttack ? enemyName : playerName;
        var attackText = battleEvent.isCrit ? "치명적인 공격" : "공격";

        Log($"{attackerName}의 {attackText}!! {AddParticle(targetName, "이", "가")} {attackText}을 받아 " +
            $"{battleEvent.damage:#,0.##}의 데미지를 입었다.",
            $" (t={battleEvent.time:0.00}, Player {battleEvent.playerHp:0.##} / Enemy {battleEvent.enemyHp:0.##})");
    }

    private void OnWaveEnded(BattleWave wave)
    {
        Log(GetWaveEndText(wave), $" ({wave.outcome} at {wave.duration:0.00}s)");
    }

    private void OnBattleEnded(BattleResult result)
    {
        var resultText = result.isVictory ? "클리어!" : "실패...";
        Log($"스테이지 {result.stage} {resultText} 골드 +{result.rewards.gold:N0} (보유 {result.currencies.Gold:N0})");
    }

    private void OnRequestRejected(string reason)
    {
        Log($"전투를 시작할 수 없다. ({reason})");
    }

    private string GetWaveEndText(BattleWave wave)
    {
        switch (wave.outcome)
        {
            case WaveOutcome.EnemyDead:
                // 체력이 가득 차 있어서 회복하지 않았으면 회복 문구는 뺀다
                var healText = wave.healOnKill > 0 ? $"체력을 {wave.healOnKill:#,0.##} 회복하고 " : "";
                return $"{AddParticle(GetEnemyName(wave), "을", "를")} 처치했다! {healText}{wave.gold:N0} 골드를 얻었다.";
            case WaveOutcome.PlayerDead:
                return $"{AddParticle(playerName, "이", "가")} 쓰러졌다...";
            case WaveOutcome.TimeOver:
                return "제한 시간이 지났다...";
            default:
                throw new ArgumentOutOfRangeException(nameof(wave.outcome), wave.outcome, null);
        }
    }

    private string GetEnemyName(BattleWave wave)
    {
        return enemyNamesByCode[wave.enemyCode];
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
