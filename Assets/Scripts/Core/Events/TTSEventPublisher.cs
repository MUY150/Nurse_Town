using UnityEngine;
using System;

namespace NurseTown.Core.Events
{
    public class TTSEventPublisher : MonoBehaviour
    {
        public static TTSEventPublisher Instance { get; private set; }

        public event Action<TTSSpeakStartedEvent> OnSpeakStarted;
        public event Action<TTSSpeakEndedEvent> OnSpeakEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PublishSpeakStarted(TTSSpeakStartedEvent evt)
        {
            Debug.Log($"[TTSEventPublisher] Speak started: {evt.Text.Substring(0, Math.Min(20, evt.Text.Length))}...");
            OnSpeakStarted?.Invoke(evt);
        }

        public void PublishSpeakEnded(TTSSpeakEndedEvent evt)
        {
            Debug.Log($"[TTSEventPublisher] Speak ended: WasCompleted={evt.WasCompleted}");
            OnSpeakEnded?.Invoke(evt);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
