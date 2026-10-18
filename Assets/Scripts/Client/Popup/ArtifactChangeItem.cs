using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArtifactChangeItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [ReadOnly] [SerializeField] private ButtonUI btnItem;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGrade;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLevel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private Image imgStatIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStat;
    [ReadOnly] [SerializeField] private Image imgStatus;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStatus;

    [SerializeField] private Sprite spriteStatusNormal;
    [SerializeField] private Sprite spriteStatusPressed;
    [SerializeField] private Sprite spriteStatusDisabled;

    // Button처럼 누르고 있는 동안 Status 이미지를 눌린 이미지로 바꾼다
    private bool isPointerDown;

    public event Action<ArtifactChangeItem> OnClickItemEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnItem);
        txtGrade = GameUtil.Bind<TextMeshProUGUI>(transform, "Top/Grade");
        txtLevel = GameUtil.Bind<TextMeshProUGUI>(transform, "Top/Level");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Name");
        imgStatIcon = GameUtil.Bind<Image>(transform, "StatRow/Icon");
        txtStat = GameUtil.Bind<TextMeshProUGUI>(transform, "StatRow/Stat");
        imgStatus = GameUtil.Bind<Image>(transform, "Status");
        txtStatus = GameUtil.Bind<TextMeshProUGUI>(transform, "Status/Label");

        btnItem.OnClickEvent += OnClickItem;
    }

    private void OnDisable()
    {
        isPointerDown = false;
    }

    public void Set(string grade, string level, string name, Sprite statIcon, string stat, string status, bool interactable)
    {
        txtGrade.text = grade;
        txtLevel.text = level;
        txtName.text = name;
        imgStatIcon.sprite = statIcon;
        txtStat.text = stat;
        txtStatus.text = status;
        btnItem.Interactable = interactable;
        UpdateStatusSprite();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerDown = true;
        UpdateStatusSprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerDown = false;
        UpdateStatusSprite();
    }

    private void UpdateStatusSprite()
    {
        if (btnItem.Interactable == false)
        {
            imgStatus.sprite = spriteStatusDisabled;
            return;
        }

        imgStatus.sprite = isPointerDown
            ? spriteStatusPressed
            : spriteStatusNormal;
    }

    private void OnClickItem()
    {
        OnClickItemEvent.Invoke(this);
    }
}
