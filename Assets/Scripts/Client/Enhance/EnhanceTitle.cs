using System;
using TMPro;
using UnityEngine;

public class EnhanceTitle : MonoBehaviour
{
    private const string StatsOpenedMark = "▲";
    private const string StatsClosedMark = "▼";

    [ReadOnly] [SerializeField] private ButtonUI btnCombatPower;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCombatPower;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStatsToggle;

    public event Action OnClickCombatPowerEvent = delegate { };

    private void Awake()
    {
        btnCombatPower = GameUtil.Bind<ButtonUI>(transform, "CombatPower");
        txtCombatPower = GameUtil.Bind<TextMeshProUGUI>(transform, "CombatPower/Value");
        txtStatsToggle = GameUtil.Bind<TextMeshProUGUI>(transform, "CombatPower/Toggle");

        btnCombatPower.OnClickEvent += OnClickCombatPower;
    }

    public void SetCombatPower(string combatPower)
    {
        txtCombatPower.text = combatPower;
    }

    public void SetStatsOpened(bool opened)
    {
        txtStatsToggle.text = opened
            ? StatsOpenedMark
            : StatsClosedMark;
    }

    private void OnClickCombatPower()
    {
        OnClickCombatPowerEvent.Invoke();
    }
}
