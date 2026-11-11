using DG.Tweening;
using TMPro;
using UnityEngine;

// 유닛 위에 뜨는 플레이어/적 이름판. 데미지 숫자가 이름판 위로 그려지도록 월드 공간 캔버스에 둔다
public class NamePlateView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtPlayerName;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtEnemyName;
    [ReadOnly] [SerializeField] private CanvasGroup canvasGroupEnemy;

    private void Awake()
    {
        txtPlayerName = GameUtil.Bind<TextMeshProUGUI>(transform, "PlayerNamePlate/Label");
        txtEnemyName = GameUtil.Bind<TextMeshProUGUI>(transform, "EnemyNamePlate/Label");
        canvasGroupEnemy = GameUtil.Bind<CanvasGroup>(transform, "EnemyNamePlate");
    }

    private void OnDestroy()
    {
        canvasGroupEnemy.DOKill();
    }

    /// <summary>
    /// 적 이름판을 duration(배속 반영) 동안 흐리게 숨기거나 보여 준다. 0이면 바로 바뀐다.
    /// </summary>
    public void SetEnemyVisible(bool visible, float duration)
    {
        canvasGroupEnemy.DOKill();
        canvasGroupEnemy.DOFade(visible ? 1f : 0f, duration);
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
