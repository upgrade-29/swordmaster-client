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

    // 내용이 있는 팝업은 Setup으로 내용을 받은 뒤에 연다. 이미 열려 있으면 켜지 않고 새 내용만 다시 반영한다
    public void Open()
    {
        if (IsOpened == false)
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            OnOpenEvent.Invoke(this);
        }

        OnOpen();
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

    // 켜진 뒤에 호출되므로 처음 열 때도 Awake의 바인딩이 끝나 있다. Setup으로 받아 둔 내용을 여기서 화면에 반영한다
    protected virtual void OnOpen()
    {
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
