using System;
using System.Collections.Generic;

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
}

public class LlmResponse
{
    public string SessionId { get; set; }
    public string Content { get; set; }
    public LlmUsage Usage { get; set; }
    public bool Success { get; set; }
}
