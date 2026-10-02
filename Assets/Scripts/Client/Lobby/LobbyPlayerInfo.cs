using TMPro;
using UnityEngine;

public class LobbyPlayerInfo : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGold;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDiamond;

    private void Awake()
    {
        txtGold = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Gold/Value");
        txtDiamond = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Diamond/Value");
    }

    public void SetCurrencies(string gold, string diamond)
    {
        txtGold.text = gold;
        txtDiamond.text = diamond;
    }
}
