using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTab : MonoBehaviour
{
    [ReadOnly] [SerializeField] private NestedScrollRect nestedScrollRect;
    [ReadOnly] [SerializeField] private LayoutElement layoutElement;

    public event Action<LobbyTab> OnBeginDragTrackEvent = delegate { };
    public event Action<LobbyTab> OnEndDragTrackEvent = delegate { };

    protected virtual void Awake()
    {
        GameUtil.Bind(gameObject, ref nestedScrollRect);

        nestedScrollRect.OnBeginRouteToParentEvent += OnBeginRouteToParent;
        nestedScrollRect.OnEndRouteToParentEvent += OnEndRouteToParent;
    }

    protected virtual void OnDestroy()
    {
        if (nestedScrollRect == null)
        {
            return;
        }

        nestedScrollRect.OnBeginRouteToParentEvent -= OnBeginRouteToParent;
        nestedScrollRect.OnEndRouteToParentEvent -= OnEndRouteToParent;
    }

    public void SetWidth(float width)
    {
        GameUtil.Bind(gameObject, ref layoutElement).preferredWidth = width;
    }

    private void OnBeginRouteToParent(NestedScrollRect scrollRect)
    {
        OnBeginDragTrackEvent.Invoke(this);
    }

    private void OnEndRouteToParent(NestedScrollRect scrollRect)
    {
        OnEndDragTrackEvent.Invoke(this);
    }
}
