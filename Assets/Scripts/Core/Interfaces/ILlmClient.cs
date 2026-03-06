using System;
using System.Collections.Generic;

public interface ILlmClient
{
    string SessionId { get; }
    string ProviderName { get; }
    string ModelName { get; }

    void Initialize(string systemPrompt, string model = null);
    void SendChatMessage(string userMessage);
    void ClearHistory();
    void SetSystemPrompt(string systemPrompt);
    List<Dictionary<string, string>> GetChatHistory();

    event Action<string> OnMessageReceived;
    event Action OnConversationUpdated;
}
