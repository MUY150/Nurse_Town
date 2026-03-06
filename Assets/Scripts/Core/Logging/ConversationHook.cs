using UnityEngine;

public static class ConversationHook
{
    public delegate void ConversationEventHandler(ConversationSnapshot snapshot);

    public static event ConversationEventHandler OnMessageSent;
    public static event ConversationEventHandler OnMessageReceived;
    public static event ConversationEventHandler OnSessionStart;
    public static event ConversationEventHandler OnSessionEnd;

    public static void TriggerMessageSent(ConversationSnapshot snapshot)
    {
        if (snapshot == null) return;
        OnMessageSent?.Invoke(snapshot);
        Debug.Log($"[ConversationHook] MessageSent triggered: {snapshot.SessionId}");
    }

    public static void TriggerMessageReceived(ConversationSnapshot snapshot)
    {
        if (snapshot == null) return;
        OnMessageReceived?.Invoke(snapshot);
        Debug.Log($"[ConversationHook] MessageReceived triggered: {snapshot.SessionId}");
    }

    public static void TriggerSessionStart(ConversationSnapshot snapshot)
    {
        if (snapshot == null) return;
        OnSessionStart?.Invoke(snapshot);
        Debug.Log($"[ConversationHook] SessionStart triggered: {snapshot.SessionId}");
    }

    public static void TriggerSessionEnd(ConversationSnapshot snapshot)
    {
        if (snapshot == null) return;
        OnSessionEnd?.Invoke(snapshot);
        Debug.Log($"[ConversationHook] SessionEnd triggered: {snapshot.SessionId}");
    }

    public static void ClearAllEvents()
    {
        OnMessageSent = null;
        OnMessageReceived = null;
        OnSessionStart = null;
        OnSessionEnd = null;
    }
}
