using System;
using UnityEngine;
using UnityEngine.UI;

// 모든 팝업의 기반 클래스. Dimmed가 뒤쪽 입력을 막고, 설정에 따라 Dimmed를 누르면 닫힌다
public class BasePopup : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Button btnDimmed;

    [ReadOnly(true)] [SerializeField] private bool closeOnOutsideTouch = true; // 팝업 밖을 누르면 닫힘

    public bool IsOpened => gameObject.activeSelf;

    public event Action<BasePopup> OnOpenEvent = delegate { };
    public event Action<BasePopup> OnCloseEvent = delegate { };

    protected virtual void Awake()
    {
        btnDimmed = GameUtil.Bind<Button>(transform, "Dimmed");

        btnDimmed.onClick.AddListener(OnClickDimmed);
    }

    public void Open()
    {
        if (IsOpened)
        {
            return;
        }

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        OnOpenEvent.Invoke(this);
    }

    public void Close()
    {
        if (IsOpened == false)
        {
            return;
        }

        gameObject.SetActive(false);
        OnCloseEvent.Invoke(this);
    }

    private void OnClickDimmed()
    {
        if (closeOnOutsideTouch == false)
        {
            return;
        }

        Close();
    }
}
