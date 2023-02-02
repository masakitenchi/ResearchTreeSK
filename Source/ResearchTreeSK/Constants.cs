using UnityEngine;

namespace ResearchTreeSK;

public static class Constants
{
	public const double Epsilon = 0.0001;

	public const float DragStartDistanceSquared = 20f;

	public const float DetailedModeZoomLevelCutoff = 1.5f;

	public const float Margin = 6f;

	public const float QueueLabelSize = 30f;

	public const float SmallQueueLabelSize = 20f;

	public const float ZoomStep = 0.05f;

	public static readonly Vector2 IconSize = new Vector2(18f, 18f);

	public static readonly Vector2 TreeLabelSize = new Vector2(200f, 30f);

	public static readonly float TreeLabelDistance = 16f;

	public const float ScrollbarWidth = 18f;

	public const int TreeSizeBorderEditMode = 5;

	public const float AnimatedHighlightDuration = 3f;

	public const float AnimatedHighlightFadeInDuration = 0.3f;

	public const float AnimatedHighlightFadeOutDuration = 0.7f;
}
