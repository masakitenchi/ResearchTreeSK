using System;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class FloatMenuOption_NoClose : FloatMenuOption
{
	public FloatMenuOption_NoClose(string label, Action action)
		: base(label, action)
	{
	}

	public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
	{
		base.DoGUI(rect, colonistOrdering, floatMenu);
		return false;
	}
}
