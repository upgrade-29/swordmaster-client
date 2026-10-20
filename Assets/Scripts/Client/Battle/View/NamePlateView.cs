using TMPro;
using UnityEngine;

// 유닛 위에 뜨는 플레이어/적 이름판. 데미지 숫자가 이름판 위로 그려지도록 월드 공간 캔버스에 둔다
public class NamePlateView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtPlayerName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtEnemyName;

    private void Awake()
    {
        txtPlayerName = GameUtil.Bind<TextMeshProUGUI>(transform, "PlayerNamePlate/Label");
        txtEnemyName = GameUtil.Bind<TextMeshProUGUI>(transform, "EnemyNamePlate/Label");
    }

    public void SetPlayerName(string playerName)
    {
        txtPlayerName.text = playerName;
    }

    public void SetEnemyName(string enemyName)
    {
        txtEnemyName.text = enemyName;
    }
}
