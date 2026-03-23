using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class AnimationConfigLoader
{
    private static Dictionary<string, AnimationConfig> _cache = new Dictionary<string, AnimationConfig>();
    
    public static AnimationConfig LoadFromFile(string characterId)
    {
        string cacheKey = characterId.ToLower();

        if (_cache.TryGetValue(cacheKey, out var cachedConfig))
        {
            return cachedConfig;
        }

        string path = Path.Combine(Application.streamingAssetsPath, "AnimationConfigs", $"{characterId}.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"[AnimationConfigLoader] Config file not found: {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<AnimationConfig>(json);

            _cache[cacheKey] = config;
            Debug.Log($"[AnimationConfigLoader] Loaded config: {characterId}");

            return config;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnimationConfigLoader] Failed to load config: {e.Message}");
            return null;
        }
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }
}
