using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

[StaticConstructorOnStartup]
public static class Assets
{
	[StaticConstructorOnStartup]
	public static class Lines
	{
		public static Texture2D Circle = ContentFinder<Texture2D>.Get("Lines/circle");

		public static Texture2D End = ContentFinder<Texture2D>.Get("Lines/end");

		public static Texture2D EW = ContentFinder<Texture2D>.Get("Lines/ew");

		public static Texture2D NS = ContentFinder<Texture2D>.Get("Lines/ns");
	}

	public static Dictionary<TechLevel, Texture2D> NodeTextures = new Dictionary<TechLevel, Texture2D>
	{
		{
			TechLevel.Neolithic,
			ContentFinder<Texture2D>.Get("Tech/Neolithic")
		},
		{
			TechLevel.Medieval,
			ContentFinder<Texture2D>.Get("Tech/Medieval")
		},
		{
			TechLevel.Industrial,
			ContentFinder<Texture2D>.Get("Tech/Industrial")
		},
		{
			TechLevel.Spacer,
			ContentFinder<Texture2D>.Get("Tech/Spacer")
		},
		{
			TechLevel.Ultra,
			ContentFinder<Texture2D>.Get("Tech/Ultra")
		},
		{
			TechLevel.Archotech,
			ContentFinder<Texture2D>.Get("Tech/Archotech")
		}
	};

	public static Dictionary<TechLevel, Texture2D> ProgressTextures = new Dictionary<TechLevel, Texture2D>
	{
		{
			TechLevel.Neolithic,
			ContentFinder<Texture2D>.Get("Tech/ProgressNeolithic")
		},
		{
			TechLevel.Medieval,
			ContentFinder<Texture2D>.Get("Tech/ProgressMedieval")
		},
		{
			TechLevel.Industrial,
			ContentFinder<Texture2D>.Get("Tech/ProgressIndustrial")
		},
		{
			TechLevel.Spacer,
			ContentFinder<Texture2D>.Get("Tech/ProgressSpacer")
		},
		{
			TechLevel.Ultra,
			ContentFinder<Texture2D>.Get("Tech/ProgressUltra")
		},
		{
			TechLevel.Archotech,
			ContentFinder<Texture2D>.Get("Tech/ProgressArchotech")
		}
	};

	public static Texture2D ResearchIcon = ContentFinder<Texture2D>.Get("Icons/Research");

	public static Texture2D MoreIcon = ContentFinder<Texture2D>.Get("Icons/more");

	public static Texture2D Lock = ContentFinder<Texture2D>.Get("Icons/padlock");

	internal static readonly Texture2D CircleFill = ContentFinder<Texture2D>.Get("Icons/circle-fill");

	public static Texture2D NoRecipeProducts = ContentFinder<Texture2D>.Get("Icons/no-recipe-products");

	public static readonly Texture2D CopyIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Copy");

	public static readonly Texture2D HelpIcon = (Texture2D)AccessTools.Field(AccessTools.TypeByName("Verse.TexButton"), "Info").GetValue(null);

	public static Color TechLevelColor = new Color(1f, 1f, 1f, 0.2f);

	public static Texture2D BackgroundRT = ContentFinder<Texture2D>.Get("BackgroundRT/backgroundRT");

	public static Texture2D Search = ContentFinder<Texture2D>.Get("Icons/magnifying-glass");
}
