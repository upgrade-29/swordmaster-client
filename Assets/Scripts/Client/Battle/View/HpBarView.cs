using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpBarView : MonoBehaviour
{
    private Image fillImage; // Image Type = Filled
    private TMP_Text hpText;

    private void Awake()
    {
        fillImage = GameUtil.Bind<Image>(gameObject, "Fill");
        hpText = GameUtil.Bind<TMP_Text>(gameObject, "HpText");
    }

    public void SetHp(double current, double max)
    {
        fillImage.fillAmount = max > 0 ? (float)(current / max) : 0f;

        // 데미지는 소수까지 계산하지만 화면에는 올림해서 보여준다. 살아 있는데 0으로 보이지 않게 하기 위해서다
        hpText.text = $"{System.Math.Ceiling(current):N0} / {System.Math.Ceiling(max):N0}";
    }
}
