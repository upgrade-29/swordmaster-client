using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceStatsPanel : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtAttack;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMaxHp;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtAttackSpeed;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCritRate;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLifesteal;
    [ReadOnly] [SerializeField] private Button btnOutside;

    public event Action OnClickOutsideEvent = delegate { };

    private void Awake()
    {
        txtAttack = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/Attack/Value");
        txtMaxHp = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/MaxHp/Value");
        txtAttackSpeed = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/AttackSpeed/Value");
        txtCritRate = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/CritRate/Value");
        txtLifesteal = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/Lifesteal/Value");
        btnOutside = GameUtil.Bind<Button>(transform, "OutsideArea");

        btnOutside.onClick.AddListener(OnClickOutside);
    }

    private void OnDestroy()
    {
        if (btnOutside != null)
        {
            btnOutside.onClick.RemoveListener(OnClickOutside);
        }
    }

    public void SetStats(string attack, string maxHp, string attackSpeed, string critRate, string lifesteal)
    {
        txtAttack.text = attack;
        txtMaxHp.text = maxHp;
        txtAttackSpeed.text = attackSpeed;
        txtCritRate.text = critRate;
        txtLifesteal.text = lifesteal;
    }

    public void SetOpened(bool opened)
    {
        gameObject.SetActive(opened);
    }

    private void OnClickOutside()
    {
        OnClickOutsideEvent.Invoke();
    }
}
