using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ResearchTreeSK;

public class Queue : WorldComponent
{
	private static Queue instance;

	private List<ResearchProjectDef> queue = new List<ResearchProjectDef>();

	public static readonly NodeClickInfo NodeClickInfo = new NodeClickInfo();

	public static int NumQueued => instance.queue.Count - 1;

	public Queue(World world)
		: base(world)
	{
		instance = this;
	}

	public static void UpdateNodeRects()
	{
		Vector2 vector = new Vector2(6f, 6f);
		foreach (ResearchProjectDef item in instance.queue)
		{
			Node node = item.TryGetNode();
			if (node != null)
			{
				Rect rect2 = (node.QueueRect = new Rect(vector.x - 6f, vector.y - 6f, ModOptions.NodeSize.x + 12f, ModOptions.NodeSize.y + 12f));
			}
			vector.x += ModOptions.NodeSize.x + 6f;
		}
	}

	public static void Dequeue(ResearchProjectDef research)
	{
		ResearchProjectDef research2 = research;
		instance.queue.Remove(research2);
		List<ResearchProjectDef> list = instance.queue.Where((ResearchProjectDef n) => n.GetIncompleteParentsRecursive().Contains(research2)).ToList();
		foreach (ResearchProjectDef item in list)
		{
			instance.queue.Remove(item);
		}
		if (Find.ResearchManager.currentProj == research2)
		{
			Find.ResearchManager.currentProj = instance.queue.FirstOrDefault();
		}
		UpdateNodeRects();
	}

	public static void DrawLabels(Rect visibleRect)
	{
		int num = 1;
		foreach (ResearchProjectDef item in instance.queue)
		{
			Rect rect = new Rect(item.Node().Rect.min + Node.QueueLabelRect.min, Node.QueueLabelRect.size);
			if (Node.IsVisible(rect, visibleRect))
			{
				Color color = ModOptions.Colors.BackgroundColor(item.techLevel);
				Color background = ((num > 1) ? ModOptions.Colors.Unavailable : color);
				DrawLabel(rect, color, background, num);
			}
			num++;
		}
	}

	public static void DrawLabel(Rect canvas, Color main, Color background, int label)
	{
		GUI.color = main;
		GUI.DrawTexture(canvas, Assets.CircleFill);
		if (background != main)
		{
			GUI.color = background;
			GUI.DrawTexture(canvas.ContractedBy(2f), Assets.CircleFill);
		}
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(canvas, label.ToString());
		Text.Anchor = TextAnchor.UpperLeft;
	}

	public static void Enqueue(ResearchProjectDef research, bool add)
	{
		if (!add)
		{
			instance.queue.Clear();
			Find.ResearchManager.currentProj = null;
		}
		if (!instance.queue.Contains(research))
		{
			instance.queue.Add(research);
		}
		ResearchProjectDef currentProj = instance.queue.FirstOrDefault();
		Find.ResearchManager.currentProj = currentProj;
		UpdateNodeRects();
	}

	public static void InsertAtBeginning(ResearchProjectDef research)
	{
		if (!instance.queue.Contains(research))
		{
			instance.queue.Insert(0, research);
		}
		else
		{
			instance.queue.Remove(research);
			instance.queue.Insert(0, research);
		}
		ResearchProjectDef currentProj = instance.queue.FirstOrDefault();
		Find.ResearchManager.currentProj = currentProj;
		UpdateNodeRects();
	}

	public static void InsertAtBeginningRange(IEnumerable<ResearchProjectDef> researches)
	{
		TutorSystem.Notify_Event("StartResearchProject");
		foreach (ResearchProjectDef item in from research in researches
			orderby research.Node().X descending, research.CostApparent
			select research)
		{
			InsertAtBeginning(item);
		}
	}

	public static void EnqueueRange(IEnumerable<ResearchProjectDef> researches, bool add)
	{
		TutorSystem.Notify_Event("StartResearchProject");
		if (!add)
		{
			instance.queue.Clear();
			Find.ResearchManager.currentProj = null;
		}
		foreach (ResearchProjectDef item in from research in researches
			orderby research.Node().X, research.CostApparent
			select research)
		{
			Enqueue(item, add: true);
		}
		UpdateNodeRects();
	}

	public static bool IsQueued(ResearchProjectDef research)
	{
		return instance.queue.Contains(research);
	}

	public static void OnFinishProject(ResearchProjectDef finished)
	{
		Log.Debug($"OnFinishProject: finished {finished}, ResearchManager.currentProj {Find.ResearchManager.currentProj}, queue.FirstOrDefault() {instance.queue.FirstOrDefault()}");
		if (Find.ResearchManager.currentProj == null)
		{
			return;
		}
		if (IsQueued(finished))
		{
			ResearchProjectDef researchProjectDef = instance.queue.FirstOrDefault();
			if (finished != researchProjectDef)
			{
				Log.Error($"OnFinishProject: current {researchProjectDef} != finished {finished}. Remove anyway!", false);
			}
			instance.queue.Remove(finished);
		}
		else
		{
			Log.Error($"Queue.OnFinishProject: finished {finished} is not queued!", false);
		}
		if (Find.ResearchManager.currentProj == finished)
		{
			Find.ResearchManager.currentProj = instance.queue.FirstOrDefault();
		}
		DoCompletionLetter(finished, Find.ResearchManager.currentProj);
		UpdateNodeRects();
	}

	public static void TryToMove(ResearchProjectDef research)
	{
		Vector2 dropPosition = Event.current.mousePosition;
		IOrderedEnumerable<ResearchProjectDef> source = instance.queue.OrderBy((ResearchProjectDef item) => Mathf.Abs(item.Node().QueueRect.center.x - dropPosition.x));
		ResearchProjectDef item2 = source.First();
		int index = instance.queue.IndexOf(item2);
		instance.queue.Remove(research);
		instance.queue.Insert(index, research);
		SortRequiredRecursive(research);
		List<ResearchProjectDef> list = research.Descendants();
		if (!list.NullOrEmpty())
		{
			List<ResearchProjectDef> list2 = list.Where((ResearchProjectDef def) => !def.IsFinished && IsQueued(def)).ToList();
			foreach (ResearchProjectDef item3 in list2)
			{
				SortRequiredRecursive(item3);
			}
		}
		Find.ResearchManager.currentProj = instance.queue.First();
		UpdateNodeRects();
	}

	private static void SortRequiredRecursive(ResearchProjectDef research)
	{
		int num = instance.queue.IndexOf(research);
		List<ResearchProjectDef> list = research.GetIncompleteParentsRecursive().ToList();
		foreach (ResearchProjectDef item in list)
		{
			if (IsQueued(item))
			{
				int num2 = instance.queue.IndexOf(item);
				if (num2 > num)
				{
					instance.queue.Remove(item);
					instance.queue.Insert(num, item);
					SortRequiredRecursive(item);
				}
			}
		}
	}

	private static void DoCompletionLetter(ResearchProjectDef current, ResearchProjectDef next)
	{
		Find.LetterStack.ReceiveLetter(new ResearchLetter(current, next));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref queue, "Queue", LookMode.Def);
		UpdateNodeRects();
	}

	public static void DrawQueue(Rect canvas, Vector2 delta)
	{
		if (!instance.queue.Any())
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Assets.TechLevelColor;
			Widgets.Label(canvas, "ResearchTreeSK.NothingQueued".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			return;
		}
		GUI.BeginGroup(canvas);
		NodeClickInfo.HandleDown(instance.queue.Select((ResearchProjectDef r) => r.Node()), (Node n) => n.QueueRect);
		Rect visibleRect = new Rect(Vector2.zero, canvas.size);
		foreach (ResearchProjectDef item in instance.queue.ToList())
		{
			if (item != NodeClickInfo.MouseDownedNode?.Research)
			{
				Node node = item.Node();
				MouseoverSounds.DoRegion(node.QueueRect);
				node.Draw(visibleRect, isDragged: false, drawInQueue: true);
			}
		}
		if (NodeClickInfo.MouseDownedNode != null)
		{
			MouseoverSounds.DoRegion(NodeClickInfo.MouseDownedNode!.QueueRect);
			NodeClickInfo.MouseDownedNode!.Draw(visibleRect, isDragged: true, drawInQueue: true);
		}
		Widgets.DraggableResult draggableResult = ButtonInvisibleDraggable.Handle(NodeClickInfo);
		if (draggableResult != 0)
		{
			Node mouseDownedNode = NodeClickInfo.MouseDownedNode;
			switch (draggableResult)
			{
			case Widgets.DraggableResult.Pressed:
				if (Event.current.alt && Event.current.button == 0)
				{
					GUI.BeginGroup(mouseDownedNode.QueueRect);
					foreach (Node.UnlockedDef unlock in mouseDownedNode.Unlocks)
					{
						if (unlock.Def != null && unlock.Rect.Contains(Event.current.mousePosition))
						{
							Find.WindowStack.Add(new Dialog_InfoCard(unlock.Def));
						}
					}
					GUI.EndGroup();
				}
				else
				{
					Dequeue(mouseDownedNode.Research);
				}
				break;
			case Widgets.DraggableResult.Dragged:
			{
				mouseDownedNode.QueueRect = new Rect(new Vector2(mouseDownedNode.QueueRect.x + delta.x, 0f), mouseDownedNode.QueueRect.size);
				Rect queueRect = mouseDownedNode.QueueRect;
				TryToMove(mouseDownedNode.Research);
				mouseDownedNode.QueueRect = queueRect;
				break;
			}
			case Widgets.DraggableResult.DraggedThenPressed:
				TryToMove(mouseDownedNode.Research);
				break;
			}
		}
		NodeClickInfo.HandleUp();
		GUI.EndGroup();
	}
}
