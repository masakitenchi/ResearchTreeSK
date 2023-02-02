using Verse;

namespace ResearchTreeSK;

public class Settings : ModSettings
{
	public static bool ShowScrollbars = true;

	public static bool DebugMode = false;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ShowScrollbars, "ShowScrollbars", defaultValue: true);
		Scribe_Values.Look(ref DebugMode, "DebugMode", defaultValue: false);
	}
}
