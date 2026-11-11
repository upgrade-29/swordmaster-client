using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랭킹 한 줄. 상위 목록과 내 기록이 같은 모양을 쓴다
public class RankingItem : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image imgBackground;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtRank;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNickname;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtScore;

    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteMine; // 상위 목록에 내가 있을 때 그 줄의 배경

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref imgBackground);
        txtRank = GameUtil.Bind<TextMeshProUGUI>(transform, "Rank");
        txtNickname = GameUtil.Bind<TextMeshProUGUI>(transform, "Nickname");
        txtScore = GameUtil.Bind<TextMeshProUGUI>(transform, "Score");
    }

    public void Set(string rank, string nickname, string score)
    {
        txtRank.text = rank;
        txtNickname.text = nickname;
        txtScore.text = score;
    }

    public void SetMine(bool mine)
    {
        imgBackground.sprite = mine
            ? spriteMine
            : spriteNormal;
    }
}
