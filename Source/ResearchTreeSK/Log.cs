using Verse;

namespace ResearchTreeSK;

public static class Log
{
	public static void Message(string msg, params object[] args)
	{
		Verse.Log.Message(Format(msg, args));
	}

	public static void Warning(string msg, params object[] args)
	{
		Verse.Log.Warning(Format(msg, args));
	}

	private static string Format(string msg, params object[] args)
	{
		return "ResearchTreeSK :: " + string.Format(msg, args);
	}

	public static void Error(string msg, bool once, params object[] args)
	{
		string text = Format(msg, args);
		if (once)
		{
			Verse.Log.ErrorOnce(text, text.GetHashCode());
		}
		else
		{
			Verse.Log.Error(text);
		}
	}

	public static void Debug(string msg, params object[] args)
	{
		if (Settings.DebugMode)
		{
			Verse.Log.Message(Format(msg, args));
		}
	}

	public static void Trace(string msg, params object[] args)
	{
		if (Settings.DebugMode)
		{
			Verse.Log.Message(Format(msg, args));
		}
	}
}
