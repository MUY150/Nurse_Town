using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ConversationLogger : MonoBehaviour
{
    private static ConversationLogger _instance;
    public static ConversationLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[ConversationLogger]");
                _instance = go.AddComponent<ConversationLogger>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("基础配置")]
    public bool enableLogging = true;
    public bool enableJson = true;
    public bool enableMarkdown = true;

    [Header("保存路径配置")]
    public LogSaveLocation saveLocation = LogSaveLocation.CustomPath;
    public string customLogPath = @"d:\UsefulDIR\tempcoding\newtry\temp_conversation_log";
    public string logFolderName = "ConversationLogs";

    private bool _isInitialized = false;
    private List<IConversationExporter> _exporters;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        _exporters = new List<IConversationExporter>();
        if (enableJson)
            _exporters.Add(new JsonExporter());
        if (enableMarkdown)
            _exporters.Add(new MarkdownExporter());

        LlmEventBus.OnSessionStart += HandleSessionStart;
        LlmEventBus.OnRequest += HandleRequest;
        LlmEventBus.OnResponse += HandleResponse;
        LlmEventBus.OnToolCall += HandleToolCall;

        SessionAggregator.Instance.OnSessionComplete += HandleSessionComplete;

        _isInitialized = true;
        Debug.Log($"[ConversationLogger] Initialized with SessionAggregator");
    }

    private void HandleSessionStart(SessionStartEvent e)
    {
        if (!enableLogging) return;

        SessionAggregator.Instance.StartMainSession(
            e.SessionId, 
            e.Provider ?? "deepseek", 
            "", 
            e.SystemPrompt ?? "",
            e.Scene
        );
    }

    private void HandleRequest(LlmRequestEvent e)
    {
        if (!enableLogging) return;

        SessionAggregator.Instance.StartRequest(e.SessionId, e.Messages, e.RawRequestBody);

        if (e.Messages != null)
        {
            foreach (var msg in e.Messages)
            {
                if (msg.Role?.ToLower() == "user")
                {
                    SessionAggregator.Instance.AddMessage(e.SessionId, "user", msg.Content);
                }
            }
        }

        if (!string.IsNullOrEmpty(e.RawRequestBody))
        {
            SessionAggregator.Instance.SetRawRequestBody(e.SessionId, e.RawRequestBody);
        }
    }

    private void HandleResponse(LlmResponseEvent e)
    {
        if (!enableLogging) return;

        SessionAggregator.Instance.AddMessage(e.SessionId, "assistant", e.Content);
        SessionAggregator.Instance.CompleteRequest(e.SessionId, e);
        
        if (e.Usage != null)
        {
            SessionAggregator.Instance.SetUsage(
                e.SessionId, 
                e.Usage.TotalTokens, 
                e.Usage.PromptTokens, 
                e.Usage.CompletionTokens
            );
        }
    }

    private void HandleToolCall(LlmToolCallEvent e)
    {
        if (!enableLogging) return;

        if (e.ToolCalls != null && e.ToolCalls.Count > 0)
        {
            SessionAggregator.Instance.AddToolCallToRequest(e.SessionId, e.ToolCalls);
            SessionAggregator.Instance.AddToolCall(e.SessionId, e.ToolCalls);
        }
    }

    private void HandleSessionComplete(AggregatedSession session)
    {
        if (!enableLogging || session == null) return;

        Debug.Log($"[ConversationLogger] Saving aggregated session: {session.SessionId}");
        SaveAggregatedSession(session);
    }

    private void SaveAggregatedSession(AggregatedSession session)
    {
        string baseFileName = GenerateFileName(session);

        foreach (var exporter in _exporters)
        {
            try
            {
                string filePath = GetFilePath(baseFileName, exporter.FileExtension);
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                exporter.ExportAggregated(filePath, session);
                Debug.Log($"[ConversationLogger] Saved: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConversationLogger] Export failed: {ex.Message}");
            }
        }
    }

    private string GenerateFileName(AggregatedSession session)
    {
        string timestamp = session.StartTime.ToString("yyyy-MM-dd_HHmmss");
        string safeSessionId = SanitizeFileName(session.SessionId ?? "unknown");
        string safeProvider = SanitizeFileName(session.Provider ?? "unknown");
        return $"conversation_{timestamp}_{safeProvider}_{safeSessionId}";
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "unknown";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }

    private string GetLogDirectory()
    {
        string basePath;

        switch (saveLocation)
        {
            case LogSaveLocation.PersistentDataPath:
                basePath = Application.persistentDataPath;
                break;

            case LogSaveLocation.ProjectDirectory:
                basePath = Application.dataPath;
                basePath = Path.GetDirectoryName(basePath);
                break;

            case LogSaveLocation.CustomPath:
                if (!string.IsNullOrEmpty(customLogPath))
                {
                    basePath = customLogPath;
                }
                else
                {
                    basePath = Application.persistentDataPath;
                    Debug.LogWarning("[ConversationLogger] CustomPath is empty, fallback to PersistentDataPath");
                }
                break;

            default:
                basePath = Application.persistentDataPath;
                break;
        }

        return Path.Combine(basePath, logFolderName);
    }

    private string GetFilePath(string baseName, string extension)
    {
        string logDir = GetLogDirectory();
        string subDir = extension == ".json" ? "json" : "markdown";
        return Path.Combine(logDir, subDir, $"{baseName}{extension}");
    }

    public string GetCurrentLogDirectory()
    {
        return GetLogDirectory();
    }

    private void OnDestroy()
    {
        LlmEventBus.OnSessionStart -= HandleSessionStart;
        LlmEventBus.OnRequest -= HandleRequest;
        LlmEventBus.OnResponse -= HandleResponse;
        LlmEventBus.OnToolCall -= HandleToolCall;
        
        if (SessionAggregator.Instance != null)
        {
            SessionAggregator.Instance.OnSessionComplete -= HandleSessionComplete;
        }
    }

    private void OnApplicationQuit()
    {
        SessionAggregator.Instance?.ForceFinalize();
    }
}
