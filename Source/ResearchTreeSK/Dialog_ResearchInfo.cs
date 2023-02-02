using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public sealed class Dialog_ResearchInfo : Dialog_NodeTree
{
	private readonly ResearchProjectDef currentProject;

	private float scrollViewHeight;

	private Vector2 scrollPosition = Vector2.zero;

	public Dialog_ResearchInfo(ResearchProjectDef researchDef, DiaNode nodeRoot, bool delayInteractivity = false)
		: base(nodeRoot, delayInteractivity)
	{
		currentProject = researchDef;
	}

	public override void PreOpen()
	{
		base.PreOpen();
	}

	public override void DoWindowContents(Rect inRect)
	{
		base.DoWindowContents(inRect);
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
		Rect contentRect = new Rect(10f, 0f, inRect.width - 10f, inRect.height - 0f);
		DrawContent(contentRect);
	}

	private void DrawContent(Rect contentRect)
	{
		float num = contentRect.height - 65f;
		Rect position = contentRect;
		GUI.BeginGroup(position);
		Text.Font = GameFont.Medium;
		GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
		Rect rect = new Rect(0f, 0f, position.width, 50f);
		Widgets.LabelCacheHeight(ref rect, "ResearchFinished".Translate(currentProject.LabelCap));
		GenUI.ResetLabelAlign();
		Text.Font = GameFont.Small;
		Rect rect2 = new Rect(15f, rect.yMax, position.width, 0f);
		if (!currentProject.description.StartsWith("description"))
		{
			Widgets.LabelCacheHeight(ref rect2, currentProject.description);
		}
		Rect outRect = new Rect(0f, rect2.yMax + 5f, position.width, num - rect2.yMax - 5f);
		Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		float num2 = 0f;
		Rect rect3 = new Rect(0f, num2, viewRect.width, 500f);
		num2 += DrawTechprintInfo(rect3);
		Rect rect4 = new Rect(0f, num2, viewRect.width, 500f);
		num2 += DrawUnlockableHyperlinks(rect4);
		scrollViewHeight = num2 + 3f;
		Widgets.EndScrollView();
		GUI.EndGroup();
	}

	private float DrawTechprintInfo(Rect rect)
	{
		if (currentProject.TechprintCount == 0)
		{
			return 0f;
		}
		float xMin = rect.xMin;
		float yMin = rect.yMin;
		string text = "ResearchTechprintsFromFactions".Translate();
		float num = Text.CalcHeight(text, rect.width);
		Widgets.Label(new Rect(rect.x, yMin, rect.width, num), text);
		rect.x += 6f;
		if (currentProject.heldByFactionCategoryTags != null)
		{
			foreach (string heldByFactionCategoryTag in currentProject.heldByFactionCategoryTags)
			{
				foreach (Faction item in Find.FactionManager.AllFactionsInViewOrder)
				{
					if (item.def.categoryTag == heldByFactionCategoryTag)
					{
						string name = item.Name;
						Rect position = new Rect(rect.x, yMin + num, rect.width, Mathf.Max(24f, Text.CalcHeight(name, rect.width - 24f - 6f)));
						GUI.BeginGroup(position);
						Rect r = new Rect(0f, 0f, 24f, 24f).ContractedBy(2f);
						FactionUIUtility.DrawFactionIconWithTooltip(r, item);
						Rect rect2 = new Rect(r.xMax + 6f, 0f, position.width - r.width - 6f, position.height);
						Text.Anchor = TextAnchor.MiddleLeft;
						Text.WordWrap = false;
						Widgets.Label(rect2, item.Name);
						Text.Anchor = TextAnchor.UpperLeft;
						Text.WordWrap = true;
						GUI.EndGroup();
						num += position.height;
					}
				}
			}
		}
		rect.xMin = xMin;
		return num;
	}

	private float DrawUnlockableHyperlinks(Rect rect)
	{
		List<Pair<Def, string>> unlockDefsAndDescs = currentProject.GetUnlockDefsAndDescs();
		if (unlockDefsAndDescs.NullOrEmpty())
		{
			return 0f;
		}
		float yMin = rect.yMin;
		Widgets.LabelCacheHeight(ref rect, "Unlocks".Translate() + ":");
		rect.x += 6f;
		rect.yMin += rect.height;
		foreach (Pair<Def, string> item in unlockDefsAndDescs)
		{
			Rect rect2 = new Rect(rect.x, rect.yMin, rect.width, 24f);
			Dialog_InfoCard.Hyperlink hyperlink = new Dialog_InfoCard.Hyperlink(item.First);
			HyperlinkWithIcon(rect2, hyperlink);
			rect.yMin += 24f;
		}
		return rect.yMin - yMin;
	}

	private static void HyperlinkWithIcon(Rect rect, Dialog_InfoCard.Hyperlink hyperlink)
	{
		string label = hyperlink.Label.CapitalizeFirst();
		GUI.BeginGroup(rect);
		Rect rect2 = new Rect(0f, 0f, rect.height, rect.height);
		rect2 = rect2.ContractedBy(2f);
		Texture2D texture2D = hyperlink.def.IconTexture();
		if (texture2D != null)
		{
			GUI.color = hyperlink.def.IconColor();
			GUI.DrawTexture(rect2, texture2D, ScaleMode.ScaleToFit);
		}
		float num = rect2.xMax + 6f;
		Rect rect3 = new Rect(rect2.xMax + 6f, 0f, rect.width - num, rect.height);
		Text.Anchor = TextAnchor.MiddleLeft;
		Text.WordWrap = false;
		Widgets.ButtonText(rect3, label, drawBackground: false, doMouseoverSound: false, Widgets.NormalOptionColor, active: false);
		if (Widgets.ButtonInvisible(rect3))
		{
			hyperlink.ActivateHyperlink();
		}
		Text.Anchor = TextAnchor.UpperLeft;
		Text.WordWrap = true;
		GUI.EndGroup();
	}
}
