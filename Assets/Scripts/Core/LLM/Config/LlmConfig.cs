using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class LlmConfigData
{
    public string defaultProvider = "deepseek";
    public Dictionary<string, ProviderConfig> providers = new Dictionary<string, ProviderConfig>();
    public Dictionary<string, string> sceneProviders = new Dictionary<string, string>();
}

[Serializable]
public class ProviderConfig
{
    public string apiKeyEnv;
    public string apiUrl;
    public string defaultModel;
    public float temperature = 0.7f;
    public int maxTokens = 1500;
    public int timeout = 30;
    public int maxRetries = 3;
}

public class LlmConfig : Singleton<LlmConfig>
{
    private LlmConfigData _data;
    private FileSystemWatcher _fileWatcher;
    private string _configPath;
    
    public event Action OnConfigChanged;
    
    public LlmConfigData Data => _data;
    
    protected override void Awake()
    {
        base.Awake();
        LoadConfig();
        SetupFileWatcher();
    }
    
    private void LoadConfig()
    {
        _configPath = Path.Combine(Application.dataPath, "Scripts/Core/LLM/Config/llm_config.json");
        
        if (File.Exists(_configPath))
        {
            try
            {
                string json = File.ReadAllText(_configPath);
                _data = JsonConvert.DeserializeObject<LlmConfigData>(json);
                Debug.Log($"[LlmConfig] Loaded config from {_configPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LlmConfig] Failed to load config: {e.Message}");
                _data = GetDefaultConfig();
            }
        }
        else
        {
            Debug.LogWarning($"[LlmConfig] Config file not found at {_configPath}, using defaults");
            _data = GetDefaultConfig();
            SaveConfig();
        }
    }
    
    private void SaveConfig()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
            File.WriteAllText(_configPath, json);
            Debug.Log($"[LlmConfig] Saved config to {_configPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LlmConfig] Failed to save config: {e.Message}");
        }
    }
    
    private void SetupFileWatcher()
    {
        if (!File.Exists(_configPath)) return;
        
        string directory = Path.GetDirectoryName(_configPath);
        string fileName = Path.GetFileName(_configPath);
        
        _fileWatcher = new FileSystemWatcher(directory, fileName);
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
        
        Debug.Log($"[LlmConfig] Watching for changes in {_configPath}");
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"[LlmConfig] Config file changed, reloading...");
        
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            LoadConfig();
            OnConfigChanged?.Invoke();
        });
    }
    
    public string GetProviderForScene(LlmScene scene)
    {
        string sceneName = scene.ToString();
        
        if (_data.sceneProviders != null && _data.sceneProviders.TryGetValue(sceneName, out string provider))
        {
            return provider;
        }
        
        return _data.defaultProvider ?? "deepseek";
    }
    
    public ProviderConfig GetProviderConfig(string providerName)
    {
        if (_data.providers != null && _data.providers.TryGetValue(providerName.ToLower(), out ProviderConfig config))
        {
            return config;
        }
        
        Debug.LogWarning($"[LlmConfig] Provider config not found for '{providerName}', using default");
        return new ProviderConfig
        {
            apiKeyEnv = $"{providerName.ToUpper()}_API_KEY",
            apiUrl = $"https://api.{providerName.ToLower()}.com/v1/chat/completions",
            defaultModel = "default"
        };
    }
    
    public string GetApiKey(string providerName)
    {
        var config = GetProviderConfig(providerName);
        return EnvironmentLoader.GetEnvVariable(config.apiKeyEnv);
    }
    
    private LlmConfigData GetDefaultConfig()
    {
        return new LlmConfigData
        {
            defaultProvider = "deepseek",
            providers = new Dictionary<string, ProviderConfig>
            {
                {
                    "deepseek", new ProviderConfig
                    {
                        apiKeyEnv = "DEEPSEEK_API_KEY",
                        apiUrl = "https://api.deepseek.com/v1/chat/completions",
                        defaultModel = "deepseek-chat",
                        temperature = 0.7f,
                        maxTokens = 1500
                    }
                },
                {
                    "openai", new ProviderConfig
                    {
                        apiKeyEnv = "OPENAI_API_KEY",
                        apiUrl = "https://api.openai.com/v1/chat/completions",
                        defaultModel = "gpt-4-turbo",
                        temperature = 0.7f,
                        maxTokens = 1500
                    }
                }
            },
            sceneProviders = new Dictionary<string, string>
            {
                { "Patient", "deepseek" },
                { "Nurse", "openai" },
                { "Evaluation", "deepseek" }
            }
        };
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_fileWatcher != null)
        {
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Dispose();
        }
    }
}
