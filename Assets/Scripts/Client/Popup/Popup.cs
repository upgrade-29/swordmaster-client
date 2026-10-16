using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 씬마다 캔버스 아래 PopupUI에 하나씩 두고, 그 씬에서 쓰는 팝업을 자식으로 모아 둔다. 씬이 바뀌면 팝업과 함께 사라진다
// 팝업은 씬에서 꺼 둔 상태로 두며, 처음 Open할 때 Awake가 실행되므로 내용은 Setup으로 받아 두었다가 Open에서 반영한다
public class Popup : SingletonMonoBehaviour<Popup>
{
    [ReadOnly] [SerializeField] private List<BasePopup> listBasePopup = new List<BasePopup>();

    protected override void OnAwakeSingleton()
    {
        foreach (Transform child in transform)
        {
            listBasePopup.Add(GameUtil.TryGetComponent<BasePopup>(child.gameObject));
        }
    }

    public T Get<T>() where T : BasePopup
    {
        return listBasePopup.OfType<T>().First();
    }
}
