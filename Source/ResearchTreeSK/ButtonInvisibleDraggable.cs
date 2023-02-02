using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public static class ButtonInvisibleDraggable
{
	private static Vector3 mouseStart = Vector2.zero;

	private static bool isDragged = false;

	public static Widgets.DraggableResult Handle(NodeClickInfo info)
	{
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && info.MouseDownedNode != null)
		{
			mouseStart = Input.mousePosition;
			isDragged = false;
			return Widgets.DraggableResult.Idle;
		}
		if (info.MouseDownedNode != null)
		{
			if (Input.GetMouseButtonUp(0))
			{
				if (!isDragged)
				{
					return Widgets.DraggableResult.Pressed;
				}
				return Widgets.DraggableResult.DraggedThenPressed;
			}
			if (Input.GetMouseButton(0))
			{
				if (!isDragged && (mouseStart - Input.mousePosition).sqrMagnitude > 20f)
				{
					isDragged = true;
				}
				return Widgets.DraggableResult.Dragged;
			}
			Log.Warning($"ButtonInvisibleDraggable: MouseDownedNode {info.MouseDownedNode} is not null, but Input.GetMouseButton(0) return false");
			return Widgets.DraggableResult.Idle;
		}
		return Widgets.DraggableResult.Idle;
	}
}
