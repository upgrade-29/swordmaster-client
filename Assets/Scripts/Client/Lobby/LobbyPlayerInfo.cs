using TMPro;
using UnityEngine;

public class LobbyPlayerInfo : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNickname;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGold;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDiamond;

    private void Awake()
    {
        txtNickname = GameUtil.Bind<TextMeshProUGUI>(transform, "Profile/NameGroup/Name");
        txtGold = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Gold/Value");
        txtDiamond = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Diamond/Value");
    }

    public void SetNickname(string nickname)
    {
        txtNickname.text = nickname;
    }

    public void SetCurrencies(string gold, string diamond)
    {
        txtGold.text = gold;
        txtDiamond.text = diamond;
    }
}
