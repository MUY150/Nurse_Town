using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public bool includeSystemPrompt = true;

    [Header("异步处理配置")]
    public float processInterval = 0.1f;
    public int batchSize = 5;
    public int maxQueueSize = 100;

    [Header("保存时机配置")]
    public bool logOnMessageSent = true;
    public bool logOnMessageReceived = true;
    public bool logOnSessionEnd = true;
    public bool logOnSessionStart = true;

    [Header("保存路径配置")]
    public LogSaveLocation saveLocation = LogSaveLocation.CustomPath;
    public string customLogPath = @"d:\UsefulDIR\tempcoding\newtry\temp_conversation_log";
    public string logFolderName = "ConversationLogs";

    private SaveTaskQueue _taskQueue;
    private List<IConversationExporter> _exporters;
    private Coroutine _processCoroutine;
    private bool _isProcessing = false;
    private bool _isInitialized = false;
    private Dictionary<string, string> _pendingRequests = new Dictionary<string, string>();

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

        _taskQueue = new SaveTaskQueue(maxQueueSize);
        _exporters = new List<IConversationExporter>();

        if (enableJson)
            _exporters.Add(new JsonExporter());
        if (enableMarkdown)
            _exporters.Add(new MarkdownExporter());

        LlmEventBus.OnSessionStart += HandleLlmSessionStart;
        LlmEventBus.OnRequest += HandleLlmRequest;
        LlmEventBus.OnResponse += HandleLlmResponse;
        LlmEventBus.OnSessionEnd += HandleLlmSessionEnd;

        _processCoroutine = StartCoroutine(ProcessQueueAsync());
        _isInitialized = true;

        Debug.Log($"[ConversationLogger] Initialized - SaveLocation: {saveLocation}, Queue: {maxQueueSize}");
    }

    private void HandleLlmSessionStart(SessionStartEvent e)
    {
        if (!enableLogging || !logOnSessionStart) return;

        var snapshot = new ConversationSnapshot
        {
            SessionId = e.SessionId,
            Provider = e.Provider,
            Model = "",
            SystemPrompt = e.SystemPrompt,
            Timestamp = e.Timestamp,
            EventType = ConversationEventType.SessionStart,
            Metadata = new Dictionary<string, object>
            {
                { "Scene", e.Scene.ToString() }
            }
        };
        EnqueueTask(snapshot);
    }

    private void HandleLlmRequest(LlmRequestEvent e)
    {
        if (!enableLogging) return;

        if (!string.IsNullOrEmpty(e.RawRequestBody))
        {
            _pendingRequests[e.SessionId] = e.RawRequestBody;
        }

        if (!logOnMessageSent) return;

        var snapshot = new ConversationSnapshot
        {
            SessionId = e.SessionId,
            Provider = e.Provider,
            Model = e.Model,
            Timestamp = e.Timestamp,
            EventType = ConversationEventType.MessageSent,
            RawRequestJson = e.RawRequestBody,
            Messages = e.Messages.Select(m => new MessageSnapshot
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };
        EnqueueTask(snapshot);
    }

    private void HandleLlmResponse(LlmResponseEvent e)
    {
        if (!enableLogging || !logOnMessageReceived) return;

        string rawRequestJson = null;
        if (_pendingRequests.TryGetValue(e.SessionId, out var cached))
        {
            rawRequestJson = cached;
            _pendingRequests.Remove(e.SessionId);
        }

        var snapshot = new ConversationSnapshot
        {
            SessionId = e.SessionId,
            Provider = e.Provider,
            Model = e.Model,
            Timestamp = e.Timestamp,
            EventType = ConversationEventType.MessageReceived,
            RawRequestJson = rawRequestJson,
            Statistics = new ConversationStatistics
            {
                TotalTokens = e.Usage?.TotalTokens ?? 0,
                PromptTokens = e.Usage?.PromptTokens ?? 0,
                CompletionTokens = e.Usage?.CompletionTokens ?? 0
            }
        };
        snapshot.Messages.Add(new MessageSnapshot
        {
            Role = "assistant",
            Content = e.Content
        });
        EnqueueTask(snapshot);
    }

    private void HandleLlmSessionEnd(SessionEndEvent e)
    {
        if (!enableLogging || !logOnSessionEnd) return;

        var snapshot = new ConversationSnapshot
        {
            SessionId = e.SessionId,
            Timestamp = e.Timestamp,
            EventType = ConversationEventType.SessionEnd,
            Statistics = new ConversationStatistics
            {
                TotalMessages = e.TotalMessages,
                TotalTokens = e.TotalTokens
            }
        };
        EnqueueTask(snapshot);
    }

    private void EnqueueTask(ConversationSnapshot snapshot)
    {
        var task = new SaveTask(snapshot, SaveTaskType.SaveBoth);
        if (_taskQueue.TryEnqueue(task))
        {
            Debug.Log($"[ConversationLogger] Task enqueued, queue size: {_taskQueue.Count}");
        }
    }

    private IEnumerator ProcessQueueAsync()
    {
        _isProcessing = true;

        while (_isProcessing)
        {
            int processed = 0;

            while (processed < batchSize && _taskQueue.TryDequeue(out var task))
            {
                yield return ProcessTaskAsync(task);
                processed++;
            }

            yield return new WaitForSeconds(processInterval);
        }
    }

    private IEnumerator ProcessTaskAsync(SaveTask task)
    {
        if (task?.Snapshot == null)
        {
            yield break;
        }

        string baseFileName = GenerateFileName(task.Snapshot);

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

                exporter.Export(filePath, task.Snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConversationLogger] Export failed: {ex.Message}");
            }

            yield return null;
        }
    }

    private string GenerateFileName(ConversationSnapshot snapshot)
    {
        string timestamp = snapshot.Timestamp.ToString("yyyy-MM-dd_HHmmss");
        string safeSessionId = SanitizeFileName(snapshot.SessionId ?? "unknown");
        string safeProvider = SanitizeFileName(snapshot.Provider ?? "unknown");
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

    public void RequestSave(ConversationSnapshot snapshot, SaveTaskType type = SaveTaskType.SaveBoth)
    {
        if (!enableLogging) return;

        var task = new SaveTask(snapshot, type);
        _taskQueue.TryEnqueue(task);
    }

    public void RequestImmediateSave(ConversationSnapshot snapshot)
    {
        if (!enableLogging || snapshot == null) return;

        StartCoroutine(ImmediateSaveCoroutine(snapshot));
    }

    private IEnumerator ImmediateSaveCoroutine(ConversationSnapshot snapshot)
    {
        string baseFileName = GenerateFileName(snapshot);

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

                exporter.Export(filePath, snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConversationLogger] Immediate save failed: {ex.Message}");
            }

            yield return null;
        }
    }

    public int GetQueueSize()
    {
        return _taskQueue?.Count ?? 0;
    }

    public string GetCurrentLogDirectory()
    {
        return GetLogDirectory();
    }

    private void OnDestroy()
    {
        _isProcessing = false;

        if (_processCoroutine != null)
        {
            StopCoroutine(_processCoroutine);
        }

        LlmEventBus.OnSessionStart -= HandleLlmSessionStart;
        LlmEventBus.OnRequest -= HandleLlmRequest;
        LlmEventBus.OnResponse -= HandleLlmResponse;
        LlmEventBus.OnSessionEnd -= HandleLlmSessionEnd;
    }

    private void OnApplicationQuit()
    {
        while (_taskQueue.TryDequeue(out var task))
        {
            try
            {
                string baseFileName = GenerateFileName(task.Snapshot);
                foreach (var exporter in _exporters)
                {
                    string filePath = GetFilePath(baseFileName, exporter.FileExtension);
                    string directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    exporter.Export(filePath, task.Snapshot);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConversationLogger] Final save failed: {ex.Message}");
            }
        }
    }
}
