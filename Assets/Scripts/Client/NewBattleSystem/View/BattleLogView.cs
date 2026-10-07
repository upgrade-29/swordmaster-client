using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 화면 아래 전투 로그 창. 받은 줄을 보여주기만 한다
// 새 줄은 아래에 붙고, 창을 넘친 오래된 줄은 위로 잘려서 안 보인다 (BattleLogPanel의 RectMask2D)
public class BattleLogView : MonoBehaviour
{
    // 창에서 잘려 안 보이는 줄까지 계속 쌓이지 않게 이 개수만 남긴다
    private const int MaxLines = 50;

    private TMP_Text logText;
    private readonly Queue<string> lines = new Queue<string>();

    private void Awake()
    {
        logText = GameUtil.Bind<TMP_Text>(gameObject, "LogText");
        logText.text = string.Empty;
    }

    public void AddLine(string line)
    {
        lines.Enqueue(line);
        if (lines.Count > MaxLines)
            lines.Dequeue();

        logText.text = string.Join("\n", lines);
    }

    public void Clear()
    {
        lines.Clear();
        logText.text = string.Empty;
    }
}
