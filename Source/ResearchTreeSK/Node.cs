using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class Node
{
	public class UnlockedDef
	{
		public readonly Def? Def;

		public readonly Rect Rect;

		private readonly Texture2D? Texture;

		private readonly string Description;

		public UnlockedDef(Def? def, string desc, int positionByRight)
		{
			Def = def;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(desc);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("ResearchTreeSK.ALClickOpenInfoCard".Translate());
			Description = stringBuilder.ToString().TrimEndNewlines();
			Rect = new Rect(IconsRect.xMax - (float)(positionByRight + 1) * (Constants.IconSize.x + 4f), IconsRect.yMin + (IconsRect.height - Constants.IconSize.y) / 2f, Constants.IconSize.x, Constants.IconSize.y);
			if (def == null)
			{
				Rect.x = IconsRect.x + 4f;
				Texture = Assets.MoreIcon;
			}
			else
			{
				Texture = def.IconTexture();
			}
		}

		public void Draw(bool showTip)
		{
			if (Def != null)
			{
				GUI.color = Def.IconColor();
			}
			else
			{
				GUI.color = Color.white;
			}
			if (Texture != null)
			{
				GUI.DrawTexture(Rect, Texture, ScaleMode.ScaleToFit);
			}
			if (showTip)
			{
				TooltipHandlerExtension.TipRegion(Rect, Description);
			}
		}
	}

	private bool _largeLabel;

	public readonly ResearchProjectDef Research;

	public readonly List<UnlockedDef> Unlocks = new List<UnlockedDef>();

	private readonly string TooltipInTreeNotQueued;

	private readonly string TooltipInTreeQueued;

	private readonly string TooltipInQueue;

	private readonly string TooltipEditMode;

	private string TooltipMissingFacilities = "";

	private string TooltipNotPoweredFacilities = "";

	private string TooltipMissingTechprints = "";

	private string TooltipMissingMechaniatorInColony = "";
	private string TooltipMissingStudiedThings = "";

	private bool BuildingPresent;

	private bool TechprintAvailable;

	private bool NeedMechaniator;
	private bool StudiedThingsRequirementsMet;

	private Vector2 topLeft;

	public Rect Rect;

	public bool Completed;

	public bool Available;

	private DateTime AnimatedHighlightStartTime = DateTime.MinValue;

	public IEnumerable<Node> Parents
	{
		get
		{
			if (Research.prerequisites != null)
			{
				return Research.prerequisites.Select((ResearchProjectDef r) => r.Node());
			}
			return new List<Node>();
		}
	}

	public IEnumerable<Node> Children => from r in Research.Descendants()
		select r.Node();

	public static Rect InnerRect { get; private set; }

	public static Rect CostIconRect { get; private set; }

	public static Rect CostLabelRect { get; private set; }

	public static Rect IconsRect { get; private set; }

	public static Rect LabelRect { get; private set; }

	public static Rect QueueLabelRect { get; private set; }

	public Vector2 TopLeft
	{
		get
		{
			return topLeft;
		}
		set
		{
			topLeft = value;
			Rect = new Rect(topLeft, ModOptions.NodeSize);
		}
	}

	public Rect QueueRect { get; set; }

	public int X { get; set; }

	public int Y { get; set; }

	public bool PositionChanged { get; set; } = false;


	public IntVec2 Pos => new IntVec2(X, Y);

	public bool Highlighted { get; set; }

	public string Label => Research.LabelCap;

	public Node(ResearchProjectDef research)
	{
		Research = research;
		X = Mathf.RoundToInt(research.researchViewX);
		Y = Mathf.RoundToInt(research.researchViewY);
		if ((double)Math.Abs((float)X - research.researchViewX) >= 0.0001 || (double)Math.Abs((float)Y - research.researchViewY) >= 0.0001)
		{
			Log.Warning("\t {0} position was fixed from ({1}, {2}) to ({3}, {4})", Label, research.researchViewX, research.researchViewY, X, Y);
		}
		List<Pair<Def, string>> unlockDefsAndDescs = Research.GetUnlockDefsAndDescs();
		for (int i = 0; i < unlockDefsAndDescs.Count; i++)
		{
			UnlockedDef unlockedDef = new UnlockedDef(unlockDefsAndDescs[i].First, unlockDefsAndDescs[i].Second, i);
			if (unlockedDef.Rect.xMin - Constants.IconSize.x < IconsRect.xMin && i + 1 < unlockDefsAndDescs.Count)
			{
				string desc = string.Join("\n", (from p in unlockDefsAndDescs.GetRange(i, unlockDefsAndDescs.Count - i)
					select p.Second).ToArray());
				unlockedDef = new UnlockedDef(null, desc, i);
				Unlocks.Add(unlockedDef);
				break;
			}
			Unlocks.Add(unlockedDef);
		}
		UpdateCaches();
		TooltipInTreeNotQueued = GetResearchTooltipString(Research, isQueued: false, drawInQueue: false, editMode: false);
		TooltipInTreeQueued = GetResearchTooltipString(Research, isQueued: true, drawInQueue: false, editMode: false);
		TooltipInQueue = GetResearchTooltipString(Research, isQueued: true, drawInQueue: true, editMode: false);
		TooltipEditMode = GetResearchTooltipString(Research, isQueued: false, drawInQueue: false, editMode: true);
	}

	public void UpdateCaches()
	{
		Completed = Research.IsFinished;
		BuildingPresent = Research.BuildingPresent();
		NeedMechaniator = Research.requiresMechanitor;
		StudiedThingsRequirementsMet = Research.StudiedThingsRequirementsMet;
        Available = !Completed && BuildingPresent && Research.TechprintAvailable() && (!NeedMechaniator || Research.PlayerMechanitorRequirementMet & Research.StudiedThingsRequirementsMet);
		var (list, list2) = Research.MissingAndNotPoweredFacilities();
		if (list.Count > 0)
		{
			TooltipMissingFacilities = "ResearchTreeSK.MissingFacilities".Translate(string.Join(", ", list.Select((ThingDef td) => td.LabelCap).ToArray()));
		}
		else
		{
			TooltipMissingFacilities = "";
		}
		if (list2.Count > 0)
		{
			TooltipNotPoweredFacilities = "ResearchTreeSK.NotPoweredFacilities".Translate(string.Join(", ", list2.Select((ThingDef td) => td.LabelCap).ToArray()));
		}
		else
		{
			TooltipNotPoweredFacilities = "";
		}
		TooltipMissingTechprints = "ResearchTreeSK.MissingTechprints".Translate(Research.TechprintsApplied, Research.techprintCount);
		if(NeedMechaniator && !Research.PlayerMechanitorRequirementMet)
		{
			TooltipMissingMechaniatorInColony = "ResearchTreeSK.NoColonistMechaniator".Translate();
        }
		else
		{
			TooltipMissingMechaniatorInColony = "";
		}
		if(NeedMechaniator && !Research.StudiedThingsRequirementsMet)
		{
			TooltipMissingStudiedThings = TranslatorFormattedStringExtensions.Translate("ResearchTreeSK.MissingStudiedThings", Research.requiredStudied.Select((ThingDef t) => "NotStudied".Translate(t.LabelCap).ToString()).ToLineList());
		}
		else
		{
			TooltipMissingStudiedThings = "";
		}
		TechprintAvailable = Research.TechprintAvailable();
	}

	public static void SetStaticRects()
	{
		InnerRect = new Rect(Vector2.zero, ModOptions.NodeSize);
		LabelRect = new Rect(6f, 3f, ModOptions.NodeSize.x - 32f - 12f - 6f, ModOptions.NodeSize.y * 0.5f - 3f);
		CostLabelRect = new Rect(ModOptions.NodeSize.x - 32f - 6f, 3f, 32f, ModOptions.NodeSize.y * 0.5f - 3f);
		CostIconRect = new Rect(ModOptions.NodeSize.x / 2f - (float)(Assets.Lock.width / 2), ModOptions.NodeSize.y / 2f - (float)(Assets.Lock.height / 2), Assets.Lock.width, Assets.Lock.height);
		IconsRect = new Rect(0f, ModOptions.NodeSize.y * 0.5f, ModOptions.NodeSize.x, ModOptions.NodeSize.y * 0.5f);
		QueueLabelRect = new Rect(ModOptions.NodeSize.x - 15f, (ModOptions.NodeSize.y - 30f) / 2f, 30f, 30f);
	}

	public void CalcLabelSize()
	{
		_largeLabel = Text.CalcHeight(Label, LabelRect.width) > LabelRect.height;
	}

	public void TopLeftReset()
	{
		TopLeft = new Vector2((float)X * ModOptions.NodeFullSize.x + ModOptions.NodeMargins.x / 2f, (float)Y * ModOptions.NodeFullSize.y + ModOptions.NodeMargins.y / 2f);
	}

	public override string ToString()
	{
		return $"{Label}({X}, {Y})";
	}

	public static bool IsVisible(Rect rect, Rect visibleRect)
	{
		return !(rect.xMin > visibleRect.xMax) && !(rect.xMax < visibleRect.xMin) && !(rect.yMin > visibleRect.yMax) && !(rect.yMax < visibleRect.yMin);
	}

	public int Matches(string query)
	{
		string query2 = query;
		CultureInfo culture = CultureInfo.CurrentUICulture;
		query2 = query2.ToLower(culture);
		if (Research.LabelCap.RawText.ToLower(culture).Contains(query2))
		{
			return 1;
		}
		if (Research.GetUnlockDefsAndDescs().Any<Pair<Def, string>>((Pair<Def, string> unlock) => unlock.First.LabelCap.RawText.ToLower(culture).Contains(query2)))
		{
			return 2;
		}
		if (Research.description.ToLower(culture).Contains(query2))
		{
			return 3;
		}
		return 0;
	}

	public void SetAnimatedHighlight()
	{
		AnimatedHighlightStartTime = DateTime.Now;
	}

	public void Draw(Rect visibleRect, bool isDragged, bool drawInQueue)
	{
		Rect rect = ((!drawInQueue) ? Rect : QueueRect);
		bool flag = visibleRect.Contains(Event.current.mousePosition);
		if (!IsVisible(rect, visibleRect))
		{
			return;
		}
		bool flag2 = drawInQueue || Tree.DetailedMode;
		bool flag3 = Completed || DebugSettings.godMode || Available;
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}
		if (!drawInQueue && Tree.EditMode && Tree.SelectedNodes.Contains(this))
		{
			Rect position = rect.ExpandedBy(3f);
			Widgets.DrawRectFast(position, ModOptions.Colors.Selected);
		}
		DateTime dateTime = AnimatedHighlightStartTime.AddSeconds(3.0);
		if (!drawInQueue && dateTime > DateTime.Now)
		{
			Color animated = ModOptions.Colors.Animated;
			DateTime dateTime2 = AnimatedHighlightStartTime.AddSeconds(0.30000001192092896);
			if (DateTime.Now < dateTime2)
			{
				animated.a *= (float)((DateTime.Now - AnimatedHighlightStartTime).TotalSeconds / 0.30000001192092896);
			}
			DateTime dateTime3 = dateTime.AddSeconds(-0.699999988079071);
			if (dateTime3 < DateTime.Now)
			{
				animated.a *= (float)((dateTime - DateTime.Now).TotalSeconds / 0.699999988079071);
			}
			Rect position2 = rect.ExpandedBy(5f);
			Widgets.DrawRectFast(position2, animated);
		}
		GUI.BeginGroup(rect);
		Color color = (Highlighted ? ModOptions.Colors.Highlighted : ((!flag3) ? ModOptions.Colors.Unavailable : ModOptions.Colors.BackgroundColor(Research.techLevel)));
		GUI.color = Color.white;
		Widgets.DrawRectFast(InnerRect, color);
		GUI.DrawTexture(InnerRect, Assets.NodeTextures[Research.techLevel]);
		if (flag3)
		{
			Rect position3 = new Rect(0f, 0f, Research.ProgressPercent * InnerRect.width, InnerRect.height);
			GUI.BeginGroup(position3);
			GUI.DrawTexture(InnerRect, Assets.ProgressTextures[Research.techLevel]);
			GUI.EndGroup();
		}
		if (flag3)
		{
			GUI.color = Color.white;
		}
		else
		{
			GUI.color = Color.grey;
		}
		if (flag2)
		{
			GUIStyle gUIStyle = (_largeLabel ? Text.fontStyles[0] : Text.fontStyles[1]);
			gUIStyle.alignment = TextAnchor.UpperLeft;
			gUIStyle.wordWrap = false;
			GUI.Label(LabelRect, Research.LabelCap, gUIStyle);
		}
		else
		{
			GUIStyle gUIStyle2 = (_largeLabel ? Text.fontStyles[1] : Text.fontStyles[2]);
			gUIStyle2.alignment = TextAnchor.MiddleCenter;
			gUIStyle2.wordWrap = false;
			GUI.Label(InnerRect, Research.LabelCap, gUIStyle2);
		}
		if (flag2)
		{
			GUIStyle gUIStyle3 = ((Research.CostApparent > 9999f) ? Text.fontStyles[0] : Text.fontStyles[1]);
			gUIStyle3.alignment = TextAnchor.UpperRight;
			gUIStyle3.wordWrap = false;
			GUI.Label(CostLabelRect, Research.CostApparent.ToStringByStyle(ToStringStyle.Integer), gUIStyle3);
		}
		Text.WordWrap = true;
		if (flag && !isDragged)
		{
			if (!Tree.EditMode)
			{
				if (!Queue.IsQueued(Research))
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipInTreeNotQueued);
				}
				else if (!drawInQueue)
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipInTreeQueued);
				}
				else
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipInQueue);
				}
				if (TooltipMissingFacilities != "")
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipMissingFacilities);
				}
				if (TooltipNotPoweredFacilities != "")
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipNotPoweredFacilities);
				}
				if (BuildingPresent && !TechprintAvailable)
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipMissingTechprints);
				}
				if(TooltipMissingMechaniatorInColony !="")
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipMissingMechaniatorInColony);
				}
				if(TooltipMissingStudiedThings != "")
				{
					TooltipHandlerExtension.TipRegion(InnerRect, TooltipMissingStudiedThings);
				}
			}
			else
			{
				TooltipHandlerExtension.TipRegion(InnerRect, TooltipEditMode);
			}
		}
		if (flag2)
		{
			foreach (UnlockedDef unlock in Unlocks)
			{
				unlock.Draw(flag && !isDragged);
			}
			if (!flag3)
			{
				GUI.color = Color.white;
				GUI.DrawTexture(CostIconRect, Assets.Lock, ScaleMode.ScaleToFit);
			}
		}
		GUI.EndGroup();
	}

	public List<Node> GetParentsRecursive()
	{
		IEnumerable<Node> enumerable = Research.prerequisites?.Select((ResearchProjectDef rpd) => rpd.Node());
		if (enumerable == null)
		{
			return new List<Node>();
		}
		List<Node> list = new List<Node>(enumerable);
		foreach (Node item in enumerable)
		{
			list.AddRange(item.GetParentsRecursive());
		}
		return list.Distinct().ToList();
	}

	private static string GetResearchTooltipString(ResearchProjectDef Research, bool isQueued, bool drawInQueue, bool editMode)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (!Research.description.StartsWith("description"))
		{
			stringBuilder.AppendLine(Research.description);
			stringBuilder.AppendLine();
		}
		if (!editMode)
		{
			if (isQueued)
			{
				stringBuilder.AppendLine("ResearchTreeSK.LClickRemoveFromQueue".Translate());
				if (drawInQueue)
				{
					stringBuilder.AppendLine("ResearchTreeSK.CLClickDragInQueue".Translate());
				}
			}
			else
			{
				stringBuilder.AppendLine("ResearchTreeSK.LClickReplaceQueue".Translate());
				stringBuilder.AppendLine("ResearchTreeSK.SLClickAddToQueue".Translate());
				stringBuilder.AppendLine("ResearchTreeSK.CLClickAddToQueue".Translate());
			}
			if (DebugSettings.godMode)
			{
				stringBuilder.AppendLine("ResearchTreeSK.RClickInstaFinish".Translate());
			}
		}
		else
		{
			stringBuilder.AppendLine("ResearchTreeSK.ClickEditMode".Translate());
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}
}
