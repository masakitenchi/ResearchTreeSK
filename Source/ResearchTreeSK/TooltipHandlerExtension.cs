using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

[StaticConstructorOnStartup]
public static class TooltipHandlerExtension
{
	private static readonly FieldInfo activeTipsField;

	private static readonly FieldInfo frameField;

	private static Dictionary<int, ActiveTip> ActiveTips => (Dictionary<int, ActiveTip>)activeTipsField.GetValue(null);

	private static int Frame => (int)frameField.GetValue(null);

	static TooltipHandlerExtension()
	{
		Type typeFromHandle = typeof(TooltipHandler);
		activeTipsField = typeFromHandle.GetField("activeTips", BindingFlags.Static | BindingFlags.NonPublic);
		frameField = typeFromHandle.GetField("frame", BindingFlags.Static | BindingFlags.NonPublic);
	}

	public static void TipRegion(Rect rect, TipSignal tip)
	{
		if (Event.current.type == EventType.Repaint && (tip.textGetter != null || !tip.text.NullOrEmpty()) && (rect.Contains(Event.current.mousePosition) || DebugViewSettings.drawTooltipEdges))
		{
			if (DebugViewSettings.drawTooltipEdges)
			{
				Widgets.DrawBox(rect);
			}
			if (!ActiveTips.ContainsKey(tip.uniqueId))
			{
				ActiveTipExt value = new ActiveTipExt(tip);
				ActiveTips.Add(tip.uniqueId, value);
				ActiveTips[tip.uniqueId].firstTriggerTime = Time.realtimeSinceStartup;
			}
			ActiveTips[tip.uniqueId].lastTriggerFrame = Frame;
			ActiveTips[tip.uniqueId].signal.text = tip.text;
			ActiveTips[tip.uniqueId].signal.textGetter = tip.textGetter;
		}
	}
}
