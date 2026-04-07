using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var jsonData = BuildSnapshotData(snapshot);
            string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            Debug.Log($"[JsonExporter] Exported to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonExporter] Export failed: {ex.Message}");
        }
    }

    public void ExportAggregated(string filePath, AggregatedSession session)
    {
        if (session == null)
        {
            Debug.LogWarning("[JsonExporter] Cannot export null session");
            return;
        }

        try
        {
            var jsonData = BuildAggregatedSessionData(session);
            string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            Debug.Log($"[JsonExporter] Exported aggregated session to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JsonExporter] Export aggregated failed: {ex.Message}");
        }
    }

    private Dictionary<string, object> BuildSnapshotData(ConversationSnapshot snapshot)
    {
        var data = new Dictionary<string, object>
        {
            { "sessionId", snapshot.SessionId },
            { "timestamp", snapshot.Timestamp.ToString("o") },
            { "provider", snapshot.Provider },
            { "model", snapshot.Model },
            { "eventType", snapshot.EventType.ToString() }
        };

        if (!string.IsNullOrEmpty(snapshot.SystemPrompt))
        {
            data["systemPrompt"] = snapshot.SystemPrompt;
        }

        if (snapshot.Messages != null && snapshot.Messages.Count > 0)
        {
            data["messages"] = snapshot.Messages.Select(m => new Dictionary<string, object>
            {
                { "role", m.Role },
                { "content", m.Content },
                { "timestamp", m.Timestamp.ToString("o") }
            }).ToList();
        }

        if (snapshot.ToolCalls != null && snapshot.ToolCalls.Count > 0)
        {
            data["toolCalls"] = snapshot.ToolCalls.Select(tc => new Dictionary<string, object>
            {
                { "id", tc.Id },
                { "name", tc.Name },
                { "arguments", tc.Arguments }
            }).ToList();
        }

        if (snapshot.Statistics != null)
        {
            data["statistics"] = new Dictionary<string, object>
            {
                { "totalTokens", snapshot.Statistics.TotalTokens },
                { "promptTokens", snapshot.Statistics.PromptTokens },
                { "completionTokens", snapshot.Statistics.CompletionTokens }
            };
        }

        return data;
    }

    private Dictionary<string, object> BuildAggregatedSessionData(AggregatedSession session)
    {
        var data = new Dictionary<string, object>
        {
            { "sessionId", session.SessionId },
            { "startTime", session.StartTime.ToString("o") },
            { "endTime", session.EndTime.ToString("o") },
            { "provider", session.Provider },
            { "model", session.Model }
        };

        if (!string.IsNullOrEmpty(session.SystemPrompt))
        {
            data["systemPrompt"] = session.SystemPrompt;
        }

        if (!string.IsNullOrEmpty(session.RawRequestBody))
        {
            data["rawRequestBody"] = session.RawRequestBody;
        }

        if (session.Messages != null && session.Messages.Count > 0)
        {
            data["conversation"] = session.Messages.Select(m => new Dictionary<string, object>
            {
                { "role", m.Role },
                { "content", m.Content },
                { "timestamp", m.Timestamp.ToString("o") }
            }).ToList();
        }

        if (session.ToolCalls != null && session.ToolCalls.Count > 0)
        {
            data["toolCalls"] = session.ToolCalls.Select(tc => new Dictionary<string, object>
            {
                { "name", tc.Name },
                { "arguments", tc.Arguments },
                { "timestamp", tc.Timestamp.ToString("o") }
            }).ToList();
        }

        if (session.RequestContexts != null && session.RequestContexts.Count > 0)
        {
            data["requestContexts"] = session.RequestContexts.Select(rc => new Dictionary<string, object>
            {
                { "requestId", rc.RequestId },
                { "timestamp", rc.Timestamp.ToString("o") },
                { "messages", rc.Messages?.Select(m => new Dictionary<string, object>
                {
                    { "role", m.Role },
                    { "content", m.Content }
                }).ToList() ?? new List<Dictionary<string, object>>() },
                { "rawRequestBody", rc.RawRequestBody ?? "" },
                { "toolCalls", rc.ToolCalls?.Select(tc => new Dictionary<string, object>
                {
                    { "name", tc.Name },
                    { "arguments", tc.Arguments },
                    { "timestamp", tc.Timestamp.ToString("o") }
                }).ToList() ?? new List<Dictionary<string, object>>() },
                { "response", rc.Response != null ? new Dictionary<string, object>
                {
                    { "content", rc.Response.Content ?? "" },
                    { "success", rc.Response.Success },
                    { "timestamp", rc.Response.Timestamp.ToString("o") }
                } : null }
            }).ToList();
        }

        if (session.ScoringMessages != null && session.ScoringMessages.Count > 0)
        {
            data["scoringConversation"] = session.ScoringMessages.Select(m => new Dictionary<string, object>
            {
                { "role", m.Role },
                { "content", m.Content },
                { "timestamp", m.Timestamp.ToString("o") }
            }).ToList();
        }

        if (!string.IsNullOrEmpty(session.ScoringResult))
        {
            data["scoringResult"] = session.ScoringResult;
        }

        data["usage"] = new Dictionary<string, object>
        {
            { "totalTokens", session.TotalTokens },
            { "promptTokens", session.PromptTokens },
            { "completionTokens", session.CompletionTokens }
        };

        return data;
    }
}
