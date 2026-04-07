using System;
using System.Collections.Generic;

public interface ILlmClient
{
    string SessionId { get; }
    string ProviderName { get; }
    string ModelName { get; }
    
    void Initialize(string systemPrompt, string model = null, bool enableLogging = true);
    void Initialize(LlmScene scene, string systemPrompt, bool enableLogging = true);
    void SendChatMessage(string userMessage);
    void ClearHistory();
    void SetSystemPrompt(string systemPrompt);
    List<Dictionary<string, string>> GetChatHistory();
    
    event Action<string> OnMessageReceived;
    event Action OnConversationUpdated;
    
    void RegisterTool(ITool tool);
    void UnregisterTool(string toolName);
    IReadOnlyList<ITool> GetRegisteredTools();
    bool HasTools { get; }
    
    event Action<ToolCallEventArgs> OnToolCalled;
}
