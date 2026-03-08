using System;
using System.Collections.Generic;

public interface ILlmEvent
{
    string SessionId { get; }
    DateTime Timestamp { get; }
}

public class LlmRequestEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public List<LlmMessage> Messages { get; set; }
    public LlmScene Scene { get; set; }
    public string RawRequestBody { get; set; }
}

public class LlmResponseEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public string Content { get; set; }
    public LlmUsage Usage { get; set; }
    public bool Success { get; set; }
}

public class LlmErrorEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Provider { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public bool WillRetry { get; set; }
}

public class SessionStartEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public LlmScene Scene { get; set; }
    public string SystemPrompt { get; set; }
    public string Provider { get; set; }
}

public class SessionEndEvent : ILlmEvent
{
    public string SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public LlmScene Scene { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
}

public static class LlmEventBus
{
    private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();
    
    public static event Action<LlmRequestEvent> OnRequest;
    public static event Action<LlmResponseEvent> OnResponse;
    public static event Action<LlmErrorEvent> OnError;
    public static event Action<SessionStartEvent> OnSessionStart;
    public static event Action<SessionEndEvent> OnSessionEnd;
    
    public static void Subscribe<T>(Action<T> handler) where T : ILlmEvent
    {
        var type = typeof(T);
        if (_handlers.ContainsKey(type))
        {
            _handlers[type] = Delegate.Combine(_handlers[type], handler);
        }
        else
        {
            _handlers[type] = handler;
        }
    }
    
    public static void Unsubscribe<T>(Action<T> handler) where T : ILlmEvent
    {
        var type = typeof(T);
        if (_handlers.ContainsKey(type))
        {
            _handlers[type] = Delegate.Remove(_handlers[type], handler);
        }
    }
    
    public static void Publish<T>(T eventData) where T : ILlmEvent
    {
        if (eventData == null) return;
        
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var handler))
        {
            (handler as Action<T>)?.Invoke(eventData);
        }
        
        if (eventData is LlmRequestEvent requestEvent)
        {
            OnRequest?.Invoke(requestEvent);
        }
        else if (eventData is LlmResponseEvent responseEvent)
        {
            OnResponse?.Invoke(responseEvent);
        }
        else if (eventData is LlmErrorEvent errorEvent)
        {
            OnError?.Invoke(errorEvent);
        }
        else if (eventData is SessionStartEvent startEvent)
        {
            OnSessionStart?.Invoke(startEvent);
        }
        else if (eventData is SessionEndEvent endEvent)
        {
            OnSessionEnd?.Invoke(endEvent);
        }
    }
    
    public static void ClearAllHandlers()
    {
        _handlers.Clear();
        OnRequest = null;
        OnResponse = null;
        OnError = null;
        OnSessionStart = null;
        OnSessionEnd = null;
    }
}
