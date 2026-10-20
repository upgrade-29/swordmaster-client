using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class LobbyPlayerInfo : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtNickname;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGold;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtDiamond;
    [ReadOnly] [SerializeField] private RectTransform rectGoldIcon;
    [ReadOnly] [SerializeField] private RectTransform rectDiamondIcon;
    [ReadOnly] [SerializeField] private ButtonUI menuButton;

    public event Action OnClickMenuEvent = delegate { };

    private void Awake()
    {
        txtNickname = GameUtil.Bind<TextMeshProUGUI>(transform, "Profile/NameGroup/Name");
        txtGold = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Gold/Value");
        txtDiamond = GameUtil.Bind<TextMeshProUGUI>(transform, "Currency/Diamond/Value");
        rectGoldIcon = GameUtil.Bind<RectTransform>(transform, "Currency/Gold/Icon");
        rectDiamondIcon = GameUtil.Bind<RectTransform>(transform, "Currency/Diamond/Icon");
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

    // 상점에서 얻은 재화가 빨려 들어갈 자리(캔버스 UI의 월드 좌표)
    public Vector3 GetCurrencyIconPosition(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                return rectGoldIcon.position;
            case CurrencyType.Diamond:
                return rectDiamondIcon.position;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    /// <summary>
    /// 날아온 재화가 닿을 때 그 재화의 아이콘과 숫자를 한 번 튀게 한다. 연달아 닿으면 처음부터 다시 튄다.
    /// </summary>
    public void PlayCurrencyArrived(CurrencyType type)
    {
        RectTransform rectIcon = type == CurrencyType.Gold ? rectGoldIcon : rectDiamondIcon;
        RectTransform rectValue = type == CurrencyType.Gold ? txtGold.rectTransform : txtDiamond.rectTransform;

        rectIcon.DOKill();
        DOVirtual.Float(0f, 1f, GameConstants.CurrencyArrive.PunchDuration, progress =>
            {
                float wave = Mathf.Sin(progress * Mathf.PI);
                rectIcon.localScale = Vector3.one * (1f + (GameConstants.CurrencyArrive.IconPunchScale - 1f) * wave);
                rectValue.localScale = Vector3.one * (1f + (GameConstants.CurrencyArrive.TextPunchScale - 1f) * wave);
            })
            .SetEase(Ease.Linear)
            .SetTarget(rectIcon)
            .SetLink(gameObject);
    }

    private void OnClickMenu()
    {
        OnClickMenuEvent.Invoke();
    }
}
