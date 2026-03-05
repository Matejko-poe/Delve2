using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExileCore.Shared.AtlasHelper;
using Newtonsoft.Json;

namespace DefaultNamespace;

public class IconStore
{
	private readonly Dictionary<string, ChartFeature> _features;

	private readonly AtlasTexturesProcessor _textures;

	public IconStore(AtlasTexturesProcessor textures, string featureDescriptionPath)
	{
		_textures = textures;
		if (File.Exists(featureDescriptionPath))
		{
			_features = JsonConvert.DeserializeObject<List<ChartFeature>>(File.ReadAllText(featureDescriptionPath))
				.ToDictionary((ChartFeature x) => x.Id);
		}
		else
		{
			_features = new Dictionary<string, ChartFeature>();
		}
	}

	public AtlasTexture GetByFeatureId(string id)
	{
		if (!_features.TryGetValue(id, out var value) || string.IsNullOrEmpty(value.Image))
		{
			return null;
		}
		return _textures.GetTextureByName(value.Image.Replace("Art/2DArt/UIImages/InGame/Delve/MapEncounters/", null));
	}
}
