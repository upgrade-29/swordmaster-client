using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceEquipmentAction : MonoBehaviour
{
    private const string EnhanceLabel = "강화";
    private const string EquipLabel = "아티팩트 장착";

    [ReadOnly] [SerializeField] private GameObject objEnhancePercentage;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtEnhancePercentage;
    [ReadOnly] [SerializeField] private GameObject objMaterial;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtMaterialCount;
    [ReadOnly] [SerializeField] private RectTransform rectMaterialFill;
    [ReadOnly] [SerializeField] private Button btnEnhance;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtEnhanceLabel;
    [ReadOnly] [SerializeField] private GameObject objEnhanceCost;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtEnhanceCost;
    [ReadOnly] [SerializeField] private Button btnSell;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtSellPrice;
    [ReadOnly] [SerializeField] private Button btnChange;

    public event Action OnClickEnhanceEvent = delegate { };
    public event Action OnClickSellEvent = delegate { };
    public event Action OnClickChangeEvent = delegate { };

    private void Awake()
    {
        objEnhancePercentage = GameUtil.Bind<RectTransform>(transform, "EnhancePercentage").gameObject;
        txtEnhancePercentage = GameUtil.Bind<TextMeshProUGUI>(transform, "EnhancePercentage/Value");
        objMaterial = GameUtil.Bind<RectTransform>(transform, "Material").gameObject;
        txtMaterialCount = GameUtil.Bind<TextMeshProUGUI>(transform, "Material/Count");
        rectMaterialFill = GameUtil.Bind<RectTransform>(transform, "Material/Progress/Fill");
        btnEnhance = GameUtil.Bind<Button>(transform, "Buttons/EnhanceButton");
        txtEnhanceLabel = GameUtil.Bind<TextMeshProUGUI>(transform, "Buttons/EnhanceButton/Label");
        objEnhanceCost = GameUtil.Bind<RectTransform>(transform, "Buttons/EnhanceButton/Cost").gameObject;
        txtEnhanceCost = GameUtil.Bind<TextMeshProUGUI>(transform, "Buttons/EnhanceButton/Cost/Value");
        btnSell = GameUtil.Bind<Button>(transform, "Buttons/SellButton");
        txtSellPrice = GameUtil.Bind<TextMeshProUGUI>(transform, "Buttons/SellButton/Cost/Value");
        btnChange = GameUtil.Bind<Button>(transform, "Buttons/ChangeButton");

        btnEnhance.onClick.AddListener(OnClickEnhance);
        btnSell.onClick.AddListener(OnClickSell);
        btnChange.onClick.AddListener(OnClickChange);
    }

    public void SetEnhancePercentage(string percentage)
    {
        txtEnhancePercentage.text = percentage;
    }

    public void SetEnhanceCost(string cost)
    {
        txtEnhanceCost.text = cost;
    }

    public void SetMaterial(string count, float fillRatio)
    {
        txtMaterialCount.text = count;
        rectMaterialFill.anchorMax = new Vector2(fillRatio, 1f);
    }

    public void SetSellPrice(string price)
    {
        txtSellPrice.text = price;
    }

    public void ShowSword()
    {
        objEnhancePercentage.SetActive(true);
        objMaterial.SetActive(false);
        txtEnhanceLabel.text = EnhanceLabel;
        objEnhanceCost.SetActive(true);
        btnSell.gameObject.SetActive(true);
        btnChange.gameObject.SetActive(false);
    }

    public void ShowArtifact(bool equipped)
    {
        objEnhancePercentage.SetActive(false);
        objMaterial.SetActive(equipped);
        txtEnhanceLabel.text = equipped
            ? EnhanceLabel
            : EquipLabel;
        objEnhanceCost.SetActive(equipped);
        btnSell.gameObject.SetActive(false);
        btnChange.gameObject.SetActive(equipped);
    }

    private void OnClickEnhance()
    {
        OnClickEnhanceEvent.Invoke();
    }

    private void OnClickSell()
    {
        OnClickSellEvent.Invoke();
    }

    private void OnClickChange()
    {
        OnClickChangeEvent.Invoke();
    }
}
