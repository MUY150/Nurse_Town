using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class WhisperSTTClient : MonoBehaviour, ISTTClient
{
    private string _apiKey;
    private string _apiUrl = "https://api.openai.com/v1/audio/transcriptions";
    private bool _isRecording = false;
    private AudioClip _recordedClip;

    public event Action<string> OnTranscriptionComplete;

    public void Initialize()
    {
        var config = ApiConfig.Instance;
        _apiKey = config.OpenAIApiKey;

        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogWarning("[WhisperSTTClient] OpenAI API key not found");
        }
        else
        {
            Debug.Log("[WhisperSTTClient] Initialized with Whisper");
        }
    }

    public void StartRecording(int durationSeconds = 10, int sampleRate = 16000)
    {
        if (!_isRecording)
        {
            _recordedClip = Microphone.Start(null, false, durationSeconds, sampleRate);
            _isRecording = true;
            Debug.Log("[WhisperSTTClient] Recording started...");
        }
    }

    public void StopRecordingAndTranscribe()
    {
        if (_isRecording)
        {
            Microphone.End(null);
            _isRecording = false;
            Debug.Log("[WhisperSTTClient] Recording stopped, transcribing...");
            StartCoroutine(TranscribeAudio());
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

    private IEnumerator TranscribeAudio()
    {
        if (_recordedClip == null || _recordedClip.samples == 0)
        {
            Debug.LogWarning("[WhisperSTTClient] No audio recorded");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"whisper_audio_{timestamp}.wav";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        try
        {
            SavWav.Save(fileName, _recordedClip);
            Debug.Log($"[WhisperSTTClient] Audio saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WhisperSTTClient] Failed to save audio: {ex.Message}");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        yield return StartCoroutine(SendToWhisperAPI(filePath));

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch { }
    }

    private IEnumerator SendToWhisperAPI(string filePath)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogError("[WhisperSTTClient] API key is missing");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        byte[] audioData = File.ReadAllBytes(filePath);
        
        // 构建 multipart/form-data 请求体
        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
        byte[] boundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
        byte[] endBoundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

        using (var request = new UnityWebRequest(_apiUrl, "POST"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            // 构建 form data
            var formData = new List<byte>();
            
            // 添加 model 字段
            formData.AddRange(boundaryBytes);
            string modelHeader = "Content-Disposition: form-data; name=\"model\"\r\n\r\n";
            formData.AddRange(Encoding.UTF8.GetBytes(modelHeader));
            formData.AddRange(Encoding.UTF8.GetBytes("whisper-1"));
            
            // 添加 file 字段
            formData.AddRange(boundaryBytes);
            string fileHeader = "Content-Disposition: form-data; name=\"file\"; filename=\"audio.wav\"\r\n";
            fileHeader += "Content-Type: audio/wav\r\n\r\n";
            formData.AddRange(Encoding.UTF8.GetBytes(fileHeader));
            formData.AddRange(audioData);
            
            formData.AddRange(endBoundaryBytes);

            byte[] bodyRaw = formData.ToArray();
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseContent = request.downloadHandler.text;
                Debug.Log($"[WhisperSTTClient] Response: {responseContent}");
                
                try
                {
                    var result = JsonConvert.DeserializeObject<WhisperResponse>(responseContent);
                    Debug.Log($"[WhisperSTTClient] Transcription: {result.text}");
                    OnTranscriptionComplete?.Invoke(result.text);
                }
                catch
                {
                    OnTranscriptionComplete?.Invoke("");
                }
            }
            else
            {
                Debug.LogError($"[WhisperSTTClient] Transcription failed: {request.error}");
                OnTranscriptionComplete?.Invoke("");
            }
        }
    }

    public bool IsRecording => _isRecording;

    private class WhisperResponse
    {
        public string text { get; set; }
    }
}
