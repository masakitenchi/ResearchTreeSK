using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ResearchTreeSK;

public static class Tree
{
	private static Rect baseViewRect = Rect.zero;

	private static Rect FullRect = Rect.zero;

	private static Vector2 scrollPosition = Vector2.zero;

	private static float MaxZoomLevel;

	private static bool editMode = false;

	public static readonly List<Node> SelectedNodes = new List<Node>();

	private static readonly NodeClickInfo NodeClickInfo = new NodeClickInfo();

	private static readonly List<Window> WindowsToShow = new List<Window>();

	public static bool Initialized;

	private static bool _initializing;

	public static readonly Dictionary<ResearchProjectDef, Node> ResearchsToNodesCache = new Dictionary<ResearchProjectDef, Node>();

	public static IntVec2 Size = IntVec2.Zero;

	public static IntVec2 SizeSK = IntVec2.Zero;

	public static readonly Dictionary<TechLevel, int> TechLevelLeftBounds = new Dictionary<TechLevel, int>();

	public static List<Node> NodesSK = new List<Node>();

	private static List<Node> NodesOtherList = new List<Node>();

	private static readonly Dictionary<ResearchTabDef, List<Node>> NodesOther = new Dictionary<ResearchTabDef, List<Node>>();

	private static readonly Dictionary<ResearchTabDef, RangeInt> TabOtherYRanges = new Dictionary<ResearchTabDef, RangeInt>();

	private static List<ResearchTabDef> TabsOther = new List<ResearchTabDef>();

	private static Rect ViewRect => new Rect(baseViewRect.min * ZoomLevel, baseViewRect.size * ZoomLevel);

	private static Vector2 ScrollPositionMax => FullRect.size - ViewRect.size;

	public static Vector2 ScrollPosition
	{
		get
		{
			return scrollPosition;
		}
		set
		{
			scrollPosition = new Vector2(Mathf.Clamp(value.x, 0f, (ScrollPositionMax.x > 0f) ? ScrollPositionMax.x : 0f), Mathf.Clamp(value.y, 0f, (ScrollPositionMax.y > 0f) ? ScrollPositionMax.y : 0f));
		}
	}

	public static float ZoomLevel { get; private set; } = 1f;


	private static GameFont LabelFont => (ZoomLevel < 1.5f) ? GameFont.Small : GameFont.Medium;

	public static bool DetailedMode => ZoomLevel < 1.5f;

	public static bool EditMode => editMode;

	public static List<TechLevel> RelevantTechLevels { get; private set; } = (from TechLevel tl in Enum.GetValues(typeof(TechLevel))
		where DefDatabase<ResearchProjectDef>.AllDefsListForReading.Any((ResearchProjectDef rp) => rp.techLevel == tl)
		select tl).ToList();


	public static List<Node> Nodes { get; private set; } = new List<Node>();


	public static void UpdateRects()
	{
		float width = (float)Size.x * ModOptions.NodeFullSize.x;
		float height = (float)Size.z * ModOptions.NodeFullSize.y;
		FullRect = new Rect(0f, 0f, width, height);
		baseViewRect = new Rect(18f, ModOptions.TopBarHeight + 6f + 18f, (float)Screen.width / Prefs.UIScale - 36f, (float)Screen.height / Prefs.UIScale - 35f - 36f - ModOptions.TopBarHeight - 6f);
		if (Settings.ShowScrollbars)
		{
			baseViewRect.width -= 18f;
			baseViewRect.height -= 18f;
		}
		float a = Mathf.Max(FullRect.width / baseViewRect.width, 0f);
		MaxZoomLevel = Mathf.Min(a, ModOptions.Params.AbsoluteMaxZoomLevel);
		ScrollPosition = scrollPosition;
	}

	private static void HandleMouseClick(Rect visibleRect, Vector2 delta)
	{
		if (visibleRect.Contains(Event.current.mousePosition))
		{
			NodeClickInfo.HandleDown(Nodes, (Node n) => n.Rect);
		}
		else
		{
			NodeClickInfo.HandleDown(new List<Node>(), (Node n) => n.Rect);
		}
		delta *= ZoomLevel;
		foreach (Node node in Nodes)
		{
			node.Highlighted = false;
		}
		Edge.ClearHighlited();
		if (NodeClickInfo.MouseOverNode != null)
		{
			NodeClickInfo.MouseOverNode!.Highlighted = true;
			Edge.SetHighlited(NodeClickInfo.MouseOverNode);
			foreach (Node item in NodeClickInfo.MouseOverNode!.GetParentsRecursive())
			{
				Edge.SetHighlited(item);
				item.Highlighted = true;
			}
		}
		if (EditMode)
		{
			if (NodeClickInfo.MouseOverNode != null)
			{
				MouseoverSounds.DoRegion(NodeClickInfo.MouseOverNode!.Rect);
			}
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				SelectedNodes.Clear();
			}
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.shift && NodeClickInfo.MouseDownedNode != null && NodesSK.Contains(NodeClickInfo.MouseDownedNode) && !SelectedNodes.Contains(NodeClickInfo.MouseDownedNode))
			{
				SelectedNodes.Clear();
				SelectedNodes.Add(NodeClickInfo.MouseDownedNode);
			}
			Widgets.DraggableResult draggableResult = ButtonInvisibleDraggable.Handle(NodeClickInfo);
			if (draggableResult != 0)
			{
				Node mouseDownedNode = NodeClickInfo.MouseDownedNode;
				switch (draggableResult)
				{
				case Widgets.DraggableResult.Pressed:
					if (!NodesSK.Contains(mouseDownedNode))
					{
						break;
					}
					if (Event.current.shift)
					{
						if (SelectedNodes.Contains(mouseDownedNode))
						{
							SelectedNodes.Remove(mouseDownedNode);
						}
						else
						{
							SelectedNodes.Add(mouseDownedNode);
						}
					}
					else
					{
						SelectedNodes.Clear();
						SelectedNodes.Add(mouseDownedNode);
					}
					break;
				case Widgets.DraggableResult.Dragged:
					if (!SelectedNodes.Contains(mouseDownedNode))
					{
						break;
					}
					foreach (Node selectedNode in SelectedNodes)
					{
						selectedNode.TopLeft = new Vector2(selectedNode.Rect.x + delta.x, selectedNode.Rect.y + delta.y);
					}
					break;
				case Widgets.DraggableResult.DraggedThenPressed:
					if (!SelectedNodes.Contains(mouseDownedNode))
					{
						break;
					}
					foreach (Node selectedNode2 in SelectedNodes)
					{
						int num = Mathf.RoundToInt((selectedNode2.Rect.x - ModOptions.NodeMargins.x / 2f) / ModOptions.NodeFullSize.x);
						int num2 = Mathf.RoundToInt((selectedNode2.Rect.y - ModOptions.NodeMargins.y / 2f) / ModOptions.NodeFullSize.y);
						if (selectedNode2.X != num || selectedNode2.Y != num2)
						{
							Log.Trace("\t" + selectedNode2?.ToString() + " X: " + selectedNode2.X + " -> " + num);
							Log.Trace("\t" + selectedNode2?.ToString() + " Y: " + selectedNode2.Y + " -> " + num2);
							selectedNode2.X = Mathf.Max(num, 0);
							selectedNode2.Y = Mathf.Max(num2, 0);
							selectedNode2.PositionChanged = true;
						}
						selectedNode2.TopLeftReset();
					}
					UpdateSize();
					CalcTechLevelsSK();
					OrderAndPlaceOtherNodes();
					CreateEdges();
					UpdateRects();
					break;
				}
			}
		}
		else if (NodeClickInfo.MouseOverNode != null)
		{
			if (Event.current.alt)
			{
				if (DetailedMode && Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					GUI.BeginGroup(NodeClickInfo.MouseOverNode!.Rect);
					foreach (Node.UnlockedDef unlock in NodeClickInfo.MouseOverNode!.Unlocks)
					{
						if (unlock.Def != null && unlock.Rect.Contains(Event.current.mousePosition))
						{
							WindowsToShow.Add(new Dialog_InfoCard(unlock.Def));
						}
					}
					GUI.EndGroup();
				}
			}
			else if (Widgets.ButtonInvisible(NodeClickInfo.MouseOverNode!.Rect))
			{
				if (NodeClickInfo.MouseOverNode!.Available && Event.current.button == 0 && !NodeClickInfo.MouseOverNode!.Research.IsFinished)
				{
					if (!Queue.IsQueued(NodeClickInfo.MouseOverNode!.Research))
					{
						IEnumerable<ResearchProjectDef> researches = NodeClickInfo.MouseOverNode!.Research.GetIncompleteParentsRecursive().Concat<ResearchProjectDef>(new List<ResearchProjectDef>(new ResearchProjectDef[1] { NodeClickInfo.MouseOverNode!.Research })).Distinct();
						if (Event.current.control)
						{
							Queue.InsertAtBeginningRange(researches);
						}
						else
						{
							Queue.EnqueueRange(researches, Event.current.shift);
						}
					}
					else
					{
						Queue.Dequeue(NodeClickInfo.MouseOverNode!.Research);
					}
				}
				if (DebugSettings.godMode && !NodeClickInfo.MouseOverNode!.Completed && Event.current.button == 1)
				{
					Find.ResearchManager.FinishProject(NodeClickInfo.MouseOverNode!.Research);
					Queue.Dequeue(NodeClickInfo.MouseOverNode!.Research);
				}
			}
		}
		if (NodeClickInfo.MouseOverNode == null && NodeClickInfo.MouseDownedNode == null && Queue.NodeClickInfo.MouseDownedNode == null && !Event.current.control && !Event.current.shift && visibleRect.Contains(Event.current.mousePosition))
		{
			ScrollPosition = scrollPosition - delta;
		}
		NodeClickInfo.HandleUp();
	}

	private static void HandleZoom(Rect visibleRect)
	{
		if (visibleRect.Contains(Event.current.mousePosition) && Event.current.isScrollWheel)
		{
			Vector2 mousePosition = Event.current.mousePosition;
			Vector2 vector = (Event.current.mousePosition - ScrollPosition) / ZoomLevel;
			float value = ZoomLevel * (1f + Event.current.delta.y * 0.05f);
			ZoomLevel = Mathf.Clamp(value, 1f, MaxZoomLevel);
			ScrollPosition = mousePosition - vector * ZoomLevel;
			Event.current.Use();
		}
	}

	private static void HandleDolly()
	{
		float num = 10f;
		if (KeyBindingDefOf.MapDolly_Left.IsDown)
		{
			ScrollPosition = new Vector2(ScrollPosition.x - num, ScrollPosition.y);
		}
		if (KeyBindingDefOf.MapDolly_Right.IsDown)
		{
			ScrollPosition = new Vector2(ScrollPosition.x + num, ScrollPosition.y);
		}
		if (KeyBindingDefOf.MapDolly_Up.IsDown)
		{
			ScrollPosition = new Vector2(ScrollPosition.x, ScrollPosition.y - num);
		}
		if (KeyBindingDefOf.MapDolly_Down.IsDown)
		{
			ScrollPosition = new Vector2(ScrollPosition.x, ScrollPosition.y + num);
		}
	}

	public static void DoContents(ref Rect windowRect, Vector2 delta)
	{
		GUIClipUtility.BeginNoClip();
		if (Settings.ShowScrollbars)
		{
			Rect position = new Rect(baseViewRect.xMin, baseViewRect.yMax, baseViewRect.width, 18f);
			float value = ScrollPosition.x / FullRect.width;
			float size = Mathf.Clamp(ViewRect.width / FullRect.width, 0f, 1f);
			value = GUI.HorizontalScrollbar(position, value, size, 0f, 1f);
			Rect position2 = new Rect(baseViewRect.xMax, baseViewRect.yMin, 18f, baseViewRect.height);
			float value2 = ScrollPosition.y / FullRect.height;
			float size2 = Mathf.Clamp(ViewRect.height / FullRect.height, 0f, 1f);
			value2 = GUI.VerticalScrollbar(position2, value2, size2, 0f, 1f);
			ScrollPosition = new Vector2(value * FullRect.width, value2 * FullRect.height);
		}
		GUIClipUtility.BeginZoom(ZoomLevel);
		windowRect.width = UI.screenWidth;
		windowRect.height = UI.screenHeight - 35;
		GUIClipUtility.BeginScrollView(ViewRect, ScrollPosition, FullRect);
		Rect visibleRect = new Rect(ScrollPosition, ViewRect.size);
		Draw(visibleRect);
		if (!EditMode)
		{
			Queue.DrawLabels(visibleRect);
		}
		HandleMouseClick(visibleRect, delta);
		HandleZoom(visibleRect);
		HandleDolly();
		if (Event.current.type == EventType.MouseDown && visibleRect.Contains(Event.current.mousePosition))
		{
			Event.current.Use();
		}
		GUIClipUtility.EndScrollView();
		GUIClipUtility.EndZoom();
		windowRect.width = UI.screenWidth;
		windowRect.height = UI.screenHeight - 35;
		GUIClipUtility.EndNoClip();
		foreach (Window item in WindowsToShow)
		{
			Find.WindowStack.Add(item);
		}
		WindowsToShow.Clear();
	}

	private static void Draw(Rect visibleRect)
	{
		foreach (TechLevel relevantTechLevel in RelevantTechLevels)
		{
			DrawTechLevel(relevantTechLevel, visibleRect);
		}
		Edge.Draw(visibleRect);
		foreach (Node node in Nodes)
		{
			node.Draw(visibleRect, node == NodeClickInfo.MouseDownedNode, drawInQueue: false);
		}
		if (TabsOther.Count <= 0)
		{
			return;
		}
		DrawSKTab(visibleRect);
		ResearchTabDef researchTabDef = TabsOther.Last();
		foreach (ResearchTabDef item in TabsOther)
		{
			if (item == researchTabDef)
			{
				DrawTab(item, visibleRect, showOnlyTop: true);
			}
			else
			{
				DrawTab(item, visibleRect, showOnlyTop: false);
			}
		}
	}

	private static string MakePatch(IEnumerable<Node> nodes, bool template)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (template)
		{
			foreach (Node node in nodes)
			{
				stringBuilder.AppendLine($"<defName>{node.Research.defName}\n\t\t<researchViewX>{node.X:F2}</researchViewX>\n\t\t<researchViewY>{node.Y:F2}</researchViewY>\n");
			}
		}
		else
		{
			stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Patch>\n    <Operation Class=\"PatchOperationSequence\">\n        <operations>");
			foreach (Node node2 in nodes)
			{
				stringBuilder.AppendLine(string.Format("\n            <li Class=\"PatchOperationReplace\">\n                <xpath>Defs/ResearchProjectDef[defName=\"{0}\"]/researchViewX</xpath>\n                <value>\n                    <researchViewX>{1:F2}</researchViewX>\n                </value>\n            </li>\n            <li Class=\"PatchOperationReplace\">\n                <xpath>Defs/ResearchProjectDef[defName=\"{0}\"]/researchViewY</xpath>\n                <value>\n                    <researchViewY>{2:F2}</researchViewY>\n                </value>\n            </li>", node2.Research.defName, node2.X, node2.Y));
			}
			stringBuilder.Append("\n        </operations>\n    </Operation>\n</Patch>");
		}
		return stringBuilder.ToString();
	}

	public static void DrawEditModeButtons(Rect visibleRect)
	{
		if (!Prefs.DevMode)
		{
			return;
		}
		Rect rect = visibleRect;
		rect.yMax = rect.yMin + 20f;
		rect.xMin = rect.xMax - 140f;
		Rect rect2 = rect.RightPartPixels(30f);
		rect = rect.LeftPartPixels(rect.width - 30f);
		Rect rect3 = rect.RightPartPixels(30f);
		rect = rect.LeftPartPixels(rect.width - 30f);
		bool flag = editMode;
		Widgets.CheckboxLabeled(rect, "Edit", ref editMode);
		if (flag != editMode)
		{
			UpdateSize();
			UpdateRects();
		}
		TooltipHandler.TipRegion(rect2, "ResearchTreeSK.BtnPatchAllTooltip".Translate());
		if (Widgets.ButtonImageFitted(rect2, Assets.CopyIcon))
		{
			GUIUtility.systemCopyBuffer = MakePatch(NodesSK, template: false);
			Messages.Message("ResearchTreeSK.BtnPatchAllMessage".Translate(), MessageTypeDefOf.SituationResolved, historical: false);
		}
		TooltipHandler.TipRegion(rect3, "ResearchTreeSK.BtnPatchChangedTooltip".Translate());
		if (Widgets.ButtonImageFitted(rect3, Assets.CopyIcon))
		{
			GUIUtility.systemCopyBuffer = MakePatch(NodesSK.Where((Node n) => n.PositionChanged), template: true);
			Messages.Message("ResearchTreeSK.BtnPatchChangedMessage".Translate(), MessageTypeDefOf.SituationResolved, historical: false);
		}
	}

	public static void DrawTechLevel(TechLevel techlevel, Rect visibleRect)
	{
		TechLevel techLevel = RelevantTechLevels.First();
		TechLevel techLevel2 = RelevantTechLevels.Last();
		float num = ModOptions.NodeFullSize.x * (float)TechLevelLeftBounds[techlevel];
		GUI.color = Assets.TechLevelColor;
		Text.Anchor = TextAnchor.MiddleCenter;
		Text.Font = LabelFont;
		if (techlevel != techLevel)
		{
			if (num > visibleRect.xMin && num < visibleRect.xMax)
			{
				Widgets.DrawLine(new Vector2(num, visibleRect.yMin), new Vector2(num, visibleRect.yMax), Assets.TechLevelColor, 1f);
			}
			Rect rect = new Rect(num + Constants.TreeLabelDistance - Constants.TreeLabelSize.x / 2f, visibleRect.center.y - Constants.TreeLabelSize.y / 2f, Constants.TreeLabelSize.x, Constants.TreeLabelSize.y);
			if (rect.Overlaps(visibleRect))
			{
				VerticalLabel(rect, techlevel.ToStringHuman());
			}
		}
		if (techlevel != techLevel2)
		{
			TechLevel key = techlevel + 1;
			float num2 = ModOptions.NodeFullSize.x * (float)TechLevelLeftBounds[key];
			Rect rect2 = new Rect(num2 - Constants.TreeLabelDistance - Constants.TreeLabelSize.x / 2f, visibleRect.center.y - Constants.TreeLabelSize.y / 2f, Constants.TreeLabelSize.x, Constants.TreeLabelSize.y);
			if (rect2.Overlaps(visibleRect))
			{
				VerticalLabel(rect2, techlevel.ToStringHuman());
			}
		}
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static void VerticalLabel(Rect rect, string text)
	{
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = Matrix4x4.identity;
		GUIUtility.RotateAroundPivot(-90f, rect.center);
		GUI.matrix = matrix * GUI.matrix;
		Widgets.Label(rect, text);
		GUI.matrix = matrix;
	}

	private static void DrawSKTab(Rect visibleRect)
	{
		float num = ModOptions.NodeFullSize.y * (float)(SizeSK.z + 1);
		GUI.color = Assets.TechLevelColor;
		Text.Anchor = TextAnchor.MiddleCenter;
		Text.Font = LabelFont;
		Rect rect = new Rect(visibleRect.center.x - Constants.TreeLabelSize.x / 2f, num - Constants.TreeLabelDistance - Constants.TreeLabelSize.y / 2f, Constants.TreeLabelSize.x, Constants.TreeLabelSize.y);
		if (rect.Overlaps(visibleRect))
		{
			Widgets.Label(rect, "HardcoreSK");
		}
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static void DrawTab(ResearchTabDef tab, Rect visibleRect, bool showOnlyTop)
	{
		float num = ModOptions.NodeFullSize.y * (float)TabOtherYRanges[tab].start;
		float num2 = ModOptions.NodeFullSize.y * (float)(TabOtherYRanges[tab].end + 1);
		GUI.color = Assets.TechLevelColor;
		Text.Anchor = TextAnchor.MiddleCenter;
		Text.Font = LabelFont;
		if (num > visibleRect.yMin && num < visibleRect.yMax)
		{
			Widgets.DrawLine(new Vector2(visibleRect.xMin, num), new Vector2(visibleRect.xMax, num), Assets.TechLevelColor, 1f);
		}
		Rect rect = new Rect(visibleRect.center.x - Constants.TreeLabelSize.x / 2f, num + Constants.TreeLabelDistance - Constants.TreeLabelSize.y / 2f, Constants.TreeLabelSize.x, Constants.TreeLabelSize.y);
		if (rect.Overlaps(visibleRect))
		{
			Widgets.Label(rect, tab.LabelCap);
		}
		if (!showOnlyTop)
		{
			rect = new Rect(visibleRect.center.x - Constants.TreeLabelSize.x / 2f, num2 - Constants.TreeLabelDistance - Constants.TreeLabelSize.y / 2f, Constants.TreeLabelSize.x, Constants.TreeLabelSize.y);
			if (rect.Overlaps(visibleRect))
			{
				Widgets.Label(rect, tab.LabelCap);
			}
		}
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
	}

	public static List<Node> Row(int Y)
	{
		return Nodes.Where((Node n) => n.Y == Y).ToList();
	}

	public new static string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int X;
		for (X = 0; X <= Nodes.Max((Node n) => n.X); X++)
		{
			stringBuilder.AppendLine($"X {X}:");
			List<Node> list = Nodes.Where((Node n) => n.X == X).ToList();
			foreach (Node item in list)
			{
				stringBuilder.AppendLine($"\t{item}");
				stringBuilder.AppendLine("\t\tAbove: " + string.Join(", ", item.Parents.Select((Node a) => a.ToString()).ToArray()));
				stringBuilder.AppendLine("\t\tBelow: " + string.Join(", ", item.Children.Select((Node b) => b.ToString()).ToArray()));
			}
		}
		return stringBuilder.ToString();
	}

	public static void DebugStatus()
	{
		Log.Message("duplicated positions:\n " + string.Join("\n", (from n in Nodes
			where Nodes.Any<Node>((Node n2) => n != n2 && n.X == n2.X && n.Y == n2.Y)
			select n.X + ", " + n.Y + ": " + n.Label).ToArray()));
		Log.Message("out-of-bounds nodes:\n" + string.Join("\n", (from n in Nodes
			where n.X < 1 || n.Y < 1
			select n.ToString()).ToArray()));
		Log.Trace(ToString());
	}

	public static void CenterOn(Node node)
	{
		Vector2 vector = new Vector2(ModOptions.NodeFullSize.x * ((float)node.X + 0.5f), ModOptions.NodeFullSize.y * ((float)node.Y + 0.5f));
		ZoomLevel = 1f;
		vector = (ScrollPosition = vector - ViewRect.size / 2f);
		node.SetAnimatedHighlight();
	}

	private static void UpdateSize()
	{
		Size.x = Nodes.Max((Node n) => n.X) + 1 + (EditMode ? 5 : 0);
		Size.z = Nodes.Max((Node n) => n.Y) + 1 + (EditMode ? 5 : 0);
		SizeSK.x = ((NodesSK.Count > 0) ? NodesSK.Max((Node n) => n.X) : 0) + 1;
		SizeSK.z = ((NodesSK.Count > 0) ? NodesSK.Max((Node n) => n.Y) : 0) + 3;
		Log.Trace("Tree.UpdateSize: Size{0} SizeSK{1}", Size, SizeSK);
	}

	public static void Initialize()
	{
		if (!Initialized && !_initializing)
		{
			_initializing = true;
			Node.SetStaticRects();
			LongEventHandler.QueueLongEvent(PopulateNodes, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(CheckPrerequisites, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(CalcTechLevelsSK, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(OrderAndPlaceOtherNodes, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(CreateEdges, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(DebugStatus, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(UpdateRects, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(Queue.UpdateNodeRects, "ResearchTreeSK.PreparingTree.Setup", doAsynchronously: false, null);
			LongEventHandler.QueueLongEvent(delegate
			{
				Initialized = true;
			}, "ResearchTreeSK.PreparingTree.Layout", doAsynchronously: false, null);
		}
	}

	private static void PopulateNodes()
	{
		Log.Debug("Populating nodes.");
		List<ResearchProjectDef> allDefsListForReading = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
		IEnumerable<ResearchProjectDef> hidden = allDefsListForReading.Where((ResearchProjectDef p) => p.prerequisites?.Contains(p) ?? false);
		IEnumerable<ResearchProjectDef> second = allDefsListForReading.Where((ResearchProjectDef p) => p.Ancestors().Intersect<ResearchProjectDef>(hidden).Any());
		Nodes = (from def in allDefsListForReading.Except(hidden).Except(second)
			select new Node(def)).ToList();
		foreach (Node node in Nodes)
		{
			node.TopLeftReset();
			node.CalcLabelSize();
			ResearchsToNodesCache[node.Research] = node;
		}
		Log.Debug("\tNodes: {0}", Nodes.Count);
		NodesSK = (from n in Nodes.FindAll((Node n) => n.Research.HasModExtension<ResearchTreeSKModExtension>())
			orderby n.X, n.Y
			select n).ToList();
		Log.Trace("\tNodesSK: {0}", NodesSK.Count);
		Dictionary<IntVec2, Node> dictionary = new Dictionary<IntVec2, Node>();
		foreach (Node item in NodesSK)
		{
			if (dictionary.ContainsKey(item.Pos))
			{
				Log.Error("HardcoreSK research {0} have the same position ({1}) as {2}", false, item, item.Pos, dictionary[item.Pos]);
			}
			dictionary[item.Pos] = item;
		}
		NodesOtherList = Nodes.FindAll((Node n) => !n.Research.HasModExtension<ResearchTreeSKModExtension>());
		foreach (Node nodesOther in NodesOtherList)
		{
			ResearchTabDef tab = nodesOther.Research.tab;
			if (!NodesOther.ContainsKey(tab))
			{
				NodesOther[tab] = new List<Node>();
			}
			NodesOther[tab].Add(nodesOther);
			nodesOther.X = -1;
			nodesOther.Y = -1;
		}
		foreach (ResearchTabDef key in NodesOther.Keys)
		{
			TabOtherYRanges[key] = new RangeInt(0, 0);
		}
		TabsOther = NodesOther.Keys.ToList();
		TabsOther.Sort((ResearchTabDef x, ResearchTabDef y) => -NodesOther[x].Count.CompareTo(NodesOther[y].Count));
		ResearchTabDef named = DefDatabase<ResearchTabDef>.GetNamed("Main");
		if (TabsOther.Contains(named))
		{
			TabsOther.Remove(named);
			TabsOther.Add(named);
		}
		Log.Trace("\tNodesOther:");
		foreach (KeyValuePair<ResearchTabDef, List<Node>> item2 in NodesOther)
		{
			Log.Trace("\t\t{0} - {1}", item2.Key, item2.Value.Count);
		}
		UpdateSize();
	}

	private static void CheckPrerequisites()
	{
		Log.Debug("Checking prerequisites.");
		Queue<Node> queue = new Queue<Node>(Nodes);
		while (queue.Count > 0)
		{
			Node node2 = queue.Dequeue();
			if (node2.Research.prerequisites.NullOrEmpty())
			{
				continue;
			}
			List<ResearchProjectDef> first = node2.Research.prerequisites?.SelectMany((ResearchProjectDef r) => r.Ancestors()).ToList();
			IEnumerable<ResearchProjectDef> enumerable = first.Intersect(node2.Research.prerequisites);
			if (!enumerable.Any())
			{
				continue;
			}
			Log.Warning("\tredundant prerequisites for {0}: {1}", node2.Research.LabelCap, string.Join(", ", enumerable.Select((ResearchProjectDef r) => r.LabelCap).ToArray()));
			foreach (ResearchProjectDef item in enumerable)
			{
				node2.Research.prerequisites.Remove(item);
			}
		}
		queue = new Queue<Node>(Nodes);
		while (queue.Count > 0)
		{
			Node node = queue.Dequeue();
			if (node.Research.prerequisites.NullOrEmpty() || !node.Research.prerequisites.Any((ResearchProjectDef r) => (int)r.techLevel > (int)node.Research.techLevel))
			{
				continue;
			}
			Log.Warning("\t{0} has a lower techlevel than (one of) it's prerequisites", node.Research.defName);
			node.Research.techLevel = node.Research.prerequisites.Max((ResearchProjectDef r) => r.techLevel);
			foreach (Node child in node.Children)
			{
				queue.Enqueue(child);
			}
		}
	}

	public static void CalcTechLevelsSK()
	{
		Log.Trace("CalcTechLevelsSK:");
		TechLevelLeftBounds.Clear();
		foreach (TechLevel techlevel in RelevantTechLevels)
		{
			IEnumerable<Node> source = NodesSK.Where((Node n) => n.Research.techLevel == techlevel);
			if (source.Count() > 0)
			{
				TechLevelLeftBounds[techlevel] = source.Min((Node n) => n.X);
			}
		}
		foreach (TechLevel relevantTechLevel in RelevantTechLevels)
		{
			if (TechLevelLeftBounds.ContainsKey(relevantTechLevel))
			{
				Log.Trace("\t{0} techlevel: min X={1}", relevantTechLevel, TechLevelLeftBounds[relevantTechLevel]);
			}
			else
			{
				Log.Trace("\t{0} techlevel: no SK nodes", relevantTechLevel);
			}
		}
	}

	public static void OrderAndPlaceOtherNodes()
	{
		Log.Trace("OrderAndPlaceOtherNodes:");
		List<Node> source = NodesOtherList.ToList();
		Dictionary<ResearchTabDef, Dictionary<IntVec2, Node>> dictionary = new Dictionary<ResearchTabDef, Dictionary<IntVec2, Node>>();
		foreach (ResearchTabDef item in TabsOther)
		{
			dictionary[item] = new Dictionary<IntVec2, Node>();
		}
		foreach (TechLevel techLevel in RelevantTechLevels)
		{
			List<Node> techLevelNodes = source.Where((Node n) => n.Research.techLevel == techLevel).ToList();
			if (!TechLevelLeftBounds.ContainsKey(techLevel))
			{
				TechLevel prevTechLevel = techLevel - 1;
				IEnumerable<Node> source2 = NodesOtherList.Where((Node n) => n.Research.techLevel == prevTechLevel);
				TechLevelLeftBounds[techLevel] = ((source2.Count() > 0) ? source2.Max((Node n) => n.X) : 0) + 1;
				Log.Trace("\t{0} techlevel: min X={1}", techLevel, TechLevelLeftBounds[techLevel]);
			}
			while (techLevelNodes.Count > 0)
			{
				List<Node> list = techLevelNodes.Where((Node node) => node.Parents.Where((Node p) => techLevelNodes.Contains(p)).Count() == 0).ToList();
				foreach (Node item2 in list)
				{
					techLevelNodes.Remove(item2);
				}
				foreach (Node node2 in list)
				{
					int newX = ((node2.Parents.Count() <= 0) ? TechLevelLeftBounds[techLevel] : Math.Max(node2.Parents.Max((Node n) => n.X) + 1, TechLevelLeftBounds[techLevel]));
					int num = 0;
					IEnumerable<Node> source3 = node2.Parents.Where((Node p) => p.Research.tab == node2.Research.tab);
					if (source3.Count() > 0)
					{
						num = Math.Max(source3.Min((Node n) => n.Y), num);
					}
					IntVec2 key = new IntVec2(newX, num);
					while (dictionary[node2.Research.tab].ContainsKey(key))
					{
						key.z++;
					}
					node2.X = key.x;
					node2.Y = key.z;
					dictionary[node2.Research.tab][key] = node2;
				}
			}
		}
		int num2 = SizeSK.z + 1;
		foreach (ResearchTabDef item3 in TabsOther)
		{
			RangeInt value = new RangeInt(num2, ((NodesOther[item3].Count > 0) ? NodesOther[item3].Max((Node n) => n.Y) : 0) + 2);
			TabOtherYRanges[item3] = value;
			Log.Trace("\t{0} tab: range by Y ({1} - {2})", item3, value.start, value.end);
			foreach (Node item4 in NodesOther[item3])
			{
				item4.Y += num2 + 1;
				item4.TopLeftReset();
				Log.Trace("\t\t{0} placed to ({1}, {2})", item4.Label, item4.X, item4.Y);
			}
			num2 = value.end + 1;
		}
		UpdateSize();
	}

	public static void CreateEdges()
	{
		Log.Debug("Creating edges.");
		Edge.ClearAll();
		foreach (Node node2 in Nodes)
		{
			if (node2.Research.prerequisites.NullOrEmpty())
			{
				continue;
			}
			foreach (ResearchProjectDef prerequisite in node2.Research.prerequisites)
			{
				Node node = ResearchsToNodesCache[prerequisite];
				if (node != null)
				{
					Edge.Add(node, node2);
				}
			}
		}
	}
}
