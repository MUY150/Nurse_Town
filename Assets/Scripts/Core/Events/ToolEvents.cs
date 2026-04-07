using System;
using Newtonsoft.Json.Linq;

public class ToolExecutedEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ToolName { get; set; }
    public JObject Parameters { get; set; }
    public ToolResult Result { get; set; }
}

public class SessionCompleteEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool DiagnosisCorrect { get; set; }
    public string Summary { get; set; }
    public string[] KeyObservations { get; set; }
    public string[] MissedPoints { get; set; }
}

public class SpeakExecutedEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Text { get; set; }
    public string Emotion { get; set; }
    public float SpeechRate { get; set; }
}

public class AnimationExecutedEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string AnimationName { get; set; }
}

public class TTSSpeakStartedEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Text { get; set; }
    public string Emotion { get; set; }
    public float Duration { get; set; }
}

public class TTSSpeakEndedEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Text { get; set; }
    public bool WasCompleted { get; set; }
}
