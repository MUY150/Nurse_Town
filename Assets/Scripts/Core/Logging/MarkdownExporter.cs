using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class MarkdownExporter : IConversationExporter
{
    public string FileExtension => ".md";

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

            sb.AppendLine($"# 对话记录");
            sb.AppendLine($"时间: {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss} | 模型: {snapshot.Provider}/{snapshot.Model}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            if (snapshot.Messages != null)
            {
                foreach (var msg in snapshot.Messages)
                {
                    string roleDisplay = GetRoleDisplayName(msg.Role);
                    sb.AppendLine($"**{roleDisplay}**:");
                    sb.AppendLine();
                    sb.AppendLine(msg.Content);
                    sb.AppendLine();
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[MarkdownExporter] Exported to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MarkdownExporter] Export failed: {ex.Message}");
        }
    }

    public void ExportAggregated(string filePath, AggregatedSession session)
    {
        if (session == null)
        {
            Debug.LogWarning("[MarkdownExporter] Cannot export null session");
            return;
        }

        try
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# 对话记录");
            sb.AppendLine();
            sb.AppendLine($"**Session**: {session.SessionId}");
            sb.AppendLine($"**时间**: {session.StartTime:yyyy-MM-dd HH:mm:ss} - {session.EndTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**模型**: {session.Provider}/{session.Model}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(session.SystemPrompt))
            {
                sb.AppendLine("## 系统提示");
                sb.AppendLine();
                sb.AppendLine(session.SystemPrompt);
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            if (session.Messages != null && session.Messages.Count > 0)
            {
                sb.AppendLine("## 对话内容");
                sb.AppendLine();
                foreach (var msg in session.Messages)
                {
                    string roleDisplay = GetRoleDisplayName(msg.Role);
                    sb.AppendLine($"**{roleDisplay}** ({msg.Timestamp:HH:mm:ss}):");
                    sb.AppendLine();
                    sb.AppendLine(msg.Content);
                    sb.AppendLine();
                }
                sb.AppendLine("---");
                sb.AppendLine();
            }

            if (session.ToolCalls != null && session.ToolCalls.Count > 0)
            {
                sb.AppendLine("## 工具调用");
                sb.AppendLine();
                foreach (var tc in session.ToolCalls)
                {
                    sb.AppendLine($"- **{tc.Name}** ({tc.Timestamp:HH:mm:ss}): {tc.Arguments}");
                }
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            if (session.ScoringMessages != null && session.ScoringMessages.Count > 0)
            {
                sb.AppendLine("## 评分对话");
                sb.AppendLine();
                foreach (var msg in session.ScoringMessages)
                {
                    string roleDisplay = GetRoleDisplayName(msg.Role);
                    sb.AppendLine($"**{roleDisplay}** ({msg.Timestamp:HH:mm:ss}):");
                    sb.AppendLine();
                    sb.AppendLine(msg.Content);
                    sb.AppendLine();
                }
                sb.AppendLine("---");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(session.ScoringResult))
            {
                sb.AppendLine("## 评分结果");
                sb.AppendLine();
                sb.AppendLine(session.ScoringResult);
                sb.AppendLine();
            }

            sb.AppendLine("## 使用统计");
            sb.AppendLine();
            sb.AppendLine($"- 总Token: {session.TotalTokens}");
            sb.AppendLine($"- 提示Token: {session.PromptTokens}");
            sb.AppendLine($"- 完成Token: {session.CompletionTokens}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[MarkdownExporter] Exported aggregated session to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MarkdownExporter] Export aggregated failed: {ex.Message}");
        }
    }

    private string GetRoleDisplayName(string role)
    {
        return role?.ToLower() switch
        {
            "user" => "用户",
            "assistant" => "助手",
            "system" => "系统",
            _ => role ?? "未知"
        };
    }
}
