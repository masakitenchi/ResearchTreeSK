using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ResearchTreeSK;

public sealed class ResearchLetter : ChoiceLetter
{
	private ResearchProjectDef researchDef;

	public override IEnumerable<DiaOption> Choices
	{
		get
		{
			yield return new DiaOption("OK".Translate())
			{
				resolveTree = true,
				action = delegate
				{
					Find.LetterStack.RemoveLetter(this);
				}
			};
			yield return new DiaOption("ResearchScreen".Translate())
			{
				resolveTree = true,
				action = delegate
				{
					Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Research);
					Tree.CenterOn(researchDef.Node());
				}
			};
		}
	}

	public ResearchLetter()
	{
	}

	public ResearchLetter(ResearchProjectDef current, ResearchProjectDef? next)
	{
		researchDef = current;
		ID = Find.UniqueIDsManager.GetNextLetterID();
		base.Label = "ResearchFinished".Translate(current.LabelCap);
		base.Text = current.LabelCap + "\n\n" + current.description;
		if (next != null)
		{
			base.Text += "\n\n" + "ResearchTreeSK.NextInQueue".Translate(next!.LabelCap);
			def = LetterDefOf.PositiveEvent;
		}
		else
		{
			base.Text += "\n\n" + "ResearchTreeSK.NextInQueue".Translate("ResearchTreeSK.None".Translate());
			def = LetterDefOf.NeutralEvent;
		}
		radioMode = true;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref researchDef, "researchDef");
	}

	public override void OpenLetter()
	{
		DiaNode diaNode = new DiaNode(string.Empty);
		diaNode.options.AddRange(Choices);
		Find.WindowStack.Add(new Dialog_ResearchInfo(researchDef, diaNode, delayInteractivity: true));
	}
}
