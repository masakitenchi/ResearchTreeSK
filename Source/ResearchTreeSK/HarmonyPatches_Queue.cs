using HarmonyLib;
using RimWorld;
using Verse;

namespace ResearchTreeSK;

public class HarmonyPatches_Queue
{
	[HarmonyPatch(typeof(ResearchManager), "FinishProject")]
	public class ResearchManager_FinishProjectPatch
	{
		private static void Prefix(ResearchProjectDef proj, ref bool doCompletionDialog)
		{
			doCompletionDialog = false;
			Node node = proj.TryGetNode();
			if (node != null)
			{
				node.Completed = node.Research.IsFinished;
			}
			Queue.OnFinishProject(proj);
		}
	}
}
