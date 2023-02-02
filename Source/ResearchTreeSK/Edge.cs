using System.Collections.Generic;
using UnityEngine;

namespace ResearchTreeSK;

public static class Edge
{
	public abstract class EdgePart
	{
		public readonly List<Node> NodesTo = new List<Node>();

		public readonly Vector2 Pos;

		public readonly Rect Rect;

		public readonly Texture2D Texture;

		public bool Highlited = false;

		public EdgePart(Vector2 pos, Rect rect, Texture2D texture)
		{
			Pos = pos;
			Rect = rect;
			Texture = texture;
		}
	}

	private class VerticalLine : EdgePart
	{
		public VerticalLine(float X, float Y)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x - ModOptions.Params.LineThickness / 2f, Y * ModOptions.NodeFullSize.y + ModOptions.Params.CurveRadius, ModOptions.Params.LineThickness, ModOptions.NodeFullSize.y - ModOptions.Params.CurveRadius * 2f), Assets.Lines.NS)
		{
		}
	}

	private class HorizontalLine : EdgePart
	{
		public HorizontalLine(float X, float Y)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x + ModOptions.Params.CurveRadius, Y * ModOptions.NodeFullSize.y - ModOptions.Params.LineThickness / 2f, ModOptions.NodeFullSize.x - ModOptions.Params.CurveRadius * 2f, ModOptions.Params.LineThickness), Assets.Lines.EW)
		{
		}
	}

	private enum CurveType
	{
		LeftTop,
		LeftBottom,
		RightTop,
		RightBottom
	}

	private class Curve : EdgePart
	{
		public readonly CurveType Type;

		public readonly Rect TexRect;

		public Curve(float X, float Y, CurveType type)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x - ModOptions.Params.CurveRadius, Y * ModOptions.NodeFullSize.y - ModOptions.Params.CurveRadius, ModOptions.Params.CurveRadius * 2f, ModOptions.Params.CurveRadius * 2f), Assets.Lines.Circle)
		{
			Type = type;
			switch (type)
			{
			case CurveType.LeftTop:
				TexRect = new Rect(0f, 0.5f, 0.5f, 0.5f);
				break;
			case CurveType.LeftBottom:
				TexRect = new Rect(0f, 0f, 0.5f, 0.5f);
				break;
			case CurveType.RightTop:
				TexRect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
				break;
			case CurveType.RightBottom:
				TexRect = new Rect(0.5f, 0f, 0.5f, 0.5f);
				break;
			}
		}
	}

	private class VerticalLineOverCurve : EdgePart
	{
		public VerticalLineOverCurve(float X, float Y)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x - ModOptions.Params.LineThickness / 2f, Y * ModOptions.NodeFullSize.y - ModOptions.Params.CurveRadius, ModOptions.Params.LineThickness, ModOptions.Params.CurveRadius * 2f), Assets.Lines.NS)
		{
		}
	}

	private class HorizontalLineOverCurve : EdgePart
	{
		public HorizontalLineOverCurve(float X, float Y)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x - ModOptions.Params.CurveRadius, Y * ModOptions.NodeFullSize.y - ModOptions.Params.LineThickness / 2f, ModOptions.Params.CurveRadius * 2f, ModOptions.Params.LineThickness), Assets.Lines.EW)
		{
		}
	}

	private class StartLine : EdgePart
	{
		public StartLine(float X, float Y)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x - ModOptions.NodeMargins.x / 2f, Y * ModOptions.NodeFullSize.y - ModOptions.Params.LineThickness / 2f, ModOptions.NodeMargins.x / 2f - ModOptions.Params.CurveRadius, ModOptions.Params.LineThickness), Assets.Lines.EW)
		{
		}
	}

	private class EndArrow : EdgePart
	{
		public EndArrow(float X, float Y)
			: base(new Vector2(X, Y), new Rect(X * ModOptions.NodeFullSize.x + ModOptions.Params.CurveRadius, Y * ModOptions.NodeFullSize.y - ModOptions.Params.ArrowThickness / 2f, ModOptions.NodeMargins.x / 2f - ModOptions.Params.CurveRadius, ModOptions.Params.ArrowThickness), Assets.Lines.End)
		{
		}
	}

	private static readonly List<EdgePart> All = new List<EdgePart>();

	private static readonly Dictionary<Node, List<EdgePart>> NodeToPartsCache = new Dictionary<Node, List<EdgePart>>();

	private static readonly Dictionary<Vector2, VerticalLine> VerticalLines = new Dictionary<Vector2, VerticalLine>();

	private static readonly Dictionary<Vector2, HorizontalLine> HorizontalLines = new Dictionary<Vector2, HorizontalLine>();

	private static readonly Dictionary<Vector2, Curve[]> Curves = new Dictionary<Vector2, Curve[]>();

	private static readonly Dictionary<Vector2, VerticalLineOverCurve> VerticalLineOverCurves = new Dictionary<Vector2, VerticalLineOverCurve>();

	private static readonly Dictionary<Vector2, HorizontalLineOverCurve> HorizontalLineOverCurves = new Dictionary<Vector2, HorizontalLineOverCurve>();

	private static readonly Dictionary<Vector2, StartLine> StartLines = new Dictionary<Vector2, StartLine>();

	private static readonly Dictionary<Vector2, EndArrow> EndArrows = new Dictionary<Vector2, EndArrow>();

	public static void ClearAll()
	{
		foreach (EdgePart item in All)
		{
			item.NodesTo.Clear();
		}
		All.Clear();
		NodeToPartsCache.Clear();
		VerticalLines.Clear();
		HorizontalLines.Clear();
		Curves.Clear();
		VerticalLineOverCurves.Clear();
		HorizontalLineOverCurves.Clear();
		StartLines.Clear();
		EndArrows.Clear();
	}

	public static void Add(Node from, Node to)
	{
		if (!NodeToPartsCache.ContainsKey(to))
		{
			NodeToPartsCache[to] = new List<EdgePart>();
		}
		int num = to.Y - from.Y;
		if (num > 0)
		{
			AddCurve(to, new Curve(from.X + 1, (float)from.Y + 0.5f, CurveType.RightTop));
			for (int i = 0; i < num; i++)
			{
				AddPart(to, new VerticalLine(from.X + 1, (float)(from.Y + i) + 0.5f), VerticalLines);
				if (i > 0)
				{
					AddPart(to, new VerticalLineOverCurve(from.X + 1, (float)(from.Y + i) + 0.5f), VerticalLineOverCurves);
				}
			}
		}
		else if (num < 0)
		{
			AddCurve(to, new Curve(from.X + 1, (float)from.Y + 0.5f, CurveType.RightBottom));
			for (int num2 = 0; num2 > num; num2--)
			{
				AddPart(to, new VerticalLine(from.X + 1, (float)(from.Y + num2) - 0.5f), VerticalLines);
				if (num2 < 0)
				{
					AddPart(to, new VerticalLineOverCurve(from.X + 1, (float)(from.Y + 1 + num2) - 0.5f), VerticalLineOverCurves);
				}
			}
		}
		else
		{
			AddPart(to, new HorizontalLineOverCurve(from.X + 1, (float)to.Y + 0.5f), HorizontalLineOverCurves);
		}
		int num3 = to.X - from.X;
		if (num3 > 0)
		{
			if (num > 0)
			{
				AddCurve(to, new Curve(from.X + 1, (float)to.Y + 0.5f, CurveType.LeftBottom));
			}
			else if (num < 0)
			{
				AddCurve(to, new Curve(from.X + 1, (float)to.Y + 0.5f, CurveType.LeftTop));
			}
			else
			{
				AddPart(to, new HorizontalLineOverCurve(to.X, (float)to.Y + 0.5f), HorizontalLineOverCurves);
			}
			for (int j = 0; j < num3 - 1; j++)
			{
				AddPart(to, new HorizontalLine(from.X + 1 + j, (float)to.Y + 0.5f), HorizontalLines);
				AddPart(to, new HorizontalLineOverCurve(from.X + 2 + j, (float)to.Y + 0.5f), HorizontalLineOverCurves);
			}
		}
		else if (num3 >= 0)
		{
		}
		AddPart(to, new StartLine(from.X + 1, (float)from.Y + 0.5f), StartLines);
		AddPart(to, new EndArrow(to.X, (float)to.Y + 0.5f), EndArrows);
	}

	private static void AddPart<T>(Node to, T part, Dictionary<Vector2, T> All) where T : EdgePart
	{
		if (All.ContainsKey(part.Pos))
		{
			part = All[part.Pos];
		}
		else
		{
			All[part.Pos] = part;
		}
		part.NodesTo.Add(to);
		Edge.All.Add(part);
		NodeToPartsCache[to].Add(part);
	}

	private static void AddCurve(Node to, Curve part)
	{
		if (Curves.ContainsKey(part.Pos))
		{
			if (Curves[part.Pos][(int)part.Type] != null)
			{
				part = Curves[part.Pos][(int)part.Type];
			}
			else
			{
				Curves[part.Pos][(int)part.Type] = part;
			}
		}
		else
		{
			Curves[part.Pos] = new Curve[4];
			Curves[part.Pos][(int)part.Type] = part;
		}
		part.NodesTo.Add(to);
		All.Add(part);
		NodeToPartsCache[to].Add(part);
	}

	public static void ClearHighlited()
	{
		foreach (EdgePart item in All)
		{
			item.Highlited = false;
		}
	}

	public static void SetHighlited(Node to)
	{
		if (!NodeToPartsCache.ContainsKey(to))
		{
			return;
		}
		foreach (EdgePart item in NodeToPartsCache[to])
		{
			item.Highlited = true;
		}
	}

	public static void Draw(Rect visibleRect)
	{
		GUI.color = ModOptions.Colors.Arrow;
		foreach (EdgePart item in All)
		{
			if (!item.Highlited && item.Rect.xMin < visibleRect.xMax && item.Rect.xMax > visibleRect.xMin && item.Rect.yMin < visibleRect.yMax && item.Rect.yMax > visibleRect.yMin)
			{
				if (item is Curve curve)
				{
					GUI.DrawTextureWithTexCoords(item.Rect, item.Texture, curve.TexRect);
				}
				else
				{
					GUI.DrawTexture(item.Rect, item.Texture);
				}
			}
		}
		GUI.color = ModOptions.Colors.Highlighted;
		foreach (EdgePart item2 in All)
		{
			if (item2.Highlited && item2.Rect.xMin < visibleRect.xMax && item2.Rect.xMax > visibleRect.xMin && item2.Rect.yMin < visibleRect.yMax && item2.Rect.yMax > visibleRect.yMin)
			{
				if (item2 is Curve curve2)
				{
					GUI.DrawTextureWithTexCoords(item2.Rect, item2.Texture, curve2.TexRect);
				}
				else
				{
					GUI.DrawTexture(item2.Rect, item2.Texture);
				}
			}
		}
	}
}
