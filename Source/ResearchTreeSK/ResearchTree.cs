using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class ResearchTree : Mod
{
	public ResearchTree(ModContentPack content)
		: base(content)
	{
		Harmony harmony = new Harmony("qwerty19106.ResearchTreeSK");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
		GetSettings<Settings>();
	}

	public override string SettingsCategory()
	{
		return "ResearchTreeSK";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Listing_Standard listing_Standard = new Listing_Standard(GameFont.Small)
		{
			ColumnWidth = inRect.width
		};
		listing_Standard.Begin(inRect);
		listing_Standard.CheckboxLabeled("ResearchTreeSK.ShowScrollbars".Translate(), ref Settings.ShowScrollbars);
		listing_Standard.CheckboxLabeled("ResearchTreeSK.DebugMode".Translate(), ref Settings.DebugMode);
		listing_Standard.End();
	}
}
