using RimWorld;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class MainButtonWorker_ResearchTree : MainButtonWorker_ToggleResearchTab
{
	public override void DoButton(Rect rect)
	{
		base.DoButton(rect);
		if (Queue.NumQueued > 0)
		{
			Rect canvas = new Rect(rect.xMax - 20f - 6f, 0f, 20f, 20f).CenteredOnYIn(rect);
			Queue.DrawLabel(canvas, Color.white, Color.grey, Queue.NumQueued);
		}
	}
}
