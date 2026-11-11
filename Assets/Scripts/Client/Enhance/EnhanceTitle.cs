using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class EnhanceTitle : MonoBehaviour
{
    private const string StatsOpenedMark = "▲";
    private const string StatsClosedMark = "▼";

    // 전투력 변화: 지금 숫자에서 새 숫자까지 올라가거나 내려가고, 다 바뀐 뒤 잠깐 더 변화 색을 보여 준다
    private const float CombatPowerChangeDuration = 0.6f;
    private const Ease CombatPowerChangeEase = Ease.OutExpo;
    private const float CombatPowerColorHoldDuration = 0.3f;

    [ReadOnly] [SerializeField] private ButtonUI btnCombatPower;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCombatPower;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStatsToggle;

    private long displayedCombatPower;
    private Color colorCombatPower;
    private Sequence seqCombatPower;

    public event Action OnClickCombatPowerEvent = delegate { };

    private void Awake()
    {
        btnCombatPower = GameUtil.Bind<ButtonUI>(transform, "CombatPower");
        txtCombatPower = GameUtil.Bind<TextMeshProUGUI>(transform, "CombatPower/Value");
        txtStatsToggle = GameUtil.Bind<TextMeshProUGUI>(transform, "CombatPower/Toggle");
        colorCombatPower = txtCombatPower.color;

        btnCombatPower.OnClickEvent += OnClickCombatPower;
    }

    /// <summary>
    /// 전투력을 표시한다. animate면 지금 숫자에서 새 숫자까지 올라가거나 내려가고, 그동안 오르면 초록, 내리면 빨강으로 보인다.
    /// </summary>
    public void SetCombatPower(long combatPower, bool animate)
    {
        seqCombatPower?.Kill();
        seqCombatPower = null;

        long from = displayedCombatPower;
        displayedCombatPower = combatPower;
        if (animate == false || from == combatPower)
        {
            txtCombatPower.color = colorCombatPower;
            txtCombatPower.text = combatPower.ToString(GameConstants.NumberFormats.Integer);
            return;
        }

        txtCombatPower.color = combatPower > from
            ? GameConstants.Colors.StatIncrease
            : GameConstants.Colors.StatDecrease;
        seqCombatPower = DOTween.Sequence()
            .Append(DOVirtual.Float(from, combatPower, CombatPowerChangeDuration, SetCombatPowerText).SetEase(CombatPowerChangeEase))
            .AppendInterval(CombatPowerColorHoldDuration)
            .OnComplete(() => txtCombatPower.color = colorCombatPower)
            .SetLink(gameObject);
    }

    public void SetStatsOpened(bool opened)
    {
        txtStatsToggle.text = opened
            ? StatsOpenedMark
            : StatsClosedMark;
    }

    private void SetCombatPowerText(float combatPower)
    {
        txtCombatPower.text = Math.Round(combatPower).ToString(GameConstants.NumberFormats.Integer);
    }

    private void OnClickCombatPower()
    {
        OnClickCombatPowerEvent.Invoke();
    }
}
