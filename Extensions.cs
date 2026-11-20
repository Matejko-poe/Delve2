using SharpDX;

public static class Extensions
{
	public static Vector2 TopMiddle(this RectangleF r)
	{
		return new Vector2(r.Center.X, r.Top);
	}

	public static Vector2 BottomMiddle(this RectangleF r)
	{
		return new Vector2(r.Center.X, r.Bottom);
	}

	public static Vector2 Clamp(this Vector2 v, RectangleF rectangle)
	{
		v = Vector2.Max(v, rectangle.TopLeft);
		v = Vector2.Min(v, rectangle.BottomRight);
		return v;
	}

	public static Vector2 LeftMiddle(this RectangleF r)
	{
		return new Vector2(r.Left, r.Center.Y);
	}

	public static Vector2 RightMiddle(this RectangleF r)
	{
		return new Vector2(r.Right, r.Center.Y);
	}

}
