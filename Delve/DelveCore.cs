using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using CoPilot;
using DefaultNamespace;
using Delve.Libs;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SharpDX;

namespace Delve;

public class DelveCore : BaseSettingsPlugin<DelveSettings>
{
	public class LargeMapData
	{
		public Camera Camera { get; set; }

		public ExileCore.PoEMemory.Elements.Map MapWindow { get; set; }

		public RectangleF MapRec { get; set; }

		public SharpDX.Vector2 PlayerPos { get; set; }

		public float PlayerPosZ { get; set; }

		public SharpDX.Vector2 ScreenCenter { get; set; }

		public float Diag { get; set; }

		public float K { get; set; }

		public float Scale { get; set; }

		public LargeMapData(GameController GC)
		{
			Camera = GC.Game.IngameState.Camera;
			MapWindow = GC.Game.IngameState.IngameUi.Map;
			MapRec = ((Element)MapWindow).GetClientRect();
			PlayerPos = GC.Player.GetComponent<Positioned>().GridPos;
			PlayerPosZ = GC.Player.GetComponent<Render>().Z;
			ScreenCenter = MathHepler.Translate(new SharpDX.Vector2(MapRec.Width / 2f, MapRec.Height / 2f), 0f, -20f) + new SharpDX.Vector2(MapRec.X, MapRec.Y) + new SharpDX.Vector2(MapWindow.LargeMapShiftX, MapWindow.LargeMapShiftY);
			Diag = (float)Math.Sqrt(Camera.Width * Camera.Width + Camera.Height * Camera.Height);
			K = (((float)Camera.Width < 1024f) ? 1120f : 1024f);
			Scale = K / (float)Camera.Height * (float)Camera.Width * 3f / 4f / MapWindow.LargeMapZoom;
		}
	}

	public class FossilTiers
	{
		[JsonProperty("t1")]
		public string[] T1 { get; set; }

		[JsonProperty("t2")]
		public string[] T2 { get; set; }

		[JsonProperty("t3")]
		public string[] T3 { get; set; }
	}

	private RectangleF DrawRect;


	private List<DelveEntity> DelveEntities;

	public Version version = Assembly.GetExecutingAssembly().GetName().Version;

	public string PluginVersion;

	public DateTime buildDate;

	public static int Selected;

	public static int idPop;

	private bool IsAzuriteMine;


	public FossilTiers FossilList = new FossilTiers();


	private float updateCooldown = 250f;

	public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
	{
		MetadataPropertyHandling = (MetadataPropertyHandling)2,
		DateParseHandling = (DateParseHandling)0,
		Converters = { (JsonConverter)new IsoDateTimeConverter
		{
			DateTimeStyles = DateTimeStyles.AssumeUniversal
		} }
	};

	private AtlasTexture _iconAbberantFossil;

	private AtlasTexture _iconAbberantFossilT1;

	private AtlasTexture _iconAbberantFossilT2;

	private AtlasTexture _iconAbberantFossilT3;

	private AtlasTexture _iconAbyssChest;

	private AtlasTexture _iconAbyssCrack;

	private AtlasTexture _iconAbyssJewelChest;

	private AtlasTexture _iconAdditionalSockets;

	private AtlasTexture _iconAmbushInvasionChest;

	private AtlasTexture _iconAzurite;

	private AtlasTexture _iconAzuriteT1;

	private AtlasTexture _iconAzuriteT2;

	private AtlasTexture _iconAzuriteT3;

	private AtlasTexture _iconBestiaryChest;

	private AtlasTexture _iconBeyondChest;

	private AtlasTexture _iconBloodlinesChest;

	private AtlasTexture _iconBombs;

	private AtlasTexture _iconBreachChest;

	private AtlasTexture _iconCorrupted;

	private AtlasTexture _iconCurrency;

	private AtlasTexture _iconDivine;

	private AtlasTexture _iconExalted;



	private readonly HashSet<uint> _alerted = new HashSet<uint>();

	private DateTime _alertUntil = DateTime.MinValue;

	private string _alertText = "";

	private AtlasTexture _iconDelveChest;

	private AtlasTexture _iconDivinationCard;

	private AtlasTexture _iconDominationChest;

	private AtlasTexture _iconEnchant;

	private AtlasTexture _iconEssence;

	private AtlasTexture _iconFlare;

	private AtlasTexture _iconFragment;


	private AtlasTexture _iconHarbingerChest;


	private AtlasTexture _iconHighLevelGem;

	private AtlasTexture _iconIncursionChest;

	private AtlasTexture _iconMap;

	private AtlasTexture _iconNemesisChest;

	private AtlasTexture _iconOfferingChest;

	private AtlasTexture _iconOnslaughtChest;

	private AtlasTexture _iconPaleCourtComplete;

	private AtlasTexture _iconPerandusChest;

	private AtlasTexture _iconPerandusCoins;

	private AtlasTexture _iconProphecy;

	private AtlasTexture _iconRampageChest;

	private AtlasTexture _iconRareAmulet;

	private AtlasTexture _iconResonatorT1;

	private AtlasTexture _iconResonatorT2;

	private AtlasTexture _iconResonatorT3;

	private AtlasTexture _iconSilverCoin;

	private AtlasTexture _iconSixLink;

	private AtlasTexture _iconSpeedArmour;

	private AtlasTexture _iconStrongbox;

	private AtlasTexture _iconStygianViseChest;

	private AtlasTexture _iconTalismanChest;

	private AtlasTexture _iconTempestChest;

	private AtlasTexture _iconTormentChest;

	private AtlasTexture _iconUniqueManaFlask;

	private AtlasTexture _iconUpgrade2x1A;

	private AtlasTexture _iconUpgrade2x2A;

	private AtlasTexture _iconWarbandsChest;

	private AtlasTexture _iconWisdomCurrency;

	public LargeMapData LargeMapInformation { get; set; }

	private string SourceDir;
	private AtlasTexturesProcessor _delveAtlas;

	private string ResolveSourceDirectory()
	{
		// Try Source path first (ExileCore compiles from Source, but DirectoryFullName points to Temp)
		var sourceDir = Path.Combine(base.PluginManager.SourcePluginDirectoryPath, base.DirectoryName);
		if (Directory.Exists(Path.Combine(sourceDir, "Textures")))
			return sourceDir;
		// Fallback to DirectoryFullName if textures exist there
		if (Directory.Exists(Path.Combine(base.DirectoryFullName, "Textures")))
			return base.DirectoryFullName;
		// Last resort: search for textures folder case-insensitively
		if (Directory.Exists(Path.Combine(base.DirectoryFullName, "textures")))
			return base.DirectoryFullName;
		return sourceDir; // return source anyway, error will be logged by AtlasTextureInit
	}

	private AtlasTexture GetDelveAtlasTexture(string textureName)
	{
		return _delveAtlas?.GetTextureByName(textureName);
	}

	public override bool Initialise()
	{
		SourceDir = ResolveSourceDirectory();
		// Create atlas from Source directory (not Temp)
		var atlasJsonPath = Path.Combine(SourceDir, "Textures", "DelveAtlas.json");
		var atlasPngPath = Path.Combine(SourceDir, "Textures", "DelveAtlas.png");
		if (File.Exists(atlasJsonPath) && File.Exists(atlasPngPath))
		{
			_delveAtlas = base.CreateAtlas(atlasJsonPath, atlasPngPath);
		}
		else
		{
			base.LogError($"Delve atlas not found at: {atlasJsonPath}", 10f);
		}
		AtlasTextureInit();
		Input.RegisterKey(base.Settings.DebugHotkey.Value);
		buildDate = new DateTime(2026, 11, 21);
		base.Name = "Delve";
		PluginVersion = "2.0.2";
		DelveEntities = new List<DelveEntity>();
		try
		{
			base.GameController.SleepingEntityListWrapper.StartWork();
		}
		catch
		{
		}
		base.GameController.Area.OnAreaChange += ((BaseSettingsPlugin<DelveSettings>)this).AreaChange;
		try { IsAzuriteMine = base.GameController.Area.CurrentArea.Name.Contains("Azurite Mine"); } catch { }
		if (File.Exists(SourceDir + "\\Fossil_Tiers.json"))
		{
			string text = File.ReadAllText(SourceDir + "\\Fossil_Tiers.json");
			FossilList = JsonConvert.DeserializeObject<FossilTiers>(text, JsonSettings);
		}
		else
		{
			base.LogError("Error loading Fossil_Tiers.json, Please re download from Random Features github repository", 10f);
		}
		return true;
	}

	public override void AreaChange(AreaInstance area)
	{
		base.AreaChange(area);
		DelveEntities = new List<DelveEntity>();
		_alerted.Clear();
		_alertUntil = DateTime.MinValue;
		IsAzuriteMine = base.GameController.Area.CurrentArea.Name.Contains("Azurite Mine");
	}

	private static bool IsResonatorEventPath(string path)
	{
		return path.StartsWith("Metadata/Chests/DelveChests/Resonator") && path.EndsWith("Encounter");
	}

	private static bool IsOpulentPath(string path)
	{
		return path.StartsWith("Metadata/Chests/DelveChests/") && !path.EndsWith("Encounter") && path.Contains("Currency3");
	}

	private static bool IsExaltedPath(string path)
	{
		return path.StartsWith("Metadata/Chests/DelveChests/") && !path.EndsWith("Encounter") && path.Contains("Currency2");
	}

	private void CheckOpulentAlerts()
	{
		if (!base.Settings.OpulentAlert.Value)
		{
			return;
		}
		foreach (DelveEntity e in DelveEntities)
		{
			if (e == null || e.IsOpened || !IsOpulentPath(e.Path) || !_alerted.Add(e.Id))
			{
				continue;
			}
			_alertText = "OPULENT CHEST (Divine) detected!";
			_alertUntil = DateTime.UtcNow.AddSeconds(base.Settings.OpulentAlertSeconds.Value);
			if (base.Settings.OpulentAlertSound.Value)
			{
				try
				{
					string wav = Path.Combine(SourceDir, "opulent.wav");
					if (File.Exists(wav))
					{
						new System.Media.SoundPlayer(wav).Play();
					}
					else
					{
						System.Media.SystemSounds.Exclamation.Play();
					}
				}
				catch
				{
				}
			}
		}
	}

	private void DrawHud()
	{
		SharpDX.Vector2 screen = new SharpDX.Vector2(base.GameController.Game.IngameState.Camera.Width, base.GameController.Game.IngameState.Camera.Height);
		if (DateTime.UtcNow < _alertUntil)
		{
			base.Graphics.DrawTextWithBackground(_alertText, new System.Numerics.Vector2(screen.X / 2f, screen.Y * 0.18f), Color.Gold, FontAlign.Center, Color.Black);
		}
	}

	private void DrawData(string icon, string data)
	{
		System.Numerics.Vector2 vector = base.Graphics.MeasureText(data, 20);
		DrawRect.Width = DrawRect.Height + vector.X + 10f;
		base.Graphics.DrawBox(DrawRect, Color.Black);
		RectangleF drawRect = DrawRect;
		drawRect.Width = drawRect.Height;
		base.Graphics.DrawImage(Path.Combine(SourceDir, icon), drawRect);
		base.Graphics.DrawFrame(DrawRect, Color.Gray, 2);
		SharpDX.Vector2 topLeft = DrawRect.TopLeft;
		topLeft.Y += DrawRect.Height / 2f;
		topLeft.X += DrawRect.Height + 3f;
		base.Graphics.DrawText(data, topLeft, Color.White, 20, (FontAlign)1);
		DrawRect.X += DrawRect.Width + 1f;
	}

	public override void Render()
	{
		base.Render();
		if (!base.Settings.Enable.Value || !IsAzuriteMine)
		{
			return;
		}
		Helper.GetDeltaTime();
		updateCooldown = Helper.MoveTowards(updateCooldown, 0f, Helper._deltaTime);
		if (updateCooldown == 0f)
		{
			UpdateEntitys();
		}
		ExileCore.PoEMemory.Elements.Map map = base.GameController.Game.IngameState.IngameUi.Map;
		SubMap val = ((RemoteMemoryObject)map.LargeMap).AsObject<SubMap>();
		MapIcon._mapScale = (float)base.GameController.IngameState.Camera.Height / 677f * val.Zoom;
		SubterraneanChart delveWindow = base.GameController.Game.IngameState.IngameUi.DelveWindow;
		if (!((Element)delveWindow).IsVisible)
		{
			RenderMapImages();
		}
		try
		{
			CheckOpulentAlerts();
			DrawHud();
		}
		catch
		{
		}
		if (base.Settings.DebugHotkey.PressedOnce())
		{
			base.Settings.DebugMode.Value = !base.Settings.DebugMode.Value;
		}
		if (!base.Settings.DebugMode.Value)
		{
			return;
		}
		foreach (DelveEntity delveEntity in DelveEntities)
		{
			if (!delveEntity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveLight"))
			{
				bool? flag = delveEntity?.IsOpened;
				if (!base.Settings.ShouldHideOnOpen.Value || flag != true)
				{
					string text = delveEntity.Path.Replace("Metadata/Chests/DelveChests/", "");
					System.Numerics.Vector2 vector = base.Graphics.MeasureText(text, 0);
					SharpDX.Vector2 vector2 = base.GameController.Game.IngameState.Camera.WorldToScreen(delveEntity.Pos);
					base.Graphics.DrawBox(new RectangleF(vector2.X - vector.X / 2f - 10f, vector2.Y - vector.Y / 2f, vector.X + 20f, vector.Y * 2f), Color.White);
					base.Graphics.DrawText(text, vector2, Color.Black, 20, (FontAlign)1);
				}
			}
		}
	}

	private void DrawToLargeMiniMap(DelveEntity entity)
	{
		if (entity.IsOpened)
		{
			return;
		}
		MapIcon mapIcon = GetMapIcon(entity);
		if (mapIcon == null)
		{
			return;
		}
		float rendererPosZ = entity.RendererPosZ;
		try
		{
			SharpDX.Vector2 vector = LargeMapInformation.ScreenCenter + MapIcon.TranslateGridDeltaToMapDelta(entity.GridPos - LargeMapInformation.PlayerPos, rendererPosZ - LargeMapInformation.PlayerPosZ);
			float num = mapIcon.Size * 2f;
			RectangleF iconRect = new RectangleF(vector.X - num / 2f, vector.Y - num / 2f, num, num);
			base.Graphics.DrawImage(mapIcon.Texture, iconRect, mapIcon.Color);
			DrawOpulentPulse(entity, iconRect);
		}
		catch (NullReferenceException)
		{
		}
	}

	private void DrawToSmallMiniMap(DelveEntity entity)
	{
		if (entity.IsOpened)
		{
			return;
		}
		MapIcon mapIcon = GetMapIcon(entity);
		if (mapIcon == null)
		{
			return;
		}
		Element smallMiniMap = base.GameController.Game.IngameState.IngameUi.Map.SmallMiniMap;
		SharpDX.Vector2 gridPos = base.GameController.Player.GetComponent<Positioned>().GridPos;
		float z = base.GameController.Player.GetComponent<Render>().Z;
		RectangleF clientRect = smallMiniMap.GetClientRect();
		SharpDX.Vector2 vector = MathHepler.Translate(new SharpDX.Vector2(clientRect.X + clientRect.Width / 2f, clientRect.Y + clientRect.Height / 2f), 0f, 0f);
		double diag = Math.Sqrt(clientRect.Width * clientRect.Width + clientRect.Height * clientRect.Height) / 2.0;
		float rendererPosZ = entity.RendererPosZ;
		try
		{
			SharpDX.Vector2 vector2 = vector + MapIcon.DeltaInWorldToMinimapDelta(entity.GridPos - gridPos, diag, 240f, (rendererPosZ - z) / 20f);
			float size = mapIcon.Size;
			RectangleF value = new RectangleF(vector2.X - size / 2f, vector2.Y - size / 2f, size, size);
			clientRect.Contains(ref value, out var result);
			if (result)
			{
				base.Graphics.DrawImage(mapIcon.Texture, value, mapIcon.Color);
				DrawOpulentPulse(entity, value);
			}
		}
		catch (NullReferenceException)
		{
		}
	}

	private void DrawOpulentPulse(DelveEntity entity, RectangleF iconRect)
	{
		if (!base.Settings.OpulentPulse.Value || !IsOpulentPath(entity.Path))
		{
			return;
		}
		float k = (float)(0.5 + 0.5 * Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * 6.0));
		RectangleF r = iconRect;
		r.Inflate(2f + 4f * k, 2f + 4f * k);
		base.Graphics.DrawFrame(r, new Color(255, 215, 0, (int)(110 + 145 * k)), 2);
	}

	private bool IsRadarLineTarget(DelveEntity entity)
	{
		if (base.Settings.RadarOnlyHighValue.Value && !IsOpulentPath(entity.Path) && !IsExaltedPath(entity.Path))
		{
			return false;
		}
		// Exclude terrain objects (walls, lights/path waypoints) - only chests are clickable targets
		return entity.Path.StartsWith("Metadata/Chests/DelveChests/");
	}

	private void DrawRadarLinesToLargeMiniMap()
	{
		if (!base.Settings.MinimapRadarLines.Value)
			return;
		foreach (DelveEntity entity in DelveEntities)
		{
			if (entity == null || entity.IsOpened || !IsRadarLineTarget(entity))
				continue;
			MapIcon mapIcon = GetMapIcon(entity);
			if (mapIcon == null)
				continue;
			float rendererPosZ = entity.RendererPosZ;
			try
			{
				SharpDX.Vector2 entityMapPos = LargeMapInformation.ScreenCenter + MapIcon.TranslateGridDeltaToMapDelta(entity.GridPos - LargeMapInformation.PlayerPos, rendererPosZ - LargeMapInformation.PlayerPosZ);
				base.Graphics.DrawLine(LargeMapInformation.ScreenCenter, entityMapPos, base.Settings.MinimapRadarLineWidth.Value, mapIcon.Color);
			}
			catch (NullReferenceException)
			{
			}
		}
	}

	private void DrawRadarLinesToSmallMiniMap()
	{
		if (!base.Settings.MinimapRadarLines.Value)
			return;
		Element smallMiniMap = base.GameController.Game.IngameState.IngameUi.Map.SmallMiniMap;
		SharpDX.Vector2 playerGridPos = base.GameController.Player.GetComponent<Positioned>().GridPos;
		float playerZ = base.GameController.Player.GetComponent<Render>().Z;
		RectangleF clientRect = smallMiniMap.GetClientRect();
		SharpDX.Vector2 mapCenter = new SharpDX.Vector2(clientRect.X + clientRect.Width / 2f, clientRect.Y + clientRect.Height / 2f);
		double diag = Math.Sqrt(clientRect.Width * clientRect.Width + clientRect.Height * clientRect.Height) / 2.0;
		foreach (DelveEntity entity in DelveEntities)
		{
			if (entity == null || entity.IsOpened || !IsRadarLineTarget(entity))
				continue;
			MapIcon mapIcon = GetMapIcon(entity);
			if (mapIcon == null)
				continue;
			float rendererPosZ = entity.RendererPosZ;
			try
			{
				SharpDX.Vector2 entityMapPos = mapCenter + MapIcon.DeltaInWorldToMinimapDelta(entity.GridPos - playerGridPos, diag, 240f, (rendererPosZ - playerZ) / 20f);
				SharpDX.Vector2 clampedEntityPos = entityMapPos.Clamp(clientRect);
				base.Graphics.DrawLine(mapCenter, clampedEntityPos, base.Settings.MinimapRadarLineWidth.Value, mapIcon.Color);
			}
			catch (NullReferenceException)
			{
			}
		}
	}

	private void UpdateEntitys()
	{
		updateCooldown = 250f;
		if (!IsAzuriteMine)
		{
			return;
		}
		ICollection<Entity> entities = base.GameController.Entities;
		foreach (Entity entity in entities)
		{
			if (entity != null && (entity.Path.StartsWith("Metadata/Chests/DelveChests/") || entity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveLight")))
			{
				DelveEntity delveEntity = DelveEntities.FirstOrDefault((DelveEntity x) => x != null && x.Id == entity.Id);
				if (delveEntity != null)
				{
					delveEntity.IsAlive = entity.IsAlive;
					delveEntity.IsOpened = entity.IsOpened;
				}
				else
				{
					// entities that were already in the zone when the plugin was (re)loaded never reach EntityAdded
					EntityAdded(entity);
				}
			}
		}
	}

	public override void EntityAdded(Entity entity)
	{
		if (IsAzuriteMine && entity != null && !DelveEntities.Exists((DelveEntity x) => x.Id == entity.Id) && (entity.Path.StartsWith("Metadata/Chests/DelveChests/") || entity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveLight")))
		{
			bool isOpened = entity.IsOpened;
			Render component = entity.GetComponent<Render>();
			float rendererPosZ = ((component != null) ? component.Pos.Z : entity.Pos.Z);
			SharpDX.Vector2 gridPos = entity.GridPos;
			uint id = entity.Id;
			DelveEntities.Add(new DelveEntity(entity.Pos, entity.Path, isOpened, rendererPosZ, gridPos, entity.IsAlive, id));
		}
	}

	private void RenderMapImages()
	{
		if (base.GameController?.Player == null)
			return;

		if (base.GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible)
		{
			try
			{
				LargeMapInformation = new LargeMapData(base.GameController);
			}
			catch (NullReferenceException)
			{
				return;
			}
			foreach (DelveEntity delveEntity in DelveEntities)
			{
				if (delveEntity != null)
				{
					DrawToLargeMiniMap(delveEntity);
				}
			}
			DrawRadarLinesToLargeMiniMap();
			return;
		}
		if (!base.GameController.Game.IngameState.IngameUi.Map.SmallMiniMap.IsVisible)
		{
			return;
		}
		foreach (DelveEntity delveEntity2 in DelveEntities)
		{
			if (delveEntity2 != null)
			{
				DrawToSmallMiniMap(delveEntity2);
			}
		}
		DrawRadarLinesToSmallMiniMap();
	}

	public void DelveMenu(int idIn, out int idPop)
	{
		idPop = idIn;
		DelveMenuExtras();
		if (ImGui.TreeNode("Delve Path's"))
		{
			ImGui.PushID(idPop);
			base.Settings.DelvePathWays.Value = ImGuiExtension.Checkbox(base.Settings.DelvePathWays.Value ? "Show" : "Hidden", base.Settings.DelvePathWays.Value);
			ImGui.PopID();
			idPop++;
			ImGui.Spacing();
			ImGui.PushID(idPop);
			base.Settings.DelvePathWaysNodeSize.Value = ImGuiExtension.IntSlider("Size", base.Settings.DelvePathWaysNodeSize);
			ImGui.PopID();
			idPop++;
			ImGui.PushID(idPop);
			base.Settings.DelvePathWaysNodeColor = ImGuiExtension.ColorPicker("Color", base.Settings.DelvePathWaysNodeColor);
			ImGui.PopID();
			idPop++;
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Delve Chests"))
		{
			ImGui.PushID(idPop);
			base.Settings.DelveChests.Value = ImGuiExtension.Checkbox(base.Settings.DelveChests.Value ? "Show" : "Hidden", base.Settings.DelveChests.Value);
			ImGui.PopID();
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelvePathwayChest.Value = ImGuiExtension.Checkbox($"Hidden Chests on the way##{idPop}", base.Settings.DelvePathwayChest.Value);
			idPop++;
			base.Settings.DelvePathwayChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelvePathwayChestSize);
			idPop++;
			base.Settings.DelvePathwayChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelvePathwayChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveMiningSuppliesDynamiteChest.Value = ImGuiExtension.Checkbox($"Dynamite Supplies##{idPop}", base.Settings.DelveMiningSuppliesDynamiteChest.Value);
			idPop++;
			base.Settings.DelveMiningSuppliesDynamiteChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveMiningSuppliesDynamiteChestSize);
			idPop++;
			base.Settings.DelveMiningSuppliesDynamiteChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveMiningSuppliesDynamiteChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveMiningSuppliesFlaresChest.Value = ImGuiExtension.Checkbox($"Flare Supplies##{idPop}", base.Settings.DelveMiningSuppliesFlaresChest.Value);
			idPop++;
			base.Settings.DelveMiningSuppliesFlaresChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveMiningSuppliesFlaresChestSize);
			idPop++;
			base.Settings.DelveMiningSuppliesFlaresChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveMiningSuppliesFlaresChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveCurrencyChest.Value = ImGuiExtension.Checkbox($"Currency Chests##{idPop}", base.Settings.DelveCurrencyChest.Value);
			idPop++;
			base.Settings.DelveCurrencyChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveCurrencyChestSize);
			idPop++;
			base.Settings.DelveCurrencyChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveCurrencyChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveAzuriteVeinChest.Value = ImGuiExtension.Checkbox($"Azurite Veins##{idPop}", base.Settings.DelveAzuriteVeinChest.Value);
			idPop++;
			base.Settings.DelveAzuriteVeinChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveAzuriteVeinChestSize);
			idPop++;
			base.Settings.DelveAzuriteVeinChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveAzuriteVeinChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveResonatorChest.Value = ImGuiExtension.Checkbox($"Resonator Chests##{idPop}", base.Settings.DelveResonatorChest.Value);
			idPop++;
			base.Settings.DelveResonatorChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveResonatorChestSize);
			idPop++;
			base.Settings.DelveResonatorChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveResonatorChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveFossilChest.Value = ImGuiExtension.Checkbox($"Fossil Chests##{idPop}", base.Settings.DelveFossilChest.Value);
			idPop++;
			base.Settings.DelveFossilChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveFossilChestSize);
			idPop++;
			base.Settings.DelveFossilChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveFossilChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Minimap radar lines"))
		{
			base.Settings.MinimapRadarLines.Value = ImGuiExtension.Checkbox($"Draw Minimap Radar Lines to Chests##{idPop}", base.Settings.MinimapRadarLines.Value);
			idPop++;
			base.Settings.MinimapRadarLineWidth.Value = ImGuiExtension.IntSlider($"  Minimap Radar Line Width##{idPop}", base.Settings.MinimapRadarLineWidth);
			idPop++;
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Debug Mode"))
		{
			ImGui.PushID(idPop);
			base.Settings.DebugHotkey.Value = ImGuiExtension.HotkeySelector("Debug Mode Hotkey", base.Settings.DebugHotkey.Value);
			ImGui.PopID();
			idPop++;
			base.Settings.DebugMode.Value = ImGuiExtension.Checkbox($"Debug Mode##{idPop}", base.Settings.DebugMode.Value);
			idPop++;
			base.Settings.ShouldHideOnOpen.Value = ImGuiExtension.Checkbox($"Hide Chest Name When Opened##{idPop}", base.Settings.ShouldHideOnOpen.Value);
			idPop++;
			ImGui.TreePop();
		}
	}

	private void DelveMenuExtras()
	{
		var st = base.Settings;
		if (ImGui.TreeNode("Currency chests by tier"))
		{
			st.CurrencyShowTier1.Value = ImGuiExtension.Checkbox("Show Chaos tier##cur1", st.CurrencyShowTier1.Value);
			st.CurrencyTier1Size.Value = ImGuiExtension.IntSlider("Chaos size##cur1s", st.CurrencyTier1Size);
			st.CurrencyShowTier2.Value = ImGuiExtension.Checkbox("Show Exalted tier##cur2", st.CurrencyShowTier2.Value);
			st.CurrencyTier2Size.Value = ImGuiExtension.IntSlider("Exalted size##cur2s", st.CurrencyTier2Size);
			st.CurrencyShowTier3.Value = ImGuiExtension.Checkbox("Show Divine tier (Hidden Opulence)##cur3", st.CurrencyShowTier3.Value);
			st.CurrencyTier3Size.Value = ImGuiExtension.IntSlider("Divine size##cur3s", st.CurrencyTier3Size);
			st.RadarOnlyHighValue.Value = ImGuiExtension.Checkbox("Radar lines only to Divine/Exalted chests##radhv", st.RadarOnlyHighValue.Value);
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Resonator / Azurite filters"))
		{
			st.ResonatorShowTier1.Value = ImGuiExtension.Checkbox("Resonator chest, lowest tier (Resonator1, ~2c per open)##res1", st.ResonatorShowTier1.Value);
			st.ResonatorShowTier2.Value = ImGuiExtension.Checkbox("Resonator chest, middle tier (Resonator2, ~5c per open)##res2", st.ResonatorShowTier2.Value);
			st.ResonatorShowTier3.Value = ImGuiExtension.Checkbox("Resonator chest, highest tier (Resonator3-5, Prime Chaotic Resonator)##res3", st.ResonatorShowTier3.Value);
			st.ResonatorShowEvent.Value = ImGuiExtension.Checkbox("Event resonator chests (Resonator Trove / Stash)##resev", st.ResonatorShowEvent.Value);
			st.AzuriteShowLowTier.Value = ImGuiExtension.Checkbox("Azurite: show the two smallest veins##azlow", st.AzuriteShowLowTier.Value);
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Opulent chest alert"))
		{
			st.OpulentAlert.Value = ImGuiExtension.Checkbox("Alert when an Opulent (Divine) chest appears##opal", st.OpulentAlert.Value);
			st.OpulentAlertSound.Value = ImGuiExtension.Checkbox("Play sound (opulent.wav in plugin folder, else system sound)##opsnd", st.OpulentAlertSound.Value);
			st.OpulentAlertSeconds.Value = ImGuiExtension.IntSlider("Alert duration (s)##opsec", st.OpulentAlertSeconds);
			st.OpulentPulse.Value = ImGuiExtension.Checkbox("Pulsing frame on the minimap##oppulse", st.OpulentPulse.Value);
			ImGui.TreePop();
		}
	}

	public override void DrawSettings()
	{
		ImGui.BulletText(base.Name + ": v" + PluginVersion);
		ImGui.BulletText($"Last Updated: {buildDate}");
		idPop = 1;
		ImGuiTreeNodeFlags val = (ImGuiTreeNodeFlags)18; // Framed | NoAutoOpenOnLog; NoTreePushOnOpen removed: TreePop() below needs the pushed ID
		ImGui.PushID(1000);
		if (ImGui.TreeNodeEx("Settings", val))
		{
			DelveMenu(idPop, out var num);
			idPop = num;
			ImGui.TreePop();
		}
		ImGui.PopID();

	}

	public void AtlasTextureInit()
	{
		_iconAbberantFossil = GetDelveAtlasTexture("AbberantFossil");
		_iconAbberantFossilT1 = GetDelveAtlasTexture("AbberantFossilT1");
		_iconAbberantFossilT2 = GetDelveAtlasTexture("AbberantFossilT2");
		_iconAbberantFossilT3 = GetDelveAtlasTexture("AbberantFossilT3");
		_iconAbyssChest = GetDelveAtlasTexture("AbyssChest");
		_iconAbyssCrack = GetDelveAtlasTexture("AbyssCrack");
		_iconAbyssJewelChest = GetDelveAtlasTexture("AbyssJewelChest");
		_iconAdditionalSockets = GetDelveAtlasTexture("AdditionalSockets");
		_iconAmbushInvasionChest = GetDelveAtlasTexture("AmbushInvasionChest");
		_iconAzurite = GetDelveAtlasTexture("Azurite");
		_iconAzuriteT1 = GetDelveAtlasTexture("AzuriteT1");
		_iconAzuriteT2 = GetDelveAtlasTexture("AzuriteT2");
		_iconAzuriteT3 = GetDelveAtlasTexture("AzuriteT3");
		_iconBestiaryChest = GetDelveAtlasTexture("BestiaryChest");
		_iconBeyondChest = GetDelveAtlasTexture("BeyondChest");
		_iconBloodlinesChest = GetDelveAtlasTexture("BloodlinesChest");
		_iconBombs = GetDelveAtlasTexture("Bombs");
		_iconBreachChest = GetDelveAtlasTexture("BreachChest");
		_iconCorrupted = GetDelveAtlasTexture("Corrupted");
		_iconCurrency = GetDelveAtlasTexture("Currency");
		_iconDivine = GetDelveAtlasTexture("DivineOrb");
		_iconExalted = GetDelveAtlasTexture("ExaltedOrb");
		_iconDelveChest = GetDelveAtlasTexture("DelveChest");
		_iconDivinationCard = GetDelveAtlasTexture("DivinationCard");
		_iconDominationChest = GetDelveAtlasTexture("DominationChest");
		_iconEnchant = GetDelveAtlasTexture("Enchant");
		_iconEssence = GetDelveAtlasTexture("Essence");
		_iconFlare = GetDelveAtlasTexture("Flare");
		_iconFragment = GetDelveAtlasTexture("Fragment");
		_iconHarbingerChest = GetDelveAtlasTexture("HarbingerChest");
		_iconHighLevelGem = GetDelveAtlasTexture("HighLevelGem");
		_iconIncursionChest = GetDelveAtlasTexture("IncursionChest");
		_iconMap = GetDelveAtlasTexture("Map");
		_iconNemesisChest = GetDelveAtlasTexture("NemesisChest");
		_iconOfferingChest = GetDelveAtlasTexture("OfferingChest");
		_iconOnslaughtChest = GetDelveAtlasTexture("OnslaughtChest");
		_iconPaleCourtComplete = GetDelveAtlasTexture("PaleCourtComplete");
		_iconPerandusChest = GetDelveAtlasTexture("PerandusChest");
		_iconPerandusCoins = GetDelveAtlasTexture("PerandusCoins");
		_iconProphecy = GetDelveAtlasTexture("Prophecy");
		_iconRampageChest = GetDelveAtlasTexture("RampageChest");
		_iconRareAmulet = GetDelveAtlasTexture("RareAmulet");
		_iconResonatorT1 = GetDelveAtlasTexture("ResonatorT1");
		_iconResonatorT2 = GetDelveAtlasTexture("ResonatorT2");
		_iconResonatorT3 = GetDelveAtlasTexture("ResonatorT3");
		_iconSilverCoin = GetDelveAtlasTexture("SilverCoin");
		_iconSixLink = GetDelveAtlasTexture("SixLink");
		_iconSpeedArmour = GetDelveAtlasTexture("SpeedArmour");
		_iconStrongbox = GetDelveAtlasTexture("Strongbox");
		_iconStygianViseChest = GetDelveAtlasTexture("StygianViseChest");
		_iconTalismanChest = GetDelveAtlasTexture("TalismanChest");
		_iconTempestChest = GetDelveAtlasTexture("TempestChest");
		_iconTormentChest = GetDelveAtlasTexture("TormentChest");
		_iconUniqueManaFlask = GetDelveAtlasTexture("UniqueManaFlask");
		_iconUpgrade2x1A = GetDelveAtlasTexture("Upgrade2x1A");
		_iconUpgrade2x2A = GetDelveAtlasTexture("Upgrade2x2A");
		_iconWarbandsChest = GetDelveAtlasTexture("WarbandsChest");
		_iconWisdomCurrency = GetDelveAtlasTexture("WisdomCurrency");
	}

	private MapIcon GetMapIcon(DelveEntity e)
	{
		if (e == null)
		{
			return null;
		}
		if (base.Settings.DelvePathWays.Value && e.Path == "Metadata/Terrain/Leagues/Delve/Objects/DelveLight")
		{
			return new MapIcon(_iconAbyssCrack, base.Settings.DelvePathWaysNodeColor, base.Settings.DelvePathWaysNodeSize.Value);
		}
		if (base.Settings.DelveChests.Value && !e.IsOpened)
		{
			if (IsResonatorEventPath(e.Path))
			{
				if (!base.Settings.DelveResonatorChest.Value || !base.Settings.ResonatorShowEvent.Value) return null;
				return new MapIcon(_iconResonatorT1, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 1.0f);
			}
			if (e.Path.EndsWith("Encounter"))
			{
				return null;
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveMiningSuppliesDynamite") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteGeneric"))
			{
				if (!base.Settings.DelveMiningSuppliesDynamiteChest.Value) return null;
				return new MapIcon(_iconBombs, base.Settings.DelveMiningSuppliesDynamiteChestColor.Value, base.Settings.DelveMiningSuppliesDynamiteChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveMiningSuppliesFlares"))
			{
				if (!base.Settings.DelveMiningSuppliesFlaresChest.Value) return null;
				return new MapIcon(_iconFlare, base.Settings.DelveMiningSuppliesFlaresChestColor.Value, base.Settings.DelveMiningSuppliesFlaresChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathCurrency") || e.Path.StartsWith("Metadata/Chests/DelveChests/PathCurrency") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericCurrency"))
			{
				if (!base.Settings.DelveCurrencyChest.Value) return null;
				return new MapIcon(_iconCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericRandomEnchant"))
			{
				return new MapIcon(_iconEnchant, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmour6LinkedUniqueBody") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeapon6LinkedTwoHanded") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourFullyLinkedBody"))
			{
				return new MapIcon(_iconSixLink, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteCurrency") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCurrency"))
			{
				if (!base.Settings.DelveCurrencyChest.Value) return null;
				string text = e.Path.Replace("Metadata/Chests/DelveChests/", "");
				if (text.Contains("Currency1") || text == "DynamiteCurrency" || text.Contains("DelveChestCurrencyHighShards"))
				{
					if (!base.Settings.CurrencyShowTier1.Value) return null;
					return new MapIcon(_iconCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.CurrencyTier1Size.Value);
				}
				if (text.Contains("Currency2"))
				{
					if (!base.Settings.CurrencyShowTier2.Value) return null;
					return new MapIcon(_iconExalted ?? _iconCurrency, Color.White, base.Settings.CurrencyTier2Size.Value);
				}
				if (text.Contains("Currency3"))
				{
					if (!base.Settings.CurrencyShowTier3.Value) return null;
					return new MapIcon(_iconDivine ?? _iconCurrency, Color.White, base.Settings.CurrencyTier3Size.Value);
				}
				if (text.Contains("DelveChestCurrencySilverCoins"))
				{
					return new MapIcon(_iconSilverCoin, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyWisdomScrolls"))
				{
					return new MapIcon(_iconWisdomCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyDivination"))
				{
					return new MapIcon(_iconDivinationCard, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyMaps"))
				{
					return new MapIcon(_iconMap, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyVaal"))
				{
					return new MapIcon(_iconCorrupted, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencySockets"))
				{
					return new MapIcon(_iconAdditionalSockets, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourBody2AdditionalSockets"))
			{
				return new MapIcon(_iconAdditionalSockets, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericAtziriFragment"))
			{
				return new MapIcon(_iconFragment, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericPaleCourtFragment"))
			{
				return new MapIcon(_iconPaleCourtComplete, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericRandomEssence") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialGenericEssence"))
			{
				return new MapIcon(_iconEssence, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericDivination") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourDivination") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponDivination") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsDivination"))
			{
				return new MapIcon(_iconDivinationCard, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveAzuriteVein"))
			{
				if (!base.Settings.DelveAzuriteVeinChest.Value) return null;
				int num = base.Settings.DelveAzuriteVeinChestSize.Value;
				AtlasTexture texture = _iconAzurite;
				if (e.Path.EndsWith("1_1"))
				{
					if (!base.Settings.AzuriteShowLowTier.Value) return null;
					texture = _iconAzuriteT3;
				}
				else if (e.Path.EndsWith("1_2"))
				{
					if (!base.Settings.AzuriteShowLowTier.Value) return null;
					texture = _iconAzuriteT3;
				}
				else if (e.Path.EndsWith("1_3"))
				{
					texture = _iconAzuriteT2;
					num *= 2;
				}
				else if (e.Path.EndsWith("2_1"))
				{
					texture = _iconAzuriteT1;
					num *= 2;
				}
				return new MapIcon(texture, num);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator3") || e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator4") || e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator5"))
			{
				if (!base.Settings.DelveResonatorChest.Value || !base.Settings.ResonatorShowTier3.Value) return null;
				return new MapIcon(_iconResonatorT1, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator2"))
			{
				if (!base.Settings.DelveResonatorChest.Value || !base.Settings.ResonatorShowTier2.Value) return null;
				return new MapIcon(_iconResonatorT2, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator1"))
			{
				if (!base.Settings.DelveResonatorChest.Value || !base.Settings.ResonatorShowTier1.Value) return null;
				return new MapIcon(_iconResonatorT3, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourMovementSpeed"))
			{
				return new MapIcon(_iconSpeedArmour, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueMana"))
			{
				return new MapIcon(_iconUniqueManaFlask, base.Settings.DelveCurrencyChestColor.Value, (float)base.Settings.DelveCurrencyChestSize.Value * 1.3f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericRandomEnchant"))
			{
				return new MapIcon(_iconEnchant, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests") && (e.Path.EndsWith("FossilChest") || e.Path.EndsWith("FossilChestDynamite")))
			{
				if (!base.Settings.DelveFossilChest.Value) return null;
				string[] t = FossilList?.T1;
				if (t != null) foreach (string text2 in t)
				{
					if (e.Path.ToLower().Contains(text2.ToLower()))
					{
						return new MapIcon(_iconAbberantFossilT1, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
					}
				}
				string[] t2 = FossilList?.T2;
				if (t2 != null) foreach (string text3 in t2)
				{
					if (e.Path.ToLower().Contains(text3.ToLower()))
					{
						return new MapIcon(_iconAbberantFossilT2, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
					}
				}
				string[] t3 = FossilList?.T3;
				if (t3 != null) foreach (string text4 in t3)
				{
					if (e.Path.ToLower().Contains(text4.ToLower()))
					{
						return new MapIcon(_iconAbberantFossilT3, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
					}
				}
				return new MapIcon(_iconAbberantFossil, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalResonator") || e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator"))
			{
				if (!base.Settings.DelveResonatorChest.Value) return null;
				return new MapIcon(_iconResonatorT1, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestMap"))
			{
				return new MapIcon(_iconMap, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourCorrupted") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCorrupted") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsCorrupted") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalDoubleCorrupted"))
			{
				return new MapIcon(_iconCorrupted, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathGeneric") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathGeneric"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathArmour") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathArmour") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteArmour"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathWeapon") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathWeapon") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteWeapon"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathTrinkets") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathTrinkets") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericTrinkets") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteTrinkets"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconRareAmulet, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/ProsperoChest"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconPerandusCoins, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericAdditionalUniques") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourMultipleUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponMultipleUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsMultipleUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericAdditionalUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniquePhysical") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueFire") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueCold") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueLightning") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueChaos") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueMana"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericProphecyItem"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconProphecy, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericElderItem") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsElder") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourElder") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponElder"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericShaperItem") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponShaper") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsShaper") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourShaper"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericOffering"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconOfferingChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericDelveUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconDelveChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueOnslaught"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconOnslaughtChest, base.Settings.DelvePathwayChestSize.Value * 2);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueAnarchy"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconOnslaughtChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueAmbushInvasion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAmbushInvasionChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueDomination"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconDominationChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueNemesis"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconNemesisChest, base.Settings.DelvePathwayChestSize.Value * 2);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueRampage"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconRampageChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBeyond"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBeyondChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBloodlines"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBloodlinesChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueTorment"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconTormentChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueWarbands"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconWarbandsChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueTempest"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconTempestChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueTalisman"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconTalismanChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeaguePerandus"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconPerandusChest, base.Settings.DelvePathwayChestSize.Value * 2);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBreach"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBreachChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueHarbinger"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconHarbingerChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueAbyss"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBestiary"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBestiaryChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueIncursion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconIncursionChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourMultipleResists"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourLife"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourAtlas"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourOfCrafting") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsOfCrafting"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponPhysicalDamage"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCaster"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponExtraQuality"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCannotRollCaster"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCannotRollAttacker"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeapon30QualityUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsAmulet"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsRing"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsJewel"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsAtlas"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsEyesOfTheGreatwolf"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemGCP"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemHighQuality"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemHighLevel"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemHighLevelQuality"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemLevel4Special"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestMapChisels"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestMapCorrupted"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestAssortedFossils"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialArmourMinion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialTrinketsMinion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialGenericMinion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalImplicit"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalAtzoatlRare"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalUberAtziri"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalDoubleCorrupted"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssStygian"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStygianViseChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssJewels"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssJewelChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssHighJewel"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssJewelChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalAzurite"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAzuriteT1, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalFossils"))
			{
				return new MapIcon(_iconAbberantFossil, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalEmblem"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecial"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGem"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinkets"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeapon"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmour"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGeneric"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (base.Settings.DelvePathwayChest.Value && e.Path.StartsWith("Metadata/Chests/DelveChests") && !e.Path.Contains("Encounter"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
		}
		return null;
	}
}
