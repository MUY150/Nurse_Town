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
}
