using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class ColorsDef : Def
{
	public Color BackgroundNeolithic;

	public Color BackgroundMedieval;

	public Color BackgroundIndustrial;

	public Color BackgroundSpacer;

	public Color BackgroundUltra;

	public Color BackgroundArchotech;

	public Color Highlighted;

	public Color Unavailable;

	public Color Selected;

	public Color Animated;

	public Color Arrow;

	public Color BackgroundColor(TechLevel level)
	{
		if (1 == 0)
		{
		}
		Color result = level switch
		{
			TechLevel.Neolithic => BackgroundNeolithic, 
			TechLevel.Medieval => BackgroundMedieval, 
			TechLevel.Industrial => BackgroundIndustrial, 
			TechLevel.Spacer => BackgroundSpacer, 
			TechLevel.Ultra => BackgroundUltra, 
			TechLevel.Archotech => BackgroundArchotech, 
			_ => throw new ArgumentException(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
