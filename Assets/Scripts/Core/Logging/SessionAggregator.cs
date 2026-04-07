using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SessionAggregator
{
    private static SessionAggregator _instance;
    public static SessionAggregator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SessionAggregator();
            }
            return _instance;
        }
    }

    private Dictionary<string, AggregatedSession> _sessions = new Dictionary<string, AggregatedSession>();
    private string _mainSessionId;
    private string _scoringSessionId;
    private string _sessionStartTime;
    private string _currentRequestId;
    
    public event Action<AggregatedSession> OnSessionComplete;

    public void StartMainSession(string sessionId, string provider, string model, string systemPrompt, LlmScene scene = LlmScene.Patient)
    {
        if (scene == LlmScene.Evaluation)
        {
            _scoringSessionId = sessionId;
            Debug.Log($"[SessionAggregator] Scoring session started: {sessionId}");
        }
        else
        {
            _mainSessionId = sessionId;
            _sessionStartTime = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            Debug.Log($"[SessionAggregator] Main session started: {sessionId}");
        }
        
        var session = new AggregatedSession
        {
            SessionId = sessionId,
            StartTime = DateTime.Now,
            Provider = provider,
            Model = model,
            SystemPrompt = systemPrompt,
            Messages = new List<MessageSnapshot>(),
            ToolCalls = new List<ToolCallRecord>(),
            Events = new List<EventRecord>(),
            RequestContexts = new List<RequestContext>()
        };
        
        _sessions[sessionId] = session;
    }

    public void RegisterScoringSession(string scoringSessionId)
    {
        _scoringSessionId = scoringSessionId;
        Debug.Log($"[SessionAggregator] Scoring session registered: {scoringSessionId}");
    }

    public void AddMessage(string sessionId, string role, string content)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            session = new AggregatedSession
            {
                SessionId = sessionId,
                StartTime = DateTime.Now,
                Messages = new List<MessageSnapshot>(),
                ToolCalls = new List<ToolCallRecord>(),
                Events = new List<EventRecord>()
            };
            _sessions[sessionId] = session;
        }

        session.Messages.Add(new MessageSnapshot
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.Now
        });
    }

    public void AddToolCall(string sessionId, List<ToolCall> toolCalls)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;

        foreach (var tc in toolCalls)
        {
            session.ToolCalls.Add(new ToolCallRecord
            {
                Name = tc.Name,
                Arguments = tc.Arguments,
                Timestamp = DateTime.Now
            });
        }
    }

    public void SetScoringResult(string sessionId, string result)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        
        session.ScoringResult = result;
        Debug.Log($"[SessionAggregator] Scoring result set for session: {sessionId}");
        
        if (sessionId == _scoringSessionId)
        {
            FinalizeAggregatedSession();
        }
    }

    public void SetUsage(string sessionId, int totalTokens, int promptTokens, int completionTokens)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        
        session.TotalTokens += totalTokens;
        session.PromptTokens += promptTokens;
        session.CompletionTokens += completionTokens;
    }

    public void SetRawRequestBody(string sessionId, string rawRequestBody)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        
        session.RawRequestBody = rawRequestBody;
        Debug.Log($"[SessionAggregator] Raw request body set for session: {sessionId}");
    }

    public void StartRequest(string sessionId, List<LlmMessage> messages, string rawRequestBody)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;

        var requestContext = new RequestContext
        {
            RequestId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.Now,
            Messages = messages ?? new List<LlmMessage>(),
            RawRequestBody = rawRequestBody,
            ToolCalls = new List<ToolCallRecord>()
        };

        session.RequestContexts.Add(requestContext);
        _currentRequestId = requestContext.RequestId;

        Debug.Log($"[SessionAggregator] Request started: {requestContext.RequestId}");
    }

    public void AddToolCallToRequest(string sessionId, List<ToolCall> toolCalls)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        if (string.IsNullOrEmpty(_currentRequestId)) return;

        var requestContext = session.RequestContexts.Find(rc => rc.RequestId == _currentRequestId);
        if (requestContext == null) return;

        foreach (var tc in toolCalls)
        {
            requestContext.ToolCalls.Add(new ToolCallRecord
            {
                Name = tc.Name,
                Arguments = tc.Arguments,
                Timestamp = DateTime.Now
            });
        }

        session.ToolCalls.AddRange(requestContext.ToolCalls);
    }

    public void CompleteRequest(string sessionId, LlmResponseEvent response)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        if (string.IsNullOrEmpty(_currentRequestId)) return;

        var requestContext = session.RequestContexts.Find(rc => rc.RequestId == _currentRequestId);
        if (requestContext == null) return;

        requestContext.Response = response;
        _currentRequestId = null;

        Debug.Log($"[SessionAggregator] Request completed: {requestContext.RequestId}");
    }

    private void FinalizeAggregatedSession()
    {
        if (string.IsNullOrEmpty(_mainSessionId) || string.IsNullOrEmpty(_scoringSessionId))
        {
            Debug.LogWarning("[SessionAggregator] Cannot finalize: missing session IDs");
            return;
        }

        var mainSession = _sessions.TryGetValue(_mainSessionId, out var ms) ? ms : null;
        var scoringSession = _sessions.TryGetValue(_scoringSessionId, out var ss) ? ss : null;

        if (mainSession == null)
        {
            Debug.LogWarning("[SessionAggregator] Main session not found");
            return;
        }

        var aggregated = new AggregatedSession
        {
            SessionId = _sessionStartTime ?? DateTime.Now.ToString("yyyy-MM-dd_HHmmss"),
            StartTime = mainSession.StartTime,
            EndTime = DateTime.Now,
            Provider = mainSession.Provider,
            Model = mainSession.Model,
            SystemPrompt = mainSession.SystemPrompt,
            Messages = mainSession.Messages.ToList(),
            ToolCalls = mainSession.ToolCalls.ToList(),
            TotalTokens = mainSession.TotalTokens,
            PromptTokens = mainSession.PromptTokens,
            CompletionTokens = mainSession.CompletionTokens,
            RawRequestBody = mainSession.RawRequestBody,
            RequestContexts = mainSession.RequestContexts?.ToList()
        };

        if (scoringSession != null)
        {
            aggregated.ScoringResult = scoringSession.ScoringResult;
            aggregated.ScoringMessages = scoringSession.Messages.ToList();
        }

        OnSessionComplete?.Invoke(aggregated);
        Debug.Log($"[SessionAggregator] Session finalized with {aggregated.Messages.Count} messages and {aggregated.RequestContexts?.Count ?? 0} requests");
        
        _sessions.Clear();
        _mainSessionId = null;
        _scoringSessionId = null;
    }

    public void ForceFinalize()
    {
        if (!string.IsNullOrEmpty(_mainSessionId))
        {
            if (string.IsNullOrEmpty(_scoringSessionId))
            {
                var mainSession = _sessions.TryGetValue(_mainSessionId, out var ms) ? ms : null;
                if (mainSession != null)
                {
                    var aggregated = new AggregatedSession
                    {
                        SessionId = _sessionStartTime ?? DateTime.Now.ToString("yyyy-MM-dd_HHmmss"),
                        StartTime = mainSession.StartTime,
                        EndTime = DateTime.Now,
                        Provider = mainSession.Provider,
                        Model = mainSession.Model,
                        SystemPrompt = mainSession.SystemPrompt,
                        Messages = mainSession.Messages.ToList(),
                        ToolCalls = mainSession.ToolCalls.ToList(),
                        TotalTokens = mainSession.TotalTokens,
                        PromptTokens = mainSession.PromptTokens,
                        CompletionTokens = mainSession.CompletionTokens,
                        RawRequestBody = mainSession.RawRequestBody,
                        RequestContexts = mainSession.RequestContexts?.ToList()
                    };
                    
                    OnSessionComplete?.Invoke(aggregated);
                    Debug.Log($"[SessionAggregator] Session finalized (no scoring) with {aggregated.Messages.Count} messages");
                }
            }
            else
            {
                FinalizeAggregatedSession();
            }
        }
    }

    public AggregatedSession GetCurrentSession()
    {
        if (!string.IsNullOrEmpty(_mainSessionId) && _sessions.TryGetValue(_mainSessionId, out var session))
        {
            return session;
        }
        return null;
    }
}

[Serializable]
public class AggregatedSession
{
    public string SessionId;
    public DateTime StartTime;
    public DateTime EndTime;
    public string Provider;
    public string Model;
    public string SystemPrompt;
    public List<MessageSnapshot> Messages;
    public List<ToolCallRecord> ToolCalls;
    public List<MessageSnapshot> ScoringMessages;
    public string ScoringResult;
    public int TotalTokens;
    public int PromptTokens;
    public int CompletionTokens;
    public List<EventRecord> Events;
    public string RawRequestBody;
    public List<RequestContext> RequestContexts;
}

[Serializable]
public class ToolCallRecord
{
    public string Name;
    public object Arguments;
    public DateTime Timestamp;
}

[Serializable]
public class EventRecord
{
    public string EventType;
    public DateTime Timestamp;
    public string Data;
}
