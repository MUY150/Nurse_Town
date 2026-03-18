using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LlmService : Singleton<LlmService>
{
    private Dictionary<string, ILlmAdapter> _adapters = new Dictionary<string, ILlmAdapter>();
    private Queue<LlmRequest> _requestQueue = new Queue<LlmRequest>();
    private bool _isProcessing = false;
    private int _maxConcurrentRequests = 3;
    
    public event Action<LlmResponseEvent> OnResponse;
    public event Action<LlmErrorEvent> OnError;
    
    protected override void Awake()
    {
        base.Awake();
        RegisterDefaultAdapters();
        
        var loggerInstance = ConversationLogger.Instance;
        Debug.Log($"[LlmService] ConversationLogger initialized: {loggerInstance != null}");
    }
    
    private void RegisterDefaultAdapters()
    {
        RegisterAdapter(new DeepSeekAdapter());
        RegisterAdapter(new OpenAIAdapter());
        RegisterAdapter(new QwenAdapter());
        
        Debug.Log($"[LlmService] Registered {_adapters.Count} adapters: {string.Join(", ", _adapters.Keys)}");
    }
    
    public void RegisterAdapter(ILlmAdapter adapter)
    {
        if (adapter == null || string.IsNullOrEmpty(adapter.ProviderName))
        {
            Debug.LogWarning("[LlmService] Cannot register adapter with null or empty name");
            return;
        }
        
        string key = adapter.ProviderName.ToLower();
        if (_adapters.ContainsKey(key))
        {
            Debug.LogWarning($"[LlmService] Adapter '{adapter.ProviderName}' already registered, replacing");
        }
        
        _adapters[key] = adapter;
        Debug.Log($"[LlmService] Registered adapter: {adapter.ProviderName}");
    }
    
    public void UnregisterAdapter(string providerName)
    {
        if (string.IsNullOrEmpty(providerName)) return;
        
        string key = providerName.ToLower();
        if (_adapters.Remove(key))
        {
            Debug.Log($"[LlmService] Unregistered adapter: {providerName}");
        }
    }
    
    public ILlmAdapter GetAdapter(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            return null;
        }
        
        string key = providerName.ToLower();
        if (_adapters.TryGetValue(key, out ILlmAdapter adapter))
        {
            return adapter;
        }
        
        Debug.LogWarning($"[LlmService] Adapter '{providerName}' not found");
        return null;
    }
    
    public IEnumerable<string> GetAvailableProviders()
    {
        return _adapters.Keys;
    }
    
    public void SendRequest(LlmRequest request)
    {
        if (request == null)
        {
            Debug.LogError("[LlmService] Cannot send null request");
            return;
        }
        
        _requestQueue.Enqueue(request);
        ProcessQueue();
    }
    
    private void ProcessQueue()
    {
        if (_isProcessing) return;
        
        int processed = 0;
        while (_requestQueue.Count > 0 && processed < _maxConcurrentRequests)
        {
            var request = _requestQueue.Dequeue();
            if (request == null) continue;
            
            processed++;
            StartCoroutine(SendRequestCoroutine(request));
        }
        
        if (processed == 0 && _requestQueue.Count == 0)
        {
            _isProcessing = false;
        }
    }
    
    private System.Collections.IEnumerator SendRequestCoroutine(LlmRequest request)
    {
        var adapter = GetAdapter(request.Provider);
        if (adapter == null)
        {
            var error = new LlmErrorEvent
            {
                SessionId = request.SessionId,
                Timestamp = DateTime.Now,
                Provider = request.Provider,
                ErrorMessage = $"No adapter found for provider '{request.Provider}'",
                RetryCount = request.RetryCount,
                WillRetry = false
            };
            LlmEventBus.Publish(error);
            OnError?.Invoke(error);
            request.OnError?.Invoke(error.ErrorMessage);
            yield break;
        }
        
        var config = LlmConfig.Instance.GetProviderConfig(request.Provider);
        if (config == null)
        {
            var error = new LlmErrorEvent
            {
                SessionId = request.SessionId,
                Timestamp = DateTime.Now,
                Provider = request.Provider,
                ErrorMessage = $"No config found for provider '{request.Provider}'",
                RetryCount = request.RetryCount,
                WillRetry = false
            };
            LlmEventBus.Publish(error);
            OnError?.Invoke(error);
            request.OnError?.Invoke(error.ErrorMessage);
            yield break;
        }
        
        string apiKey = LlmConfig.Instance.GetApiKey(request.Provider);
        if (string.IsNullOrEmpty(apiKey))
        {
            var error = new LlmErrorEvent
            {
                SessionId = request.SessionId,
                Timestamp = DateTime.Now,
                Provider = request.Provider,
                ErrorMessage = $"No API key found for provider '{request.Provider}'",
                RetryCount = request.RetryCount,
                WillRetry = false
            };
            LlmEventBus.Publish(error);
            OnError?.Invoke(error);
            request.OnError?.Invoke(error.ErrorMessage);
            yield break;
        }
        
        string requestBody = adapter.BuildRequestBody(
            request.Messages, 
            request.Model, 
            config.temperature, 
            config.maxTokens,
            request.Tools
        );
        
        var requestEvent = new LlmRequestEvent
        {
            SessionId = request.SessionId,
            Timestamp = DateTime.Now,
            Provider = request.Provider,
            Model = request.Model,
            Messages = new List<LlmMessage>(request.Messages),
            Scene = request.Scene,
            RawRequestBody = requestBody
        };
        LlmEventBus.Publish(requestEvent);
        
        string url = adapter.GetApiUrl();
        var headers = adapter.GetHeaders(apiKey);
        
        using (var webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            foreach (var header in headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            var timeout = config.timeout > 0 ? config.timeout : 30;
            webRequest.timeout = timeout;
            
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = webRequest.error;
                bool willRetry = request.RetryCount < config.maxRetries;
                
                if (willRetry)
                {
                    request.RetryCount++;
                    _requestQueue.Enqueue(request);
                    Debug.LogWarning($"[LlmService] Request failed, will retry ({request.RetryCount}/{config.maxRetries}): {errorMsg}");
                }
                
                var error = new LlmErrorEvent
                {
                    SessionId = request.SessionId,
                    Timestamp = DateTime.Now,
                    Provider = request.Provider,
                    ErrorMessage = errorMsg,
                    RetryCount = request.RetryCount,
                    WillRetry = willRetry
                };
                LlmEventBus.Publish(error);
                OnError?.Invoke(error);
                request.OnError?.Invoke(errorMsg);
            }
            else
            {
                string responseContent = webRequest.downloadHandler.text;
                
                try
                {
                    var toolCalls = adapter.ParseToolCalls(responseContent);
                    string content = adapter.ParseResponse(responseContent);
                    var usage = adapter.ParseUsage(responseContent);
                    
                    if (toolCalls != null && toolCalls.Count > 0)
                    {
                        var toolCallEvent = new LlmToolCallEvent
                        {
                            SessionId = request.SessionId,
                            Timestamp = DateTime.Now,
                            Provider = request.Provider,
                            Model = request.Model,
                            ToolCalls = toolCalls
                        };
                        LlmEventBus.Publish(toolCallEvent);
                        
                        foreach (var toolCall in toolCalls)
                        {
                            var result = ToolRegistry.Instance?.ExecuteTool(toolCall.Name, toolCall.Arguments);
                            Debug.Log($"[LlmService] Tool '{toolCall.Name}' executed: {result?.Success}");
                        }
                        
                        var toolResponseEvent = new LlmResponseEvent
                        {
                            SessionId = request.SessionId,
                            Timestamp = DateTime.Now,
                            Provider = request.Provider,
                            Model = request.Model,
                            Content = content,
                            Usage = usage,
                            Success = true
                        };
                        
                        LlmEventBus.Publish(toolResponseEvent);
                        request.OnSuccess?.Invoke(content);
                        OnResponse?.Invoke(toolResponseEvent);
                        yield break;
                    }
                    
                    var responseEvent = new LlmResponseEvent
                    {
                        SessionId = request.SessionId,
                        Timestamp = DateTime.Now,
                        Provider = request.Provider,
                        Model = request.Model,
                        Content = content,
                        Usage = usage,
                        Success = true
                    };
                    
                    LlmEventBus.Publish(responseEvent);
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        request.OnSuccess?.Invoke(content);
                    }
                    OnResponse?.Invoke(responseEvent);
                }
                catch (Exception e)
                {
                    var error = new LlmErrorEvent
                    {
                        SessionId = request.SessionId,
                        Timestamp = DateTime.Now,
                        Provider = request.Provider,
                        ErrorMessage = $"Exception: {e.Message}",
                        RetryCount = request.RetryCount,
                        WillRetry = false
                    };
                    LlmEventBus.Publish(error);
                    OnError?.Invoke(error);
                    request.OnError?.Invoke(e.Message);
                }
            }
        }
    }
    
    protected override void OnDestroy()
    {
        _requestQueue.Clear();
        _adapters.Clear();
        OnResponse = null;
        OnError = null;
    }
}
