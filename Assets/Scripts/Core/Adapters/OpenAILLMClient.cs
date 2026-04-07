using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Obsolete("Use LlmClient with OpenAIAdapter instead. This class will be removed in a future version.")]
public class OpenAILLMClient : MonoBehaviour, ILlmClient
{
    private string _apiUrl;
    private string _apiKey;
    private string _model;
    private string _sessionId;
    private string _systemPrompt;
    private List<Dictionary<string, string>> _chatMessages;
    private bool _isRequestInProgress = false;
    private float _requestCooldown = 1.0f;
    private MonoBehaviour _owner;
    private bool _enableLogging = true;

    public string SessionId => _sessionId;
    public string ProviderName => "OpenAI";
    public string ModelName => _model;
    public bool HasTools => _tools.Count > 0;

    public event Action<string> OnMessageReceived;
    public event Action OnConversationUpdated;
    public event Action<ToolCallEventArgs> OnToolCalled;
    
    private List<ITool> _tools = new List<ITool>();

    public void SetOwner(MonoBehaviour owner)
    {
        _owner = owner;
    }

    public void Initialize(string systemPrompt, string model = null, bool enableLogging = true)
    {
        _enableLogging = enableLogging;
        
        var config = ApiConfig.Instance;
        _apiUrl = config.OpenAIChatUrl;
        _apiKey = config.OpenAIApiKey;
        _model = model ?? config.OpenAIModel;
        _systemPrompt = systemPrompt;
        _sessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        _chatMessages = new List<Dictionary<string, string>>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _chatMessages.Add(new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", systemPrompt }
            });
            if (_enableLogging)
            {
                TriggerSessionStart(systemPrompt);
            }
        }

        if (_enableLogging)
        {
            Debug.Log($"[OpenAILLMClient] Initialized with model: {_model}, sessionId: {_sessionId}");
        }
    }

    public void Initialize(LlmScene scene, string systemPrompt, bool enableLogging = true)
    {
        Initialize(systemPrompt, null, enableLogging);
    }

    public void SendChatMessage(string userMessage)
    {
        if (_isRequestInProgress || _owner == null)
        {
            Debug.LogWarning("[OpenAILLMClient] Request in progress or owner is null. Waiting...");
            return;
        }

        _chatMessages.Add(new Dictionary<string, string>
        {
            { "role", "user" },
            { "content", userMessage }
        });

        TriggerMessageSent(userMessage);

        _owner.StartCoroutine(SendRequest());
    }

    private IEnumerator SendRequest()
    {
        _isRequestInProgress = true;

        var requestBody = new
        {
            model = _model,
            messages = _chatMessages,
            temperature = 0.7f,
            max_tokens = 1500
        };

        string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {_apiKey}" }
        };

        using (var request = new UnityWebRequest(_apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[OpenAILLMClient] Request failed: {request.error}");
            }
            else
            {
                try
                {
                    var jsonResponse = JObject.Parse(request.downloadHandler.text);
                    var assistantMessage = jsonResponse["choices"][0]["message"]["content"].ToString();

                    _chatMessages.Add(new Dictionary<string, string>
                    {
                        { "role", "assistant" },
                        { "content", assistantMessage }
                    });

                    TriggerMessageReceived();

                    OnMessageReceived?.Invoke(assistantMessage);
                    OnConversationUpdated?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OpenAILLMClient] Error parsing response: {ex.Message}");
                }
            }
        }

        yield return new WaitForSeconds(_requestCooldown);
        _isRequestInProgress = false;
    }

    public void ClearHistory()
    {
        TriggerSessionEnd();

        if (_chatMessages != null && _chatMessages.Count > 0)
        {
            var systemMessage = _chatMessages[0];
            _chatMessages.Clear();
            _chatMessages.Add(systemMessage);
        }
    }

    public void SetSystemPrompt(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        if (_chatMessages != null && _chatMessages.Count > 0)
        {
            _chatMessages[0]["content"] = systemPrompt;
        }
    }

    public List<Dictionary<string, string>> GetChatHistory()
    {
        return _chatMessages;
    }
    
    public void RegisterTool(ITool tool)
    {
        if (tool == null) return;
        if (!_tools.Contains(tool))
        {
            _tools.Add(tool);
        }
    }
    
    public void UnregisterTool(string toolName)
    {
        _tools.RemoveAll(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }
    
    public IReadOnlyList<ITool> GetRegisteredTools()
    {
        return _tools.AsReadOnly();
    }

    private void TriggerSessionStart(string systemPrompt)
    {
        var snapshot = CreateSnapshot(ConversationEventType.SessionStart);
        snapshot.Messages = new List<MessageSnapshot>
        {
            new MessageSnapshot { Role = "system", Content = systemPrompt, Timestamp = DateTime.Now }
        };
        ConversationHook.TriggerSessionStart(snapshot);
    }

    private void TriggerMessageSent(string content)
    {
        var snapshot = CreateSnapshot(ConversationEventType.MessageSent);
        snapshot.Messages = new List<MessageSnapshot>
        {
            new MessageSnapshot { Role = "user", Content = content, Timestamp = DateTime.Now }
        };
        ConversationHook.TriggerMessageSent(snapshot);
    }

    private void TriggerMessageReceived()
    {
        var snapshot = CreateSnapshot(ConversationEventType.MessageReceived);
        snapshot.Messages = GetFullMessageHistory();
        ConversationHook.TriggerMessageReceived(snapshot);
    }

    private void TriggerSessionEnd()
    {
        var snapshot = CreateSnapshot(ConversationEventType.SessionEnd);
        snapshot.Messages = GetFullMessageHistory();
        ConversationHook.TriggerSessionEnd(snapshot);
    }

    private ConversationSnapshot CreateSnapshot(ConversationEventType eventType)
    {
        var scene = SceneManager.GetActiveScene();
        string sceneName = scene.IsValid() ? scene.name : "Unknown";
        
        return new ConversationSnapshot
        {
            SessionId = _sessionId,
            Provider = ProviderName,
            Model = ModelName ?? "unknown",
            SystemPrompt = _systemPrompt,
            Timestamp = DateTime.Now,
            EventType = eventType,
            Metadata = new Dictionary<string, object>
            {
                { "sceneName", sceneName }
            }
        };
    }

    private List<MessageSnapshot> GetFullMessageHistory()
    {
        if (_chatMessages == null) return new List<MessageSnapshot>();

        return _chatMessages.Select(m => new MessageSnapshot
        {
            Role = m.ContainsKey("role") ? m["role"] : "unknown",
            Content = m.ContainsKey("content") ? m["content"] : "",
            Timestamp = DateTime.Now
        }).ToList();
    }
}
