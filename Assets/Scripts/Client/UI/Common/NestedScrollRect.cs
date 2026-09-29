using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NestedScrollRect : ScrollRect
{
    private const float RouteToParentDragRatio = 1.25f; // 스크롤 배율 -> 이 값이 존재해야 대각선 드래그 시 보정이 가능

    private bool isRoutedToParent;

    public event Action<NestedScrollRect> OnBeginRouteToParentEvent = delegate { };
    public event Action<NestedScrollRect> OnEndRouteToParentEvent = delegate { };

    private bool IsRouteToParentDrag(PointerEventData eventData)
    {
        if (horizontal == false && vertical == false)
        {
            return true;
        }

        Vector2 delta = eventData.position - eventData.pressPosition;
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        if (horizontal == false && absX > absY * RouteToParentDragRatio)
        {
            return true;
        }

        return vertical == false && absY > absX * RouteToParentDragRatio;
    }

    private void ExecuteParent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
    {
        if (transform.parent == null)
        {
            return;
        }

        ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, handler);
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        ExecuteParent(eventData, ExecuteEvents.initializePotentialDrag);
        base.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        isRoutedToParent = IsRouteToParentDrag(eventData);

        if (isRoutedToParent)
        {
            ExecuteParent(eventData, ExecuteEvents.beginDragHandler);
            OnBeginRouteToParentEvent.Invoke(this);
            return;
        }

        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (isRoutedToParent)
        {
            ExecuteParent(eventData, ExecuteEvents.dragHandler);
            return;
        }

        base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (isRoutedToParent)
        {
            isRoutedToParent = false;
            ExecuteParent(eventData, ExecuteEvents.endDragHandler);
            OnEndRouteToParentEvent.Invoke(this);
            return;
        }

        base.OnEndDrag(eventData);
    }
}
