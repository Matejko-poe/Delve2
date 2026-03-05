using System;
using ExileCore.Shared.AtlasHelper;
using SharpDX;

namespace Delve.Libs;

public class MapIcon
{
	private const int TileToGridConversion = 23;

	private const int TileToWorldConversion = 250;

	public const float GridToWorldMultiplier = 10.869565f;

	internal static double _mapScale;

	private const double CameraAngle = 0.6754424205218056;

	private static readonly float CameraAngleCos = (float)Math.Cos(0.6754424205218056);

	private static readonly float CameraAngleSin = (float)Math.Sin(0.6754424205218056);

	public AtlasTexture Texture { get; set; }

	public Color Color { get; set; } = Color.White;

	public float Size { get; set; } = 13f;

	public MapIcon()
	{
	}

	public MapIcon(AtlasTexture texture, Color color, float size)
	{
		Texture = texture;
		Color = color;
		Size = size;
	}

	public MapIcon(AtlasTexture texture, float size)
	{
		Texture = texture;
		Size = size;
	}

	internal static Vector2 TranslateGridDeltaToMapDelta(Vector2 delta, float deltaZ)
	{
		deltaZ /= 10.869565f;
		return (float)_mapScale * new Vector2((delta.X - delta.Y) * CameraAngleCos, (deltaZ - (delta.X + delta.Y)) * CameraAngleSin);
	}

	public static Vector2 DeltaInWorldToMinimapDelta(Vector2? delta, double diag, float scale, float deltaZ = 0f)
	{
		float num = (float)(diag * Math.Cos(0.6632251143455505) / (double)scale);
		float num2 = (float)(diag * Math.Sin(0.6632251143455505) / (double)scale);
		return new Vector2((delta.Value.X - delta.Value.Y) * num, deltaZ - (delta.Value.X + delta.Value.Y) * num2);
	}
}
