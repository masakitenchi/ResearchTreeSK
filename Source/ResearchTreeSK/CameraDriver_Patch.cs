using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(CameraDriver), "CameraDriverOnGUI")]
public static class CameraDriver_Patch
{
	private static readonly FieldInfo? desiredDollyField;

	static CameraDriver_Patch()
	{
		Type typeFromHandle = typeof(CameraDriver);
		desiredDollyField = typeFromHandle.GetField("desiredDolly", BindingFlags.Instance | BindingFlags.NonPublic);
		if (desiredDollyField == null)
		{
			Log.Error("Can not get CameraDriver.desiredDolly field by reflection.", true);
		}
	}

	private static void Postfix(CameraDriver __instance)
	{
		if (MainTabWindow_ResearchTree.Instance != null)
		{
			desiredDollyField?.SetValue(__instance, Vector2.zero);
		}
	}
}
