using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class MainTabWindow_ResearchTree : MainTabWindow
{
	private Vector2 mousePosition = Vector2.zero;

	private string _query = "";

	public static MainTabWindow_ResearchTree? Instance { get; private set; }

	public MainTabWindow_ResearchTree()
	{
		closeOnClickedOutside = false;
		doWindowBackground = false;
	}

	public override void PreOpen()
	{
		base.PreOpen();
		Tree.UpdateRects();
		windowRect.x = 0f;
		windowRect.y = 0f;
		windowRect.width = UI.screenWidth;
		windowRect.height = UI.screenHeight - 35;
		if (!Tree.Initialized)
		{
			Tree.Initialize();
		}
		ResearchProjectDef_Extensions.UpdateOnlineCaches();
		foreach (Node node in Tree.Nodes)
		{
			node.UpdateCaches();
		}
		closeOnClickedOutside = false;
		Instance = this;
	}

	public override void PreClose()
	{
		base.PreClose();
		Instance = null;
	}

	public override void DoWindowContents(Rect canvas)
	{
		GUIClipUtility.BeginNoClip();
		GUI.DrawTexture(windowRect, Assets.BackgroundRT);
		GUIClipUtility.EndNoClip();
		if (Tree.Initialized)
		{
			Vector2 delta = HandleDragging();
			Rect rect = new Rect(canvas.xMin, canvas.yMin, canvas.width, ModOptions.TopBarHeight);
			if (!Tree.EditMode)
			{
				DrawTopBar(rect, delta);
			}
			Tree.DrawEditModeButtons(rect);
			Tree.DoContents(ref windowRect, delta);
			EventType type = Event.current.type;
			EventType eventType = type;
			if (eventType == EventType.MouseDrag || eventType == EventType.ScrollWheel)
			{
				Event.current.Use();
			}
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	public Vector2 HandleDragging()
	{
		Vector2 result = Vector2.zero;
		if (Event.current.type == EventType.MouseDown)
		{
			mousePosition = Event.current.mousePosition;
		}
		if (Event.current.type == EventType.MouseUp)
		{
			mousePosition = Vector2.zero;
		}
		if (Event.current.type == EventType.MouseDrag)
		{
			result = Event.current.mousePosition - mousePosition;
			mousePosition = Event.current.mousePosition;
		}
		return result;
	}

	private void DrawTopBar(Rect canvas, Vector2 delta)
	{
		Rect rect = canvas;
		Rect rect2 = canvas;
		rect.width = 200f;
		rect2.xMin += 206f;
		DrawSearchBar(rect.ContractedBy(6f));
		Queue.DrawQueue(rect2.ContractedBy(6f), delta);
	}

	private void DrawSearchBar(Rect canvas)
	{
		Rect position = new Rect(canvas.xMax - 6f - 16f, 0f, 16f, 16f).CenteredOnYIn(canvas);
		Rect rect = new Rect(canvas.xMin, 0f, canvas.width, 30f).CenteredOnYIn(canvas);
		GUI.DrawTexture(position, Assets.Search);
		string query = Widgets.TextField(rect, _query);
		if (!(query != _query))
		{
			return;
		}
		_query = query;
		Find.WindowStack.FloatMenu?.Close(doCloseSound: false);
		if (query.Length <= 2)
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (var result2 in from n in Tree.Nodes
			select new
			{
				node = n,
				match = n.Matches(query)
			} into result
			where result.match > 0
			orderby result.match
			select result)
		{
			list.Add(new FloatMenuOption_NoClose(result2.node.Label, delegate
			{
				Tree.CenterOn(result2.node);
			}));
		}
		if (!list.Any())
		{
			list.Add(new FloatMenuOption("ResearchTreeSK.NoResearchFound".Translate(), null));
		}
		Find.WindowStack.Add(new FloatMenu_Fixed(list, UI.GUIToScreenPoint(new Vector2(rect.xMin, rect.yMax))));
	}
}
