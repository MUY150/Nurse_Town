using System;
using System.Collections.Generic;

[Serializable]
public class ConversationSnapshot
{
    public string SessionId { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public string SystemPrompt { get; set; }
    public List<MessageSnapshot> Messages { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public DateTime Timestamp { get; set; }
    public ConversationEventType EventType { get; set; }
    public ConversationStatistics Statistics { get; set; }
    public string RawRequestJson { get; set; }
    public List<ToolCallSnapshot> ToolCalls { get; set; }

    public ConversationSnapshot()
    {
        Messages = new List<MessageSnapshot>();
        Metadata = new Dictionary<string, object>();
        Statistics = new ConversationStatistics();
        Timestamp = DateTime.Now;
        ToolCalls = new List<ToolCallSnapshot>();
    }

    public ConversationSnapshot(string sessionId, string provider, string model)
    {
        SessionId = sessionId;
        Provider = provider;
        Model = model;
        Messages = new List<MessageSnapshot>();
        Metadata = new Dictionary<string, object>();
        Statistics = new ConversationStatistics();
        Timestamp = DateTime.Now;
        ToolCalls = new List<ToolCallSnapshot>();
    }
}

[Serializable]
public class ConversationStatistics
{
    public int TotalMessages { get; set; }
    public int UserMessages { get; set; }
    public int AssistantMessages { get; set; }
    public int SystemMessages { get; set; }
    public int TotalTokens { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}
