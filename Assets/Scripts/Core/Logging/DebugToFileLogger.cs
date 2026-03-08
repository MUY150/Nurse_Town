using System;
using System.IO;
using UnityEngine;

public class DebugToFileLogger : MonoBehaviour
{
    private static DebugToFileLogger _instance;
    public static DebugToFileLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (_instance == null)
        {
            CreateInstance();
        }
    }

    private static void CreateInstance()
    {
        var go = new GameObject("[DebugToFileLogger]");
        _instance = go.AddComponent<DebugToFileLogger>();
        DontDestroyOnLoad(go);
    }

    [Header("保存路径配置")]
    public LogSaveLocation saveLocation = LogSaveLocation.CustomPath;
    public string customLogPath = @"d:\UsefulDIR\tempcoding\newtry\temp_conversation_log";
    public string logFolderName = "DebugLogs";

    [Header("日志配置")]
    public bool enableFileLogging = true;
    public bool includeStackTrace = true;

    private StreamWriter _logWriter;
    private string _currentLogFilePath;
    private bool _isInitialized = false;
    private readonly object _lockObj = new object();

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

        try
        {
            string logDir = GetLogDirectory();
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string fileName = $"debug_{timestamp}.log";
            _currentLogFilePath = Path.Combine(logDir, fileName);

            _logWriter = new StreamWriter(_currentLogFilePath, true);
            _logWriter.AutoFlush = true;

            Application.logMessageReceived += HandleLogMessage;

            _isInitialized = true;

            Debug.Log($"[DebugToFileLogger] Initialized - Log file: {_currentLogFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DebugToFileLogger] Failed to initialize: {ex.Message}");
        }
    }

    private void HandleLogMessage(string logString, string stackTrace, LogType type)
    {
        if (!enableFileLogging || _logWriter == null) return;

        lock (_lockObj)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logTypeStr = GetLogTypeString(type);
                string formattedMessage = $"[{timestamp}] [{logTypeStr}] {logString}";

                _logWriter.WriteLine(formattedMessage);

                if (includeStackTrace && (type == LogType.Error || type == LogType.Exception || type == LogType.Warning))
                {
                    _logWriter.WriteLine($"    {stackTrace.Trim().Replace("\n", "\n    ")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DebugToFileLogger] Write failed: {ex.Message}");
            }
        }
    }

    private string GetLogTypeString(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                return "ERROR";
            case LogType.Warning:
                return "WARNING";
            case LogType.Exception:
                return "EXCEPTION";
            case LogType.Assert:
                return "ASSERT";
            default:
                return "INFO";
        }
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
                    Debug.LogWarning("[DebugToFileLogger] CustomPath is empty, fallback to PersistentDataPath");
                }
                break;

            default:
                basePath = Application.persistentDataPath;
                break;
        }

        return Path.Combine(basePath, logFolderName);
    }

    public string GetCurrentLogFilePath()
    {
        return _currentLogFilePath;
    }

    public string GetCurrentLogDirectory()
    {
        return GetLogDirectory();
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLogMessage;

        if (_logWriter != null)
        {
            try
            {
                _logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [DebugToFileLogger] Logger shutdown");
                _logWriter.Flush();
                _logWriter.Close();
                _logWriter.Dispose();
                _logWriter = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DebugToFileLogger] Failed to close log writer: {ex.Message}");
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (_logWriter != null)
        {
            try
            {
                _logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [DebugToFileLogger] Application quit");
                _logWriter.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DebugToFileLogger] Failed to write quit message: {ex.Message}");
            }
        }
    }

    public static void WriteCustomLog(string message)
    {
        if (_instance != null && _instance.enableFileLogging && _instance._logWriter != null)
        {
            lock (_instance._lockObj)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    _instance._logWriter.WriteLine($"[{timestamp}] [CUSTOM] {message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DebugToFileLogger] Custom log write failed: {ex.Message}");
                }
            }
        }
    }
}
