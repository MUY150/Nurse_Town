using UnityEngine;
using System;

public interface ITTSClient
{
    void Initialize();
    void SetVoice(string voice);
    void SetSpeed(float speed);
    void ConvertTextToSpeech(string text);
    event Action<AudioClip> OnTTSComplete;
}
