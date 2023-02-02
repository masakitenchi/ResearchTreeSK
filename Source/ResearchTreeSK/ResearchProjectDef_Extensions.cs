using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using SK;
using Verse;

namespace ResearchTreeSK;

[StaticConstructorOnStartup]
public static class ResearchProjectDef_Extensions
{
	private static readonly Dictionary<ResearchProjectDef, List<Pair<Def, string>>> _unlocksCache;

	private static List<Building_ResearchBench> researchBenchesCache;

	private static readonly Dictionary<Building_ResearchBench, List<ThingDef>> availableFacilityDefsCache;

	private static readonly Dictionary<Building_ResearchBench, List<ThingDef>> poweredFacilityDefsCache;

	private static readonly Dictionary<ResearchProjectDef, List<ThingDef>> missingFacilitiesCache;

	private static readonly Dictionary<ResearchProjectDef, List<ThingDef>> notPoweredFacilitiesCache;

	private static readonly IEnumerable<Def> AndroidUpgradeDefs;

	private static readonly FieldInfo? AndroidUpgradeDef_requiredResearch;

	static ResearchProjectDef_Extensions()
	{
		_unlocksCache = new Dictionary<ResearchProjectDef, List<Pair<Def, string>>>();
		researchBenchesCache = new List<Building_ResearchBench>();
		availableFacilityDefsCache = new Dictionary<Building_ResearchBench, List<ThingDef>>();
		poweredFacilityDefsCache = new Dictionary<Building_ResearchBench, List<ThingDef>>();
		missingFacilitiesCache = new Dictionary<ResearchProjectDef, List<ThingDef>>();
		notPoweredFacilitiesCache = new Dictionary<ResearchProjectDef, List<ThingDef>>();
		if (ModsConfig.IsActive("ChJees.Androids"))
		{
			Type type = AccessTools.TypeByName("Androids.AndroidUpgradeDef");
			Type typeFromHandle = typeof(DefDatabase<>);
			Type[] typeArguments = new Type[1] { type };
			Type type2 = typeFromHandle.MakeGenericType(typeArguments);
			MethodInfo methodInfo = AccessTools.PropertyGetter(type2, "AllDefsListForReading");
			AndroidUpgradeDefs = (IEnumerable<Def>)methodInfo.Invoke(null, null);
			AndroidUpgradeDef_requiredResearch = AccessTools.Field(type, "requiredResearch");
		}
		else
		{
			AndroidUpgradeDefs = new List<Def>();
			AndroidUpgradeDef_requiredResearch = null;
		}
		CalcUnlocksCache();
	}

	public static void UpdateOnlineCaches()
	{
		researchBenchesCache = Find.Maps.SelectMany((Map map) => map.listerBuildings.allBuildingsColonist).OfType<Building_ResearchBench>().ToList();
		availableFacilityDefsCache.Clear();
		poweredFacilityDefsCache.Clear();
		foreach (Building_ResearchBench item in researchBenchesCache)
		{
			CompAffectedByFacilities comp = item.TryGetComp<CompAffectedByFacilities>();
			if (comp != null)
			{
				availableFacilityDefsCache[item] = comp.LinkedFacilitiesListForReading.Select((Thing f) => f.def).ToList();
				poweredFacilityDefsCache[item] = (from f in comp.LinkedFacilitiesListForReading
					where comp.IsFacilityActive(f)
					select f.def).ToList();
			}
			else
			{
				availableFacilityDefsCache[item] = new List<ThingDef>();
				poweredFacilityDefsCache[item] = new List<ThingDef>();
			}
		}
		missingFacilitiesCache.Clear();
		notPoweredFacilitiesCache.Clear();
	}

	public static List<ResearchProjectDef> Descendants(this ResearchProjectDef research)
	{
		ResearchProjectDef research2 = research;
		HashSet<ResearchProjectDef> hashSet = new HashSet<ResearchProjectDef>();
		Queue<ResearchProjectDef> queue = new Queue<ResearchProjectDef>(DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where((ResearchProjectDef res) => res.prerequisites?.Contains(research2) ?? false));
		while (queue.Count > 0)
		{
			ResearchProjectDef current = queue.Dequeue();
			hashSet.Add(current);
			foreach (ResearchProjectDef item in DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where((ResearchProjectDef res) => res.prerequisites?.Contains(current) ?? false))
			{
				queue.Enqueue(item);
			}
		}
		return hashSet.ToList();
	}

	public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research)
	{
		ResearchProjectDef research2 = research;
		List<ResearchProjectDef> list = new List<ResearchProjectDef>();
		if (research2.prerequisites.NullOrEmpty())
		{
			return list;
		}
		Stack<ResearchProjectDef> stack = new Stack<ResearchProjectDef>(research2.prerequisites.Where((ResearchProjectDef parent) => parent != research2));
		while (stack.Count > 0)
		{
			ResearchProjectDef researchProjectDef = stack.Pop();
			list.Add(researchProjectDef);
			if (researchProjectDef.prerequisites.NullOrEmpty())
			{
				continue;
			}
			foreach (ResearchProjectDef prerequisite in researchProjectDef.prerequisites)
			{
				if (prerequisite != researchProjectDef && !list.Contains(prerequisite))
				{
					stack.Push(prerequisite);
				}
			}
		}
		return list.Distinct().ToList();
	}

	public static List<ResearchProjectDef> GetIncompleteParentsRecursive(this ResearchProjectDef research)
	{
		if (research.prerequisites == null)
		{
			return new List<ResearchProjectDef>();
		}
		IEnumerable<ResearchProjectDef> enumerable = research.prerequisites.Where((ResearchProjectDef rpd) => !rpd.IsFinished);
		List<ResearchProjectDef> list = enumerable.ToList();
		foreach (ResearchProjectDef item in enumerable)
		{
			list.AddRange(item.GetIncompleteParentsRecursive());
		}
		return list.Distinct().ToList();
	}

	private static void AppendToUnlocksCache<T>(IEnumerable<T> list, Func<T, List<ResearchProjectDef>?> getResearchPrerequisites, string translateStr) where T : Def
	{
		foreach (T item in list)
		{
			List<ResearchProjectDef> list2 = getResearchPrerequisites(item);
			if (list2.NullOrEmpty())
			{
				continue;
			}
			list2 = list2.Distinct().ToList();
			if (list2.Count > 1)
			{
				Log.Warning("Multiple researchPrerequisites {0} for Def {1}", string.Join(", ", list2), item);
			}
			foreach (ResearchProjectDef item2 in list2)
			{
				_unlocksCache[item2].Add(new Pair<Def, string>(item, translateStr.Translate(item.LabelCap)));
			}
		}
	}

	private static void CalcUnlocksCache()
	{
		Log.Message("CalcUnlocksCache start");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		foreach (ResearchProjectDef item in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
		{
			_unlocksCache[item] = new List<Pair<Def, string>>();
		}
		AppendToUnlocksCache(DefDatabase<ThingDef>.AllDefsListForReading, (ThingDef def) => def.researchPrerequisites, "ResearchTreeSK.AllowsBuildingX");
		AppendToUnlocksCache(DefDatabase<TerrainDef>.AllDefsListForReading, (TerrainDef def) => def.researchPrerequisites, "ResearchTreeSK.AllowsBuildingX");
		AppendToUnlocksCache(DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef def) => def.plant != null), (ThingDef def) => def.plant.sowResearchPrerequisites, "ResearchTreeSK.AllowsPlantingX");
		AppendToUnlocksCache(DefDatabase<RecipeDef>.AllDefsListForReading, (RecipeDef def) => def.researchPrerequisites, "ResearchTreeSK.AllowsCraftingX");
		AppendToUnlocksCache(DefDatabase<RecipeDef>.AllDefsListForReading, (RecipeDef def) => (def.researchPrerequisite != null) ? new List<ResearchProjectDef> { def.researchPrerequisite } : null, "ResearchTreeSK.AllowsCraftingX");
		IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef td) => !td.researchPrerequisites.NullOrEmpty() && !td.AllRecipes.NullOrEmpty());
		foreach (ThingDef item2 in enumerable)
		{
			foreach (RecipeDef allRecipe in item2.AllRecipes)
			{
				if (allRecipe.researchPrerequisite != null || !allRecipe.researchPrerequisites.NullOrEmpty())
				{
					continue;
				}
				List<ResearchProjectDef> list = item2.researchPrerequisites.Distinct().ToList();
				if (list.Count > 1)
				{
					Log.Warning("Multiple researchPrerequisites {0} for Def {1}, recipe {2}", string.Join(", ", list), item2, allRecipe);
				}
				foreach (ResearchProjectDef item3 in list)
				{
					if (!_unlocksCache.ContainsKey(item3))
					{
						_unlocksCache[item3] = new List<Pair<Def, string>>();
					}
					_unlocksCache[item3].Add(new Pair<Def, string>(allRecipe, "ResearchTreeSK.AllowsCraftingX".Translate(allRecipe.LabelCap)));
				}
			}
		}
		if (AndroidUpgradeDef_requiredResearch != null)
		{
			foreach (Def androidUpgradeDef in AndroidUpgradeDefs)
			{
				ResearchProjectDef key = (ResearchProjectDef)AndroidUpgradeDef_requiredResearch!.GetValue(androidUpgradeDef);
				_unlocksCache[key].Add(new Pair<Def, string>(androidUpgradeDef, "ResearchTreeSK.AllowsAndroidUpgradeX".Translate(androidUpgradeDef.LabelCap)));
			}
		}
		foreach (KeyValuePair<ResearchProjectDef, List<Pair<Def, string>>> item4 in _unlocksCache)
		{
			List<Pair<Def, string>> list2 = item4.Value.Distinct().ToList();
			if (item4.Value.Count != list2.Count)
			{
				Log.Warning(string.Join(", ", "The research {0} have duplicate unlocked defs {1}.\nDistinct reduce count from {2} to {3}."), item4.Key, string.Join(", ", item4.Value.Select((Pair<Def, string> u) => u.First)), item4.Value.Count, list2.Count);
			}
			item4.Value.Clear();
			item4.Value.AddRange(list2);
		}
		foreach (KeyValuePair<ResearchProjectDef, List<Pair<Def, string>>> item5 in _unlocksCache)
		{
			List<ResearchProjectDef> list3 = item5.Key.Descendants();
			if (!list3.Any())
			{
				continue;
			}
			List<Def> descendantUnlocks = item5.Key.Descendants().SelectMany((ResearchProjectDef research) => _unlocksCache[research].Select<Pair<Def, string>, Def>((Pair<Def, string> u) => u.First)).Distinct()
				.ToList();
			List<Pair<Def, string>> list4 = item5.Value.Where((Pair<Def, string> u) => !descendantUnlocks.Contains(u.First)).ToList();
			if (item5.Value.Count != list4.Count)
			{
				Log.Warning("The research {0} have unlocked defs {1}.\nBut it's descendants have duplicate unlocked defs {2}.\nCount was been reduced from {3} to {4}.", item5.Key, (item5.Value.Count > 0) ? string.Join(", ", item5.Value.Select((Pair<Def, string> u) => u.First)) : "", string.Join(", ", descendantUnlocks.Intersect(item5.Value.Select((Pair<Def, string> u) => u.First))), item5.Value.Count, list4.Count);
			}
			item5.Value.Clear();
			item5.Value.AddRange(list4);
		}
		stopwatch.Stop();
		long num = stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000);
		Log.Message("CalcUnlocksCache end with {0} milliseconds", num);
	}

	public static List<Pair<Def, string>> GetUnlockDefsAndDescs(this ResearchProjectDef research)
	{
		return _unlocksCache[research];
	}

	public static bool BuildingPresent(this ResearchProjectDef research)
	{
		if (research.requiredResearchBuilding == null)
		{
			return true;
		}
		foreach (Building_ResearchBench item in researchBenchesCache)
		{
			if (research.CanBeResearchedAt(item, ignoreResearchBenchPowerStatus: false))
			{
				return true;
			}
		}
		return false;
	}

	public static (List<ThingDef>, List<ThingDef>) MissingAndNotPoweredFacilities(this ResearchProjectDef research)
	{
		if (missingFacilitiesCache.ContainsKey(research))
		{
			return (missingFacilitiesCache[research], notPoweredFacilitiesCache[research]);
		}
		List<ThingDef> list = new List<ThingDef>();
		List<ThingDef> list2 = new List<ThingDef>();
		IEnumerable<ResearchProjectDef> enumerable = from rpd in research.Ancestors()
			where !rpd.IsFinished
			select rpd;
		foreach (ResearchProjectDef item in enumerable)
		{
			var (collection, collection2) = item.MissingAndNotPoweredFacilities();
			list.AddRange(collection);
			list2.AddRange(collection2);
		}
		if (research.requiredResearchBuilding != null)
		{
			List<ThingDef> requiredResearchBuildings;
			if (research.HasModExtension<AdvancedResearchExtension>())
			{
				requiredResearchBuildings = research.GetModExtension<AdvancedResearchExtension>().requiredResearchBuildings;
			}
			else
			{
				requiredResearchBuildings = new List<ThingDef>();
				requiredResearchBuildings.Add(research.requiredResearchBuilding);
			}
			List<Building_ResearchBench> list3 = researchBenchesCache.Where((Building_ResearchBench b) => requiredResearchBuildings.Contains(b.def)).ToList();
			if (list3.Count == 0)
			{
				list.Add(research.requiredResearchBuilding);
			}
			else
			{
				List<Building_ResearchBench> list4 = list3.Where((Building_ResearchBench b) => b.GetComp<CompPowerTrader>()?.PowerOn ?? true).ToList();
				if (list4.Count > 0)
				{
					list3 = list4;
				}
				else
				{
					list2.Add(research.requiredResearchBuilding);
				}
			}
			if (!research.requiredResearchFacilities.NullOrEmpty())
			{
				if (list3.Count == 0)
				{
					list.AddRange(research.requiredResearchFacilities);
				}
				else
				{
					Building_ResearchBench key = null;
					List<ThingDef> list5 = null;
					foreach (Building_ResearchBench item2 in list3)
					{
						List<ThingDef> list6 = research.requiredResearchFacilities.Except<ThingDef>(availableFacilityDefsCache[item2]).ToList();
						if (list5 == null || list6.Count < list5.Count)
						{
							key = item2;
							list5 = list6;
						}
					}
					list.AddRange(list5);
					IEnumerable<ThingDef> collection3 = research.requiredResearchFacilities.Except(list).Except<ThingDef>(poweredFacilityDefsCache[key]);
					list2.AddRange(collection3);
				}
			}
		}
		missingFacilitiesCache[research] = list.Distinct().ToList();
		notPoweredFacilitiesCache[research] = list2.Distinct().ToList();
		return (missingFacilitiesCache[research], notPoweredFacilitiesCache[research]);
	}

	public static bool TechprintAvailable(this ResearchProjectDef research)
	{
		return research.TechprintRequirementMet;
	}

	public static Node Node(this ResearchProjectDef research)
	{
		return Tree.ResearchsToNodesCache[research];
	}

	public static Node? TryGetNode(this ResearchProjectDef research)
	{
		if (Tree.ResearchsToNodesCache.ContainsKey(research))
		{
			return Tree.ResearchsToNodesCache[research];
		}
		return null;
	}
}
