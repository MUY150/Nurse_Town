using System.Collections.Generic;

public interface ILlmAdapter
{
    string ProviderName { get; }
    string GetApiUrl();
    Dictionary<string, string> GetHeaders(string apiKey);
    string BuildRequestBody(List<LlmMessage> messages, string model, float temperature, int maxTokens);
    string ParseResponse(string jsonResponse);
    LlmUsage ParseUsage(string jsonResponse);
}

public class LlmMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    
    public LlmMessage() { }
    
    public LlmMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

public class LlmUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
