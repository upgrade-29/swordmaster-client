using TMPro;
using UnityEngine;

// 카테고리 섹션 1개의 헤더 표시와 상품 아이템 컨테이너 제공만 담당한다. 상품 목록 구성이나 구매 로직을 갖지 않는다.
public class ShopSectionView : MonoBehaviour
{
    [ReadOnly] [SerializeField] private TextMeshProUGUI headerText;
    [ReadOnly] [SerializeField] private Transform itemsContainer;

    public Transform ItemsContainer => itemsContainer;

    private void Awake()
    {
        headerText = GameUtil.Bind<TextMeshProUGUI>(transform, "Header");
        itemsContainer = GameUtil.Bind<RectTransform>(transform, "ItemsContainer");
    }

    public void SetHeader(string categoryDisplayName)
    {
        headerText.text = categoryDisplayName;
    }
}
