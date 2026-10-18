using System;
using TMPro;
using UnityEngine;

public class LobbyPlayerInfo : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNickname;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGold;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDiamond;
    [ReadOnly] [SerializeField] private ButtonUI menuButton;

    public event Action OnClickMenuEvent = delegate { };

    private void Awake()
    {
        txtNickname = GameUtil.Bind<TextMeshProUGUI>(transform, "Profile/NameGroup/Name");
        txtGold = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Gold/Value");
        txtDiamond = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Diamond/Value");
        menuButton = GameUtil.Bind<ButtonUI>(transform, "MenuButton");

        menuButton.OnClickEvent += OnClickMenu;
    }

    private void OnDestroy()
    {
        if (menuButton != null)
        {
            menuButton.OnClickEvent -= OnClickMenu;
        }
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

    private void OnClickMenu()
    {
        OnClickMenuEvent.Invoke();
    }
}
