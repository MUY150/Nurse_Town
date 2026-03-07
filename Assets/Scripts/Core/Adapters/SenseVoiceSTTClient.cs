using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class SenseVoiceSTTClient : MonoBehaviour, ISTTClient
{
    private string _apiKey;
    private string _apiUrl = "https://dashscope.aliyuncs.com/api/v1/services/audio/asr/transcription";
    private bool _isRecording = false;
    private AudioClip _recordedClip;

    public event Action<string> OnTranscriptionComplete;

    public void Initialize()
    {
        var config = ApiConfig.Instance;
        _apiKey = config.AlibabaApiKey;

        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogWarning("[SenseVoiceSTTClient] Alibaba API key not found");
        }
        else
        {
            Debug.Log("[SenseVoiceSTTClient] Initialized with SenseVoice");
        }
    }

    public void StartRecording(int durationSeconds = 10, int sampleRate = 16000)
    {
        if (!_isRecording)
        {
            _recordedClip = Microphone.Start(null, false, durationSeconds, sampleRate);
            _isRecording = true;
            Debug.Log("[SenseVoiceSTTClient] Recording started...");
        }
    }

    public void StopRecordingAndTranscribe()
    {
        if (_isRecording)
        {
            Microphone.End(null);
            _isRecording = false;
            Debug.Log("[SenseVoiceSTTClient] Recording stopped, transcribing...");
            _ = TranscribeAudio();
        }
    }

    public void StopRecording()
    {
        if (_isRecording)
        {
            Microphone.End(null);
            _isRecording = false;
        }
    }

    private async Task TranscribeAudio()
    {
        if (_recordedClip == null || _recordedClip.samples == 0)
        {
            Debug.LogWarning("[SenseVoiceSTTClient] No audio recorded");
            OnTranscriptionComplete?.Invoke("");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"sensevoice_audio_{timestamp}.wav";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        try
        {
            SavWav.Save(fileName, _recordedClip);
            Debug.Log($"[SenseVoiceSTTClient] Audio saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SenseVoiceSTTClient] Failed to save audio: {ex.Message}");
            OnTranscriptionComplete?.Invoke("");
            return;
        }

        string transcription = await SendToSenseVoiceAPI(filePath);
        OnTranscriptionComplete?.Invoke(transcription);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch { }
    }

    private async Task<string> SendToSenseVoiceAPI(string filePath)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogError("[SenseVoiceSTTClient] API key is missing");
            return "Error: API key missing";
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);

            using (var form = new MultipartFormDataContent())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var audioContent = new StreamContent(fileStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                form.Add(audioContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent("sensevoice-v1"), "model");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(_apiUrl, form);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Debug.Log($"[SenseVoiceSTTClient] Response: {responseContent}");
                        
                        var result = JObject.Parse(responseContent);
                        var text = result["output"]?["text"]?.ToString();
                        
                        Debug.Log($"[SenseVoiceSTTClient] Transcription: {text}");
                        return text ?? "";
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"[SenseVoiceSTTClient] Transcription failed: {response.ReasonPhrase} - {errorContent}");
                        return "";
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SenseVoiceSTTClient] Exception: {ex.Message}");
                    return "";
                }
            }
        }
    }

    public bool IsRecording => _isRecording;
}
