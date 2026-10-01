using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 공통 버튼 표현. Label/Icon/Interactable/Loading 상태만 다루며 상점 등 특정 기능 로직을 갖지 않는다.
public class ShopButtonView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Button button;
    [ReadOnly] [SerializeField] private Image icon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI label;
    [ReadOnly] [SerializeField] private GameObject objLoading;

    private bool requestedInteractable = true;
    private bool isLoading;

    public event Action OnClickEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref button);
        icon = GameUtil.Bind<Image>(transform, "Icon");
        label = GameUtil.Bind<TextMeshProUGUI>(transform, "Label");
        objLoading = GameUtil.Bind<RectTransform>(transform, "Loading").gameObject;

        button.onClick.AddListener(OnClickButton);
    }

    public void SetLabel(string text)
    {
        label.text = text;
    }

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
        icon.gameObject.SetActive(sprite != null);
    }

    public void SetInteractable(bool interactable)
    {
        requestedInteractable = interactable;
        ApplyInteractable();
    }

    // Loading 중에는 중복 클릭을 막기 위해 강제로 Interactable을 false로 둔다.
    public void SetLoading(bool loading)
    {
        isLoading = loading;
        objLoading.SetActive(loading);
        ApplyInteractable();
    }

    private void ApplyInteractable()
    {
        button.interactable = requestedInteractable && isLoading == false;
    }

    private void OnClickButton()
    {
        OnClickEvent.Invoke();
    }
}
