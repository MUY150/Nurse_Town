using System;
using System.Collections.Generic;
using UnityEngine;

public class LlmClient : ILlmClient
{
    private string _sessionId;
    private LlmScene _scene;
    private string _systemPrompt;
    private List<LlmMessage> _messages;
    private string _provider;
    private string _model;
    private bool _enableLogging = true;

    public string SessionId => _sessionId;
    public string ProviderName => _provider;
    public string ModelName => _model;

    public event Action<string> OnMessageReceived;
    public event Action OnConversationUpdated;

    public LlmClient() { }

    public LlmClient(LlmScene scene, string systemPrompt = null, bool enableLogging = true)
    {
        Initialize(scene, systemPrompt, enableLogging);
    }

    public void Initialize(string systemPrompt, string model = null, bool enableLogging = true)
    {
        Initialize(LlmScene.Custom, systemPrompt, enableLogging);
    }

    public void Initialize(LlmScene scene, string systemPrompt, bool enableLogging = true)
    {
        _scene = scene;
        _systemPrompt = systemPrompt;
        _enableLogging = enableLogging;
        _sessionId = $"session_{System.DateTime.Now:yyyyMMdd_HHmmss}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
        _messages = new List<LlmMessage>();
        
        _provider = LlmConfig.Instance.GetProviderForScene(scene);
        var providerConfig = LlmConfig.Instance.GetProviderConfig(_provider);
        _model = providerConfig?.defaultModel ?? "unknown";
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _messages.Add(new LlmMessage("system", systemPrompt));
        }
        
        if (_enableLogging)
        {
            var startEvent = new SessionStartEvent
            {
                SessionId = _sessionId,
                Timestamp = System.DateTime.Now,
                Scene = _scene,
                SystemPrompt = _systemPrompt,
                Provider = _provider
            };
            LlmEventBus.Publish(startEvent);
            
            Debug.Log($"[LlmClient] Initialized: scene={_scene}, provider={_provider}, model={_model}, sessionId={_sessionId}");
        }
    }

    public void SendChatMessage(string userMessage)
    {
        if (string.IsNullOrEmpty(userMessage))
        {
            Debug.LogWarning("[LlmClient] Cannot send empty message");
            return;
        }
        
        _messages.Add(new LlmMessage("user", userMessage));
        
        var request = new LlmRequest
        {
            SessionId = _sessionId,
            Provider = _provider,
            Model = _model,
            Messages = new List<LlmMessage>(_messages),
            Scene = _scene,
            OnSuccess = HandleSuccess,
            OnError = HandleError,
            RetryCount = 0
        };
        
        LlmService.Instance.SendRequest(request);
    }

    private void HandleSuccess(string response)
    {
        _messages.Add(new LlmMessage("assistant", response));

        OnMessageReceived?.Invoke(response);
        OnConversationUpdated?.Invoke();
    }

    private void HandleError(string error)
    {
        Debug.LogError($"[LlmClient] Error: {error}");
    }

    public void ClearHistory()
    {
        if (_enableLogging)
        {
            var endEvent = new SessionEndEvent
            {
                SessionId = _sessionId,
                Timestamp = System.DateTime.Now,
                Scene = _scene,
                TotalMessages = _messages.Count,
                TotalTokens = 0
            };
            LlmEventBus.Publish(endEvent);
        }

        if (_messages != null && _messages.Count > 0)
        {
            var systemMessage = _messages[0];
            _messages.Clear();
            _messages.Add(systemMessage);
        }

        Debug.Log($"[LlmClient] History cleared for session {_sessionId}");
    }

    public void SetSystemPrompt(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        if (_messages != null && _messages.Count > 0)
        {
            _messages[0] = new LlmMessage("system", systemPrompt);
        }
    }

    public List<Dictionary<string, string>> GetChatHistory()
    {
        var result = new List<Dictionary<string, string>>();
        foreach (var msg in _messages)
        {
            result.Add(new Dictionary<string, string>
            {
                { "role", msg.Role },
                { "content", msg.Content }
            });
        }
        return result;
    }

    public List<LlmMessage> GetMessages()
    {
        return new List<LlmMessage>(_messages);
    }
}
