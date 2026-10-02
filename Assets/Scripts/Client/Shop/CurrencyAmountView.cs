using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 재화 아이콘과 값 표시만 담당한다. 플레이어 재화의 권위 상태를 보유하지 않는다.
public class CurrencyAmountView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Image icon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI amountText;

    private void Awake()
    {
        icon = GameUtil.Bind<Image>(transform, "Icon");
        amountText = GameUtil.Bind<TextMeshProUGUI>(transform, "AmountText");
    }

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    public void SetAmount(long amount)
    {
        amountText.text = amount.ToString();
    }
}
