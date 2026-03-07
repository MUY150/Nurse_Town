using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class OpenAIAdapter : ILlmAdapter
{
    public string ProviderName => "OpenAI";
    
    public string GetApiUrl()
    {
        return "https://api.openai.com/v1/chat/completions";
    }
    
    public Dictionary<string, string> GetHeaders(string apiKey)
    {
        return new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {apiKey}" }
        };
    }
    
    public string BuildRequestBody(List<LlmMessage> messages, string model, float temperature, int maxTokens)
    {
        var messageList = messages.Select(m => new { role = m.Role, content = m.Content }).ToList();
        
        var requestBody = new
        {
            model = model,
            messages = messageList,
            temperature = temperature,
            max_tokens = maxTokens
        };
        
        return JsonConvert.SerializeObject(requestBody);
    }
    
    public string ParseResponse(string jsonResponse)
    {
        try
        {
            var response = JObject.Parse(jsonResponse);
            var choice = response["choices"]?[0];
            var content = choice?["message"]?["content"]?.ToString();
            return content ?? string.Empty;
        }
        catch (Exception e)
        {
            Debug.LogError($"[OpenAIAdapter] Failed to parse response: {e.Message}");
            return string.Empty;
        }
    }
    
    public LlmUsage ParseUsage(string jsonResponse)
    {
        try
        {
            var response = JObject.Parse(jsonResponse);
            var usage = response["usage"];
            
            return new LlmUsage
            {
                PromptTokens = usage?["prompt_tokens"]?.Value<int>() ?? 0,
                CompletionTokens = usage?["completion_tokens"]?.Value<int>() ?? 0,
                TotalTokens = usage?["total_tokens"]?.Value<int>() ?? 0
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[OpenAIAdapter] Failed to parse usage: {e.Message}");
            return new LlmUsage
            {
                PromptTokens = 0,
                CompletionTokens = 0,
                TotalTokens = 0
            };
        }
    }
}
