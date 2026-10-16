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

        // 소수 첫째 자리까지 보여주고 그 아래는 올린다. 살아 있는데 0으로 보이지 않게 하기 위해서다
        hpText.text = $"{CeilToTenth(current):#,0.#} / {CeilToTenth(max):#,0.#}";
    }

    // 87.54 -> 87.6. 87.5처럼 이미 딱 떨어지는 값이 부동소수 오차로 87.6이 되지 않게 조금 깎고 올린다
    private static double CeilToTenth(double value)
    {
        return System.Math.Ceiling(value * 10 - 1e-6) / 10;
    }
}
