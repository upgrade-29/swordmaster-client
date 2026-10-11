using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 로비의 팝업을 모아 둔다. 팝업은 씬에서 꺼 둔 상태로 두며, 처음 Open할 때 Awake가 실행되므로 내용은 Open 후에 채운다
public class LobbyPopupUI : SingletonMonoBehaviour<LobbyPopupUI>
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
