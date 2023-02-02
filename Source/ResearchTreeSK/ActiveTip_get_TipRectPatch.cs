using HarmonyLib;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

[HarmonyPatch(typeof(ActiveTip), "get_TipRect")]
public static class ActiveTip_get_TipRectPatch
{
	private static float MaxWidth;

	static ActiveTip_get_TipRectPatch()
	{
		MaxWidth = 450f;
	}

	public static bool Prefix(ActiveTip __instance, ref Rect __result)
	{
		if (__instance is ActiveTipExt)
		{
			string text = __instance.signal.text;
			Vector2 vector = Text.CalcSize(text);
			if (vector.x > MaxWidth)
			{
				vector.x = MaxWidth;
				vector.y = Text.CalcHeight(text, vector.x);
			}
			__result = new Rect(0f, 0f, vector.x, vector.y).ContractedBy(-4f);
			return false;
		}
		return true;
	}
}
