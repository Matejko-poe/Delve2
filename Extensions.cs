using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Helpers;
using GameOffsets.Native;
using SharpDX;
using System.Runtime.InteropServices;

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

	public static bool HasEncounter(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.Read<bool>(((RemoteMemoryObject)cell).Address + 1163);
	}

	public static bool IsCellVisible(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.Read<bool>(((RemoteMemoryObject)cell).Address + 1164);
	}

	public static bool PathIsHidden(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.Read<bool>(((RemoteMemoryObject)cell).Address + 1167);
	}

	public static bool WasVisited(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.Read<bool>(((RemoteMemoryObject)cell).Address + 1168);
	}

	public static string IconPath(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.ReadStringU(((RemoteMemoryObject)cell).M.Read<long>(((RemoteMemoryObject)cell).Address + 1448, new int[1]), 256, true);
	}

	public static string Type(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.ReadStringU(((RemoteMemoryObject)cell).M.Read<long>(((RemoteMemoryObject)cell).Address + 1624, new int[1]), 256, true);
	}

	public static string TypeHuman(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.ReadStringU(((RemoteMemoryObject)cell).M.Read<long>(((RemoteMemoryObject)cell).Address + 1624, new int[1] { 8 }), 256, true);
	}

	public static string BiomeName(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.ReadStringU(((RemoteMemoryObject)cell).M.Read<long>(((RemoteMemoryObject)cell).Address + 1640, new int[1]), 256, true);
	}

	public static string BiomeDisplayName(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.ReadStringU(((RemoteMemoryObject)cell).M.Read<long>(((RemoteMemoryObject)cell).Address + 1640, new int[1] { 8 }), 256, true);
	}

	public static bool IsFeatureHovered(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.Read<bool>(((RemoteMemoryObject)cell).Address + 1703);
	}

	public static bool IsCellHovered(this DelveCell cell)
	{
		return ((RemoteMemoryObject)cell).M.Read<bool>(((RemoteMemoryObject)cell).Address + 1704);
	}

	public static string Mods(this DelveCell cell)
	{
		var nativeStr = ((RemoteMemoryObject)cell).M.Read<NativeUtf16Text>(((RemoteMemoryObject)cell).Address + 1296);
		return ((RemoteMemoryObject)cell).M.ReadStringU(nativeStr.Buffer, (int)nativeStr.Length);
	}
}
