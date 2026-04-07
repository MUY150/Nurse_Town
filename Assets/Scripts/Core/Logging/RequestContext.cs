using System;
using System.Collections.Generic;

[Serializable]
public class RequestContext
{
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<LlmMessage> Messages { get; set; }
    public string RawRequestBody { get; set; }
    public List<ToolCallRecord> ToolCalls { get; set; }
    public LlmResponseEvent Response { get; set; }

    public RequestContext()
    {
        RequestId = Guid.NewGuid().ToString();
        Timestamp = DateTime.Now;
        Messages = new List<LlmMessage>();
        ToolCalls = new List<ToolCallRecord>();
    }

    public RequestContext(string requestId, DateTime timestamp, List<LlmMessage> messages, string rawRequestBody)
    {
        RequestId = requestId;
        Timestamp = timestamp;
        Messages = messages ?? new List<LlmMessage>();
        RawRequestBody = rawRequestBody;
        ToolCalls = new List<ToolCallRecord>();
    }
}