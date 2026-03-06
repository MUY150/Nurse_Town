using System;

[Serializable]
public class LoggingConfig
{
    public bool enableLogging = true;
    public bool enableJson = true;
    public bool enableMarkdown = true;
    public bool includeSystemPrompt = false;

    public float processInterval = 0.1f;
    public int batchSize = 5;
    public int maxQueueSize = 100;

    public bool logOnMessageSent = false;
    public bool logOnMessageReceived = true;
    public bool logOnSessionEnd = true;

    public LogSaveLocation saveLocation = LogSaveLocation.PersistentDataPath;
    public string customLogPath = "";
    public string logFolderName = "ConversationLogs";

    private static LoggingConfig _instance;
    public static LoggingConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LoggingConfig();
            }
            return _instance;
        }
    }

    public static void SetConfig(LoggingConfig config)
    {
        _instance = config;
    }
}
