using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactChangeItem : MonoBehaviour
{
    [ReadOnly] [SerializeField] private ButtonUI btnItem;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtGrade;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtLevel;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtName;
    [ReadOnly] [SerializeField] private Image imgArtifact;
    [ReadOnly] [SerializeField] private Image imgGradeFrame;
    [ReadOnly] [SerializeField] private Image imgStatIcon;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStat;
    [ReadOnly] [SerializeField] private Image imgStatus;
    [ReadOnly] [SerializeField] private TextMeshProUGUI txtStatus;

    [SerializeField] private Sprite spriteStatusNormal;
    [SerializeField] private Sprite spriteStatusPressed;
    [SerializeField] private Sprite spriteStatusDisabled;

    public event Action<ArtifactChangeItem> OnClickItemEvent = delegate { };

    private void Awake()
    {
        GameUtil.Bind(gameObject, ref btnItem);
        txtGrade = GameUtil.Bind<TextMeshProUGUI>(transform, "Top/Grade");
        txtLevel = GameUtil.Bind<TextMeshProUGUI>(transform, "Top/Level");
        txtName = GameUtil.Bind<TextMeshProUGUI>(transform, "Name");
        imgArtifact = GameUtil.Bind<Image>(transform, "IconArea/Icon/Image");
        imgGradeFrame = GameUtil.Bind<Image>(transform, "IconArea/Icon/GradeFrame");
        imgStatIcon = GameUtil.Bind<Image>(transform, "StatRow/Icon");
        txtStat = GameUtil.Bind<TextMeshProUGUI>(transform, "StatRow/Stat");
        imgStatus = GameUtil.Bind<Image>(transform, "Status");
        txtStatus = GameUtil.Bind<TextMeshProUGUI>(transform, "Status/Label");

        btnItem.OnClickEvent += OnClickItem;
        btnItem.OnPressedChangedEvent += OnPressedChangedItem;
    }

    // 이미지를 미리 읽지 못했거나 등급 테두리가 없으면 그 칸을 숨긴다
    public void Set(string grade, Color gradeColor, string level, string name, Sprite image, Sprite gradeFrame, Sprite statIcon, string stat,
        string status, bool interactable)
    {
        txtGrade.text = grade;
        txtGrade.color = gradeColor;
        txtLevel.text = level;
        txtName.text = name;
        imgArtifact.sprite = image;
        imgArtifact.enabled = image != null;
        imgGradeFrame.sprite = gradeFrame;
        imgGradeFrame.enabled = gradeFrame != null;
        imgStatIcon.sprite = statIcon;
        txtStat.text = stat;
        txtStatus.text = status;
        btnItem.Interactable = interactable;
        UpdateStatusSprite();
    }

    private void UpdateStatusSprite()
    {
        if (btnItem.Interactable == false)
        {
            imgStatus.sprite = spriteStatusDisabled;
            return;
        }

        // 누르고 있는 동안 Status 줄도 눌린 버튼 이미지로 바꾼다
        imgStatus.sprite = btnItem.IsPressed
            ? spriteStatusPressed
            : spriteStatusNormal;
    }

    private void OnPressedChangedItem(bool pressed)
    {
        UpdateStatusSprite();
    }

    private void OnClickItem()
    {
        OnClickItemEvent.Invoke(this);
    }
}
