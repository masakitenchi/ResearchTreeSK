using RimWorld;
using UnityEngine;

namespace ResearchTreeSK;

[DefOf]
public static class ModOptions
{
	public static readonly ParamsDef Params;

	public static readonly ColorsDef Colors;

	public static Vector2 NodeSize => Params.NodeSize;

	public static Vector2 NodeMargins => Params.NodeMargins;

	public static Vector2 NodeFullSize => NodeSize + NodeMargins;

	public static float TopBarHeight => NodeSize.y + 12f;

	static ModOptions()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(ModOptions));
	}
}
