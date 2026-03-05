using System;

namespace DefaultNamespace;

public static class Helper
{
	private static long _lastTime;

	internal static float _deltaTime;

	internal static Random random = new Random();

	public static void GetDeltaTime()
	{
		long ticks = DateTime.Now.Ticks;
		float deltaTime = (ticks - _lastTime) / 10000;
		_lastTime = ticks;
		_deltaTime = deltaTime;
	}

	internal static float MoveTowards(float cur, float tar, float max)
	{
		if (Math.Abs(tar - cur) <= max)
		{
			return tar;
		}
		return cur + (float)Math.Sign(tar - cur) * max;
	}
}
