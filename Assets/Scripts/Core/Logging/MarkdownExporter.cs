using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class MarkdownExporter : IConversationExporter
{
    public string FileExtension => ".md";

    private bool _includeSystemPrompt;

    public MarkdownExporter(bool includeSystemPrompt = false)
    {
        _includeSystemPrompt = includeSystemPrompt;
    }

    public void Export(string filePath, ConversationSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[MarkdownExporter] Cannot export null snapshot");
            return;
        }

        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("# 对话记录");
            sb.AppendLine();
            sb.AppendLine($"**会话ID**: {snapshot.SessionId}");
            sb.AppendLine($"**时间**: {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**模型**: {snapshot.Provider} ({snapshot.Model})");
            sb.AppendLine();

            if (snapshot.Metadata != null && snapshot.Metadata.Count > 0)
            {
                sb.AppendLine("## 元数据");
                foreach (var kvp in snapshot.Metadata)
                {
                    sb.AppendLine($"- **{kvp.Key}**: {kvp.Value}");
                }
                sb.AppendLine();
            }

            if (_includeSystemPrompt && !string.IsNullOrEmpty(snapshot.SystemPrompt))
            {
                sb.AppendLine("## 系统提示词");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(snapshot.SystemPrompt);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 对话内容");
            sb.AppendLine();

            if (snapshot.Messages != null)
            {
                foreach (var msg in snapshot.Messages)
                {
                    if (!_includeSystemPrompt && msg.Role?.ToLower() == "system")
                    {
                        continue;
                    }

                    string roleDisplay = GetRoleDisplayName(msg.Role);
                    string content = EscapeMarkdown(msg.Content);

                    sb.AppendLine($"**{roleDisplay}**: {content}");
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            sb.AppendLine(GenerateStatistics(snapshot.Messages, snapshot.SystemPrompt));

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[MarkdownExporter] Exported to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MarkdownExporter] Export failed: {ex.Message}");
        }
    }

    private string GetRoleDisplayName(string role)
    {
        switch (role?.ToLower())
        {
            case "user":
                return "用户";
            case "assistant":
                return "助手";
            case "system":
                return "系统";
            default:
                return role ?? "未知";
        }
    }

    private string EscapeMarkdown(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";

        return content
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("#", "\\#")
            .Replace("[", "\\[")
            .Replace("]", "\\]");
    }

    private string GenerateStatistics(List<MessageSnapshot> messages, string systemPrompt)
    {
        var sb = new StringBuilder();
        
        if (messages == null || messages.Count == 0)
        {
            sb.AppendLine("*无消息记录*");
        }
        else
        {
            int userCount = 0;
            int assistantCount = 0;

            foreach (var msg in messages)
            {
                switch (msg.Role?.ToLower())
                {
                    case "user":
                        userCount++;
                        break;
                    case "assistant":
                        assistantCount++;
                        break;
                }
            }

            sb.AppendLine($"*共 {messages.Count} 条消息 | 用户: {userCount} 条 | 助手: {assistantCount} 条*");
        }
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            sb.AppendLine($"*系统提示词长度: {systemPrompt.Length} 字符*");
        }

        return sb.ToString();
    }
}
