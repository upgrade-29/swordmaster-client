using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 화면 아래 전투 로그 창. 받은 줄을 보여주기만 한다
// BattleLogPanel 아래 LogText 칸마다 한 줄씩 쓴다. 새 줄은 맨 아래 칸에 붙고, 칸 수를 넘친 오래된 줄은 위로 밀려 사라진다
public class BattleLogView : MonoBehaviour
{
    private readonly List<TMP_Text> logTexts = new List<TMP_Text>(); // 위에서 아래 순서
    private readonly Queue<string> lines = new Queue<string>();

    private void Awake()
    {
        GameUtil.TryBindAll(gameObject, logTexts);
        foreach (var logText in logTexts)
            logText.textWrappingMode = TextWrappingModes.NoWrap; // 한 칸에 한 줄만 쓴다
        Clear();
    }

    // 줄마다 TMP 자동 크기를 쓴다. 칸에 들어가는 줄은 기본 크기(fontSizeMax)로, 넘치는 긴 줄만 칸 폭에 맞게 줄어든다
    // 가장 작게 줄어드는 크기(fontSizeMin)는 나올 수 있는 가장 긴 줄이 딱 칸 폭에 들어가는 크기로 잡는다
    public void FitFontSizeMin(string longestLine)
    {
        foreach (var logText in logTexts)
        {
            logText.enableAutoSizing = true;

            // 자동 크기가 켜져 있으면 fontSizeMax 기준으로 잰 폭이 나온다. 글자 폭은 크기에 비례하므로 비율로 줄인다
            var width = logText.GetPreferredValues(longestLine).x;
            var fitSize = logText.fontSizeMax * logText.rectTransform.rect.width / width;
            logText.fontSizeMin = Mathf.Min(Mathf.Floor(fitSize * 10f) / 10f, logText.fontSizeMax);
        }
    }

    public void AddLine(string line)
    {
        lines.Enqueue(line);
        if (lines.Count > logTexts.Count)
            lines.Dequeue();

        Refresh();
    }

    public void Clear()
    {
        lines.Clear();
        Refresh();
    }

    // 줄이 칸보다 적으면 위쪽 칸을 비워서 마지막 줄이 항상 맨 아래 칸에 오게 한다
    private void Refresh()
    {
        var index = 0;
        for (; index < logTexts.Count - lines.Count; index++)
            logTexts[index].text = string.Empty;

        foreach (var line in lines)
            logTexts[index++].text = line;
    }
}
