using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class JsonExporter : IConversationExporter
{
    public string FileExtension => ".json";

    public void Export(string filePath, ConversationSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[JsonExporter] Cannot export null snapshot");
            return;
        }

        try
        {
            var jsonData = new Dictionary<string, object>
            {
                { "sessionId", snapshot.SessionId },
                { "timestamp", snapshot.Timestamp.ToString("o") },
                { "provider", snapshot.Provider },
                { "model", snapshot.Model },
                { "systemPrompt", snapshot.SystemPrompt },
                { "eventType", snapshot.EventType.ToString() },
                { "messages", ConvertMessages(snapshot.Messages) },
                { "metadata", snapshot.Metadata ?? new Dictionary<string, object>() },
                { "statistics", CalculateStatistics(snapshot.Messages, snapshot.SystemPrompt) }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);

            Debug.Log($"[JsonExporter] Exported to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonExporter] Export failed: {ex.Message}");
        }
    }

    private List<Dictionary<string, object>> ConvertMessages(List<MessageSnapshot> messages)
    {
        var result = new List<Dictionary<string, object>>();
        if (messages == null) return result;

        foreach (var msg in messages)
        {
            result.Add(new Dictionary<string, object>
            {
                { "role", msg.Role },
                { "content", msg.Content },
                { "timestamp", msg.Timestamp.ToString("o") }
            });
        }
        return result;
    }

    private Dictionary<string, object> CalculateStatistics(List<MessageSnapshot> messages, string systemPrompt)
    {
        var stats = new Dictionary<string, object>
        {
            { "totalMessages", messages?.Count ?? 0 },
            { "userMessages", 0 },
            { "assistantMessages", 0 },
            { "systemMessages", 0 },
            { "hasSystemPrompt", !string.IsNullOrEmpty(systemPrompt) },
            { "systemPromptLength", systemPrompt?.Length ?? 0 }
        };

        if (messages == null) return stats;

        foreach (var msg in messages)
        {
            switch (msg.Role?.ToLower())
            {
                case "user":
                    stats["userMessages"] = (int)stats["userMessages"] + 1;
                    break;
                case "assistant":
                    stats["assistantMessages"] = (int)stats["assistantMessages"] + 1;
                    break;
                case "system":
                    stats["systemMessages"] = (int)stats["systemMessages"] + 1;
                    break;
            }
        }

        return stats;
    }
}
