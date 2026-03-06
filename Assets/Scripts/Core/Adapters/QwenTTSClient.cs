using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class QwenTTSClient : MonoBehaviour, ITTSClient
{
    private string _apiUrl;
    private string _apiKey;
    private string _voice = "longxiaochun";
    private float _speed = 1.0f;

    public AudioSource audioSource;
    public event Action<AudioClip> OnTTSComplete;

    public void Initialize()
    {
        var config = ApiConfig.Instance;
        _apiKey = config.AlibabaApiKey;
        _apiUrl = config.QwenMultiModalUrl;

        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogWarning("[QwenTTSClient] Alibaba API key not found");
        }
        else
        {
            Debug.Log("[QwenTTSClient] Initialized with Qwen TTS");
        }
    }

    public void SetVoice(string voice)
    {
        _voice = voice;
    }

    public void SetSpeed(float speed)
    {
        _speed = Mathf.Clamp(speed, 0.5f, 2.0f);
    }

    public void ConvertTextToSpeech(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[QwenTTSClient] Empty text provided");
            return;
        }

        string ttsText = text;
        Match match = Regex.Match(text.Trim(), @"\[\s*(10|[0-9])\s*\]\s*$");
        if (match.Success)
        {
            ttsText = text.Substring(0, match.Index).Trim();
        }

        StartCoroutine(GetQwenTTSAudio(ttsText));
    }

    private IEnumerator GetQwenTTSAudio(string text)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogError("[QwenTTSClient] API key is missing");
            yield break;
        }

        var payload = new
        {
            model = "qwen3-tts-flash",
            task = "text_to_speech",
            input = new { text = text.Trim() },
            parameters = new
            {
                voice = _voice,
                language_type = "Chinese",
                speed = _speed,
                format = "wav",
                sample_rate = 24000
            }
        };

        string json = JsonConvert.SerializeObject(payload);

        var request = new UnityWebRequest(_apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + _apiKey.Trim());

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[QwenTTSClient] TTS Request Failed: {request.error} - {request.downloadHandler.text}");
            yield break;
        }

        var responseObject = JObject.Parse(request.downloadHandler.text);
        var audioDataBase64 = responseObject["output"]?["audio_data"]?.ToString();

        if (!string.IsNullOrEmpty(audioDataBase64))
        {
            byte[] audioData = Convert.FromBase64String(audioDataBase64);
            StartCoroutine(LoadAndPlayAudio(audioData));
        }
        else
        {
            var audioUrl = responseObject["output"]?["audio"]?["url"]?.ToString();
            if (!string.IsNullOrEmpty(audioUrl))
            {
                yield return StartCoroutine(DownloadAndPlayAudio(audioUrl));
            }
            else
            {
                Debug.LogError("[QwenTTSClient] No audio data in response");
            }
        }
    }

    private IEnumerator DownloadAndPlayAudio(string url)
    {
        var www = UnityWebRequest.Get(url);
        www.downloadHandler = new DownloadHandlerBuffer();
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            StartCoroutine(LoadAndPlayAudio(www.downloadHandler.data));
        }
        else
        {
            Debug.LogError($"[QwenTTSClient] Failed to download audio: {www.error}");
        }
    }

    private IEnumerator LoadAndPlayAudio(byte[] audioData)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{Guid.NewGuid()}.wav");
        File.WriteAllBytes(filePath, audioData);

        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            
            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }

            OnTTSComplete?.Invoke(clip);
        }
        else
        {
            Debug.LogError($"[QwenTTSClient] Failed to load audio: {www.error}");
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch { }
    }
}
