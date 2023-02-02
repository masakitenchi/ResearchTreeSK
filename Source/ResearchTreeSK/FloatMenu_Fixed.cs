using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ResearchTreeSK;

public class FloatMenu_Fixed : FloatMenu
{
	private readonly Vector2 _position;

	public FloatMenu_Fixed(List<FloatMenuOption> options, Vector2 position, bool focus = false)
		: base(options)
	{
		_position = position;
		vanishIfMouseDistant = false;
		focusWhenOpened = focus;
	}

	protected override void SetInitialSizeAndPosition()
	{
		Vector2 position = _position;
		if (position.x + InitialSize.x > (float)UI.screenWidth)
		{
			position.x = (float)UI.screenWidth - InitialSize.x;
		}
		if (position.y + InitialSize.y > (float)UI.screenHeight)
		{
			position.y = (float)UI.screenHeight - InitialSize.y;
		}
		windowRect = new Rect(position.x, position.y, InitialSize.x, InitialSize.y);
	}
}
