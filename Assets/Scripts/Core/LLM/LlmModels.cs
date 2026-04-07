using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class LlmRequest
{
    public string SessionId { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public List<LlmMessage> Messages { get; set; }
    public LlmScene Scene { get; set; }
    public int RetryCount { get; set; }
    public Action<string> OnSuccess { get; set; }
    public Action<string> OnError { get; set; }
    public JArray Tools { get; set; }
}

public class LlmResponse
{
    public string SessionId { get; set; }
    public string Content { get; set; }
    public LlmUsage Usage { get; set; }
    public bool Success { get; set; }
}

public class ToolCall
{
    public string Id { get; set; }
    public string Name { get; set; }
    public JObject Arguments { get; set; }
}

public class LlmToolCallEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public List<ToolCall> ToolCalls { get; set; }
}
