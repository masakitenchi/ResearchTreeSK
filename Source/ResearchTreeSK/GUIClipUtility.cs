using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

[StaticConstructorOnStartup]
public static class GUIClipUtility
{
	private static readonly Func<Rect> GetTopRectDelegate;

	private static readonly Action<Rect, Vector2, Vector2, bool> PushAction;

	private static readonly Action PopAction;

	private static readonly List<Rect> rectStack;

	static GUIClipUtility()
	{
		rectStack = new List<Rect>();
		Assembly assembly = Assembly.GetAssembly(typeof(GUI));
		Type type = assembly.GetType("UnityEngine.GUIClip", throwOnError: true);
		MethodInfo method = type.GetMethod("GetTopRect", BindingFlags.Static | BindingFlags.NonPublic);
		GetTopRectDelegate = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), method);
		MethodInfo method2 = type.GetMethod("Push", BindingFlags.Static | BindingFlags.NonPublic);
		PushAction = (Action<Rect, Vector2, Vector2, bool>)Delegate.CreateDelegate(typeof(Action<Rect, Vector2, Vector2, bool>), method2);
		MethodInfo method3 = type.GetMethod("Pop", BindingFlags.Static | BindingFlags.NonPublic);
		PopAction = (Action)Delegate.CreateDelegate(typeof(Action), method3);
	}

	public static void BeginNoClip()
	{
		Rect rect = GetTopRectDelegate();
		while (rect != new Rect(-10000f, -10000f, 40000f, 40000f))
		{
			rectStack.Add(rect);
			GUI.EndClip();
			rect = GetTopRectDelegate();
		}
	}

	public static void EndNoClip()
	{
		rectStack.Reverse();
		foreach (Rect item in rectStack)
		{
			GUI.BeginClip(item);
		}
		rectStack.Clear();
	}

	public static void BeginZoom(float ZoomLevel)
	{
		GUI.matrix = Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(Prefs.UIScale / ZoomLevel, Prefs.UIScale / ZoomLevel, 1f));
		UI.screenWidth = Mathf.RoundToInt((float)Screen.width / Prefs.UIScale * ZoomLevel);
		UI.screenHeight = Mathf.RoundToInt((float)Screen.height / Prefs.UIScale * ZoomLevel);
	}

	public static void EndZoom()
	{
		UI.screenWidth = Mathf.RoundToInt((float)Screen.width / Prefs.UIScale);
		UI.screenHeight = Mathf.RoundToInt((float)Screen.height / Prefs.UIScale);
		GUI.matrix = Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(Prefs.UIScale, Prefs.UIScale, 1f));
	}

	public static void BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect)
	{
		PushAction(position, new Vector2(Mathf.Round(0f - scrollPosition.x - viewRect.x), Mathf.Round(0f - scrollPosition.y - viewRect.y)), Vector2.zero, arg4: false);
	}

	public static void EndScrollView()
	{
		PopAction();
	}
}
