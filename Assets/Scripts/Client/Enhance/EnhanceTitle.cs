using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceTitle : MonoBehaviour
{
    private const string StatsOpenedMark = "▲";
    private const string StatsClosedMark = "▼";

    [ReadOnly] [SerializeField] private Button btnCombatPower;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCombatPower;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStatsToggle;

    public event Action OnClickCombatPowerEvent = delegate { };

    private void Awake()
    {
        btnCombatPower = GameUtil.Bind<Button>(transform, "CombatPower");
        txtCombatPower = GameUtil.Bind<TextMeshProUGUI>(transform, "CombatPower/Value");
        txtStatsToggle = GameUtil.Bind<TextMeshProUGUI>(transform, "CombatPower/Toggle");

        btnCombatPower.onClick.AddListener(OnClickCombatPower);
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
