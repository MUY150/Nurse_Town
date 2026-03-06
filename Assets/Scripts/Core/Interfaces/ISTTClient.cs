using UnityEngine;
using System;

public interface ISTTClient
{
    void Initialize();
    void StartRecording(int durationSeconds = 10, int sampleRate = 16000);
    void StopRecordingAndTranscribe();
    void StopRecording();
    bool IsRecording { get; }
    event Action<string> OnTranscriptionComplete;
}
