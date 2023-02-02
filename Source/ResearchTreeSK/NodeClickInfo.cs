using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ResearchTreeSK;

public class NodeClickInfo
{
    public Node? MouseOverNode { get; private set; } = null;


	public Node? MouseDownedNode { get; private set; } = null;

    public void HandleDown(IEnumerable<Node> nodes, Func<Node, Rect> getRect)
	{
		MouseOverNode = null;
		foreach (Node node in nodes)
		{
			if (getRect(node).Contains(Event.current.mousePosition))
			{
				MouseOverNode = node;
				break;
			}
		}
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			if (MouseOverNode != null)
			{
				MouseDownedNode = MouseOverNode;
			}
			else
			{
				MouseDownedNode = null;
			}
		}
	}

	public void HandleUp()
	{
		if (Input.GetMouseButtonUp(0))
		{
			MouseDownedNode = null;
		}
	}
}
