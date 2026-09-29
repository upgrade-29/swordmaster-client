using UnityEngine;

// 전투에 나오는 플레이어/적 한 명의 겉모습. 계산은 하지 않고 보여주기만 한다
public class BattleUnitView : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref spriteRenderer);
    }

    public void SetVisible(bool visible)
    {
        spriteRenderer.enabled = visible;
    }

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }
}
