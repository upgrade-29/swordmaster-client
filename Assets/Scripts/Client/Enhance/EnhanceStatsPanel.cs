using TMPro;
using UnityEngine;

public class EnhanceStatsPanel : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtAttack;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMaxHp;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtAttackSpeed;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtCritRate;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLifesteal;

    private void Awake()
    {
        txtAttack = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/Attack/Value");
        txtMaxHp = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/MaxHp/Value");
        txtAttackSpeed = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/AttackSpeed/Value");
        txtCritRate = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/CritRate/Value");
        txtLifesteal = GameUtil.Bind<TextMeshProUGUI>(transform, "Background/Stats/Lifesteal/Value");
    }

    public void SetOpened(bool opened)
    {
        gameObject.SetActive(opened);
    }
}
