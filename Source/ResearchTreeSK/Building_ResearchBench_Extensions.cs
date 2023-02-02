using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ResearchTreeSK;

public static class Building_ResearchBench_Extensions
{
	public static bool HasFacility(this Building_ResearchBench building, ThingDef facility)
	{
		CompAffectedByFacilities comp = building.GetComp<CompAffectedByFacilities>();
		if (comp == null)
		{
			return false;
		}
		if (comp.LinkedFacilitiesListForReading.Select((Thing f) => f.def).Contains<ThingDef>(facility))
		{
			return true;
		}
		return false;
	}

	public static List<Thing> AvailableFacilities(this Building_ResearchBench building)
	{
		CompAffectedByFacilities comp = building.GetComp<CompAffectedByFacilities>();
		if (comp == null)
		{
			return new List<Thing>();
		}
		return comp.LinkedFacilitiesListForReading;
	}
}
