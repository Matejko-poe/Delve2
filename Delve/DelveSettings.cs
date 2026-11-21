using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Delve;

public class DelveSettings : ISettings
{
	public ToggleNode DelvePathWays = new ToggleNode(true);

	public RangeNode<int> DelvePathWaysNodeSize = new RangeNode<int>(7, 1, 200);

	public ColorBGRA DelvePathWaysNodeColor = new ColorBGRA(byte.MaxValue, 140, 0, byte.MaxValue);

	public ToggleNode DelveChests = new ToggleNode(true);

	public ToggleNode DelveMiningSuppliesDynamiteChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveMiningSuppliesDynamiteChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveMiningSuppliesDynamiteChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveMiningSuppliesFlaresChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveMiningSuppliesFlaresChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveMiningSuppliesFlaresChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveAzuriteVeinChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveAzuriteVeinChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveAzuriteVeinChestColor { get; set; } = (Color)(new ColorBGRA(0, 115, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveCurrencyChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveCurrencyChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveCurrencyChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveResonatorChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveResonatorChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveResonatorChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveFossilChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveFossilChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveFossilChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelvePathwayChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelvePathwayChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelvePathwayChestColor { get; set; } = (Color)(new ColorBGRA(0, 131, 0, byte.MaxValue));

	public ToggleNode DebugMode { get; set; } = new ToggleNode(false);

	public ToggleNode ShouldHideOnOpen { get; set; } = new ToggleNode(false);

	public HotkeyNode DebugHotkey { get; set; } = new HotkeyNode(Keys.Menu);

	public ToggleNode Enable { get; set; } = new ToggleNode(false);

	// Currency chests by tier: chaos (1) / exalted (2) / divine = "Hidden Opulence" (3)
	public ToggleNode CurrencyShowTier1 { get; set; } = new ToggleNode(true);
	public ToggleNode CurrencyShowTier2 { get; set; } = new ToggleNode(true);
	public ToggleNode CurrencyShowTier3 { get; set; } = new ToggleNode(true);
	public RangeNode<int> CurrencyTier1Size { get; set; } = new RangeNode<int>(9, 1, 200);
	public RangeNode<int> CurrencyTier2Size { get; set; } = new RangeNode<int>(12, 1, 200);
	public RangeNode<int> CurrencyTier3Size { get; set; } = new RangeNode<int>(16, 1, 200);

	// Resonator chests by chest tier (path Resonator1 = lowest .. Resonator3+ = highest)
	public ToggleNode ResonatorShowTier1 { get; set; } = new ToggleNode(true);
	public ToggleNode ResonatorShowTier2 { get; set; } = new ToggleNode(true);
	public ToggleNode ResonatorShowTier3 { get; set; } = new ToggleNode(true);
	// Event chests: Resonator Trove (Resonator3Encounter), Resonator Stash (Resonator1Encounter)
	public ToggleNode ResonatorShowEvent { get; set; } = new ToggleNode(true);

	// Azurite: hide the two smallest veins
	public ToggleNode AzuriteShowLowTier { get; set; } = new ToggleNode(true);

	// Opulent chest (top currency chest) alert
	public ToggleNode OpulentAlert { get; set; } = new ToggleNode(true);
	public ToggleNode OpulentAlertSound { get; set; } = new ToggleNode(true);
	public RangeNode<int> OpulentAlertSeconds { get; set; } = new RangeNode<int>(8, 2, 30);
	public ToggleNode OpulentPulse { get; set; } = new ToggleNode(true);

	// Radar lines only to Divine / Exalted currency chests
	public ToggleNode RadarOnlyHighValue { get; set; } = new ToggleNode(false);

	// Minimap: radar lines from player to chest entities
	public ToggleNode MinimapRadarLines { get; set; } = new ToggleNode(true);
	public RangeNode<int> MinimapRadarLineWidth { get; set; } = new RangeNode<int>(2, 1, 8);
}
