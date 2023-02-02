using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public static class Def_Extensions
{
	private static readonly Dictionary<Def, Texture2D?> _cachedDefIcons = new Dictionary<Def, Texture2D>();

	private static readonly Dictionary<Def, Color> _cachedIconColors = new Dictionary<Def, Color>();

	private static readonly Type? AndroidUpgradeDefType = (ModsConfig.IsActive("ChJees.Androids") ? AccessTools.TypeByName("Androids.AndroidUpgradeDef") : null);

	public static Color IconColor(this RecipeDef def)
	{
		if (!def.products.NullOrEmpty())
		{
			return def.products.First().thingDef.IconColor();
		}
		return Color.white;
	}

	public static Color IconColor(this ThingDef def)
	{
		if (def.entityDefToBuild != null)
		{
			return def.entityDefToBuild.IconColor();
		}
		if (def.graphic != null)
		{
			return def.graphic.color;
		}
		if (def.MadeFromStuff)
		{
			ThingDef thingDef = GenStuff.DefaultStuffFor(def);
			return thingDef.stuffProps.color;
		}
		return Color.white;
	}

	public static Color IconColor(this BuildableDef def)
	{
		if (def.graphic != null)
		{
			return def.graphic.color;
		}
		return Color.white;
	}

	public static Color IconColor(this Def def)
	{
		if (_cachedIconColors.ContainsKey(def))
		{
			return _cachedIconColors[def];
		}
		Color color = ((!(def is RecipeDef def2)) ? ((!(def is ThingDef def3)) ? ((!(def is BuildableDef def4)) ? Color.white : def4.IconColor()) : def3.IconColor()) : def2.IconColor());
		_cachedIconColors.Add(def, color);
		return color;
	}

	private static Texture2D IconTexture(this RecipeDef def)
	{
		if (!def.products.NullOrEmpty())
		{
			return def.products.First().thingDef.IconTexture();
		}
		return Assets.NoRecipeProducts;
	}

	private static Texture2D IconTexture(this BuildableDef def)
	{
		return def.uiIcon;
	}

	private static Texture2D IconTexture(this ThingDef def)
	{
		if (def.entityDefToBuild != null)
		{
			return def.entityDefToBuild.IconTexture();
		}
		return ((BuildableDef)def).IconTexture();
	}

	private static Texture2D AndroidUpgradeDef_IconTexture(this Def def)
	{
		string itemPath = (string)AccessTools.Field(AndroidUpgradeDefType, "iconTexturePath").GetValue(def);
		return ContentFinder<Texture2D>.Get(itemPath);
	}

	public static Texture2D? IconTexture(this Def def)
	{
		if (_cachedDefIcons.ContainsKey(def))
		{
			return _cachedDefIcons[def];
		}
		Texture2D texture2D;
		if (def is RecipeDef def2)
		{
			texture2D = def2.IconTexture();
		}
		else if (def is ThingDef def3)
		{
			texture2D = def3.IconTexture();
		}
		else if (def is BuildableDef def4)
		{
			texture2D = def4.IconTexture();
		}
		else if (AndroidUpgradeDefType != null && def.GetType() == AndroidUpgradeDefType)
		{
			texture2D = def.AndroidUpgradeDef_IconTexture();
		}
		else
		{
			Log.Warning($"IconTexture: Unknown def {def}");
			texture2D = null;
		}
		_cachedDefIcons.Add(def, texture2D);
		return texture2D;
	}
}
