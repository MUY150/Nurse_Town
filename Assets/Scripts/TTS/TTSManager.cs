using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.Audio;
using System.Collections;
using Newtonsoft.Json;
using System.Text;
using NurseTown.Core.Events;

/// <summary>
/// 文本转语音管理器，负责将文本转换为语音并播放，支持Qwen TTS API和Audio2Face集成
/// 通过角色ID配置不同的动画控制器和情绪代码范围
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 单例模式（Singleton）：使用静态Instance字段确保全局唯一
/// - async/await异步编程：用于异步HTTP请求
/// - Task异步操作：表示异步操作
/// - using语句：自动资源管理（HttpClient、UnityWebRequest、FileStream）
/// - 异常处理：try-catch块
/// - 正则表达式（Regex）：提取情绪代码
/// - JSON序列化：使用JsonConvert处理JSON数据
/// - 字符串插值：$""语法构建字符串
/// - Unity生命周期方法：Awake()、Start()
/// - 协程（Coroutine）：使用IEnumerator和yield return实现异步操作
/// - 文件I/O：Path.Combine、File.WriteAllBytes、File.Delete、File.ReadAllBytes
/// - 泛型：HttpClient<T>、Task<T>、Dictionary<K,V>
/// - [Serializable]特性：标记类可被序列化
/// - 匿名类型：创建临时对象
/// - Guid：生成唯一标识符
/// - 条件运算符：三元运算符 ?:
/// - switch表达式：模式匹配
/// </remarks>
public class TTSManager : Singleton<TTSManager>, ITTSProvider
{
    public bool IsAvailable => audioSource != null;

    [Header("Audio Settings")]
    [Tooltip("Reference to the AudioSource where the speech will be played")]
    public AudioSource audioSource;

    [Header("TTS Configuration")]
    [Tooltip("API key for DashScope (Qwen TTS) - loaded from environment variable by default")]
    [SerializeField] private string dashScopeApiKey;

    [Tooltip("Voice name for Qwen TTS (e.g., Cherry, Serena, Ethan)")]
    public string voice = "Serena";

    [Header("Voice Settings")]
    [Range(0.5f, 2.0f)]
    [Tooltip("Speed multiplier (0.5 - 2.0)")]
    public float speed = 1.0f;

    [Header("Audio2Face Integration")]
    [Tooltip("Whether to use Audio2Face for facial animation")]
    public bool useAudio2Face = true;

    [Tooltip("Whether to delete cached audio files after use")]
    public bool deleteCachedFiles = true;

    [Header("Blood Effect Configuration")]
    [Tooltip("Reference to the BloodEffectController for blood effects")]
    public bool useBloodEffectController = true;

    private static readonly string ttsEndpoint = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";

    private BloodEffectController bloodEffectController;
    private BloodTextController bloodTextController;
    private Audio2FaceManager audio2FaceManager;
    private EmotionMappingConfig _emotionConfig;
    private string _currentEmotion;

    private static readonly HttpClient httpClient = new HttpClient();

    void Start()
    {
        if (string.IsNullOrEmpty(dashScopeApiKey))
        {
            dashScopeApiKey = EnvironmentLoader.GetEnvVariable("DASHSCOPE_API_KEY");
            Debug.Log("TTS Manager: API key loaded from environment variable (DASHSCOPE_API_KEY)");
        }

        LoadEmotionConfig();

        if (TTSEventPublisher.Instance == null)
        {
            var publisher = new GameObject("TTSEventPublisher");
            publisher.AddComponent<TTSEventPublisher>();
        }

        if (useAudio2Face)
        {
            audio2FaceManager = FindObjectOfType<Audio2FaceManager>();
            if (audio2FaceManager == null)
            {
                Debug.LogWarning("Audio2FaceManager not found in the scene. Audio2Face integration disabled.");
                useAudio2Face = false;
            }
            else
            {
                Debug.Log("Audio2Face integration enabled");
            }
        }

        if (!useBloodEffectController) return;

        bloodEffectController = FindObjectOfType<BloodEffectController>();
        if (bloodEffectController == null)
        {
            Debug.LogError("BloodEffectController not found in the scene. Make sure it exists in the UI!");
        }

        bloodTextController = FindObjectOfType<BloodTextController>();
        if (bloodTextController == null)
        {
            Debug.LogError("BloodTextController not found in the scene. Make sure it exists in the UI!");
        }
    }

    private void LoadEmotionConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "TTSConfigs", "emotion_mapping.json");
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                _emotionConfig = JsonConvert.DeserializeObject<EmotionMappingConfig>(json);
                Debug.Log($"[TTSManager] Loaded emotion config with {_emotionConfig.mappings.Count} mappings");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TTSManager] Failed to load emotion config: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[TTSManager] Emotion config file not found: {path}");
        }
    }

    /// <summary>
    /// 将文本转换为语音的公共方法
    /// </summary>
    /// <param name="text">要转换为语音的文本</param>
    public async void ConvertTextToSpeech(string text)
    {
        ConvertTextToSpeech(text, null, 1.0f);
    }

    /// <summary>
    /// 将文本转换为语音的公共方法（带emotion和speechRate参数）
    /// </summary>
    /// <param name="text">要转换为语音的文本</param>
    /// <param name="emotion">情绪类型（可选）</param>
    /// <param name="speechRate">语速（0.5-2.0）</param>
    public async void ConvertTextToSpeech(string text, string emotion, float speechRate)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("TTS Manager: No text provided for TTS");
            return;
        }

        string ttsText = text;

        float actualSpeed = (speechRate >= 0.5f && speechRate <= 2.0f) ? speechRate : this.speed;
        float pitch = 1.0f;
        
        if (_emotionConfig != null && !string.IsNullOrEmpty(emotion))
        {
            var mapping = _emotionConfig.GetMapping(emotion);
            if (mapping != null)
            {
                actualSpeed = actualSpeed * mapping.speedModifier;
                pitch = mapping.pitchModifier;
                Debug.Log($"[TTSManager] Emotion '{emotion}' applied: speed={actualSpeed:F2}, pitch={pitch:F2}");
            }
        }
        
        Debug.Log($"[TTSManager] TTS request: text='{ttsText.Substring(0, System.Math.Min(30, ttsText.Length))}...', emotion={emotion}, speed={actualSpeed}, pitch={pitch}");

        _currentEmotion = emotion;

        byte[] audioData = await GetQwenTTSAudio(ttsText, actualSpeed, pitch);

        if (audioData != null)
        {
            if (useAudio2Face && audio2FaceManager != null)
            {
                ProcessWithAudio2Face(audioData, text);
            }
            else
            {
                ProcessAudioBytes(audioData, text);
            }
        }
        else
        {
            Debug.LogError("TTS Manager: Failed to get audio data from Qwen TTS");
        }
    }

    /// <summary>
    /// 从Qwen获取TTS音频数据
    /// </summary>
    /// <param name="inputText">输入文本</param>
    /// <param name="speedOverride">语速覆盖（可选）</param>
    /// <param name="pitch">音调（可选）</param>
    /// <returns>音频字节数组</returns>
    private async Task<byte[]> GetQwenTTSAudio(string inputText, float? speedOverride = null, float pitch = 1.0f)
    {
        float useSpeed = speedOverride ?? speed;

        var requestBody = new
        {
            model = "qwen3-tts-flash",
            task = "text_to_speech",
            input = new { text = inputText },
            parameters = new
            {
                voice = voice,
                language_type = "Chinese",
                format = "wav",
                sample_rate = 24000,
                speed = useSpeed,
                pitch = pitch
            }
        };

        string jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        Debug.Log($"[TTSManager] === TTS HTTP Request Details ===");
        Debug.Log($"[TTSManager] Endpoint: {ttsEndpoint}");
        Debug.Log($"[TTSManager] Request Body: {jsonContent}");
        Debug.Log($"[TTSManager] ====================================");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dashScopeApiKey.Trim());

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(ttsEndpoint, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            Debug.Log($"[TTSManager] TTS API Response - Status Code: {response.StatusCode}");
            Debug.Log($"[TTSManager] TTS API Response - Response Body Length: {responseBody.Length} characters");
            Debug.Log($"[TTSManager] TTS API Response - First 500 chars: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"Qwen TTS Error: {response.StatusCode}\n{responseBody}");
                return null;
            }

            var jsonResponse = JsonConvert.DeserializeObject<QwenTTSResponse>(responseBody);
            
            if (jsonResponse?.output?.audio?.url != null)
            {
                string audioUrl = jsonResponse.output.audio.url;
                Debug.Log($"[TTSManager] Got audio URL from new API format: {audioUrl}");
                
                if (!string.IsNullOrEmpty(jsonResponse.output.audio.data))
                {
                    Debug.Log($"[TTSManager] Using Base64 data from new API format, length: {jsonResponse.output.audio.data.Length}");
                    return Convert.FromBase64String(jsonResponse.output.audio.data);
                }
                
                Debug.Log("[TTSManager] Downloading audio from URL...");
                return await DownloadAudioFromUrl(audioUrl);
            }
            
            if (jsonResponse?.output?.audio_data != null)
            {
                Debug.Log("[TTSManager] Using Base64 data from old API format");
                return Convert.FromBase64String(jsonResponse.output.audio_data);
            }
            
            Debug.LogError($"[TTSManager] Missing audio data in Qwen TTS response. Response body: {responseBody}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in GetQwenTTSAudio: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 从URL下载音频数据
    /// </summary>
    /// <param name="url">音频文件URL</param>
    /// <returns>音频字节数组</returns>
    private async Task<byte[]> DownloadAudioFromUrl(string url)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                Debug.Log($"[TTSManager] Starting audio download from URL: {url}");
                Debug.Log($"[TTSManager] HTTP Client timeout: 30 seconds");
                
                byte[] audioData = await httpClient.GetByteArrayAsync(url);
                
                Debug.Log($"[TTSManager] Successfully downloaded audio from URL");
                Debug.Log($"[TTSManager] Downloaded bytes: {audioData.Length} bytes ({audioData.Length / 1024.0:F2} KB)");
                
                return audioData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TTSManager] Failed to download audio from URL: {ex.Message}");
            Debug.LogError($"[TTSManager] Exception details: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 修复WAV文件头的chunk大小字段
    /// 阿里云TTS返回的WAV文件data chunk大小字段是错误值，需要根据实际数据大小修正
    /// </summary>
    /// <param name="audioData">原始音频数据</param>
    /// <returns>修复后的音频数据</returns>
    private byte[] FixWavChunkSize(byte[] audioData)
    {
        if (audioData == null || audioData.Length < 44)
        {
            return audioData;
        }

        try
        {
            byte[] riff = { audioData[0], audioData[1], audioData[2], audioData[3] };
            if (riff[0] != 0x52 || riff[1] != 0x49 || riff[2] != 0x46 || riff[3] != 0x46)
            {
                Debug.LogWarning("[TTSManager] Not a valid WAV file, skipping fix");
                return audioData;
            }

            int actualDataSize = audioData.Length - 44;
            int dataChunkSizePos = 40;

            byte[] correctedSize = BitConverter.GetBytes(actualDataSize);

            byte[] fixedData = new byte[audioData.Length];
            Array.Copy(audioData, 0, fixedData, 0, audioData.Length);

            fixedData[dataChunkSizePos] = correctedSize[0];
            fixedData[dataChunkSizePos + 1] = correctedSize[1];
            fixedData[dataChunkSizePos + 2] = correctedSize[2];
            fixedData[dataChunkSizePos + 3] = correctedSize[3];

            int fileSizeMinus8 = audioData.Length - 8;
            byte[] correctedFileSize = BitConverter.GetBytes(fileSizeMinus8);
            fixedData[4] = correctedFileSize[0];
            fixedData[5] = correctedFileSize[1];
            fixedData[6] = correctedFileSize[2];
            fixedData[7] = correctedFileSize[3];

            Debug.Log($"[TTSManager] Fixed WAV chunk sizes: data={actualDataSize} bytes, file_size={fileSizeMinus8} bytes");
            return fixedData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TTSManager] Failed to fix WAV chunk size: {ex.Message}");
            return audioData;
        }
    }

    /// <summary>
    /// 使用Audio2Face处理音频
    /// </summary>
    /// <param name="audioData">音频字节数组</param>
    /// <param name="messageContent">消息内容</param>
    private async void ProcessWithAudio2Face(byte[] audioData, string messageContent)
    {
        try
        {
            Debug.Log("Starting Audio2Face processing...");

            string filePath = Path.Combine(Application.persistentDataPath, $"{Guid.NewGuid()}.wav");
            byte[] fixedAudioData = FixWavChunkSize(audioData);
            File.WriteAllBytes(filePath, fixedAudioData);

            bool success = await audio2FaceManager.ProcessAudioForFacialAnimation(File.ReadAllBytes(filePath), messageContent);

            if (success)
            {
                Debug.Log("Audio2Face processing completed successfully");
            }
            else
            {
                Debug.LogError("Audio2Face processing failed. Falling back to direct audio playback.");
                ProcessAudioBytes(audioData, messageContent);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in ProcessWithAudio2Face: {ex.Message}");
            ProcessAudioBytes(audioData, messageContent);
        }
    }

    /// <summary>
    /// 处理并播放接收到的音频字节数据
    /// </summary>
    /// <param name="audioData">音频字节数组</param>
    /// <param name="messageContent">消息内容</param>
    private void ProcessAudioBytes(byte[] audioData, string messageContent)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{Guid.NewGuid()}.wav");
        byte[] fixedAudioData = FixWavChunkSize(audioData);
        File.WriteAllBytes(filePath, fixedAudioData);

        StartCoroutine(LoadAndPlayAudio(filePath, messageContent));
    }

    /// <summary>
    /// 加载并播放音频文件的协程
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="messageContent">消息内容</param>
    private IEnumerator LoadAndPlayAudio(string filePath, string messageContent)
    {
        // 验证音频文件是否存在
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[TTSManager] Audio file not found: {filePath}");
            yield break;
        }

        // 获取音频文件大小
        long fileSize = new FileInfo(filePath).Length;
        Debug.Log($"[TTSManager] Audio file size: {fileSize} bytes ({fileSize / 1024.0:F2} KB)");

        // 验证文件大小是否合理 (至少100字节)
        if (fileSize < 100)
        {
            Debug.LogError($"[TTSManager] Audio file too small: {fileSize} bytes, possibly corrupted");
            yield break;
        }

        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);

            // 添加audioClip null检查
            if (audioClip == null)
            {
                Debug.LogError("[TTSManager] Audio clip is null after download");
                yield break;
            }

            // 记录audioClip详细信息
            Debug.Log($"[TTSManager] Audio clip details - " +
                      $"length: {audioClip.length:F2} seconds, " +
                      $"samples: {audioClip.samples}, " +
                      $"channels: {audioClip.channels}, " +
                      $"frequency: {audioClip.frequency} Hz, " +
                      $"loadState: {audioClip.loadState}");

            // 验证audioClip.length是否合理
            if (audioClip.length <= 0)
            {
                Debug.LogError($"[TTSManager] Invalid audio clip length: {audioClip.length:F2} seconds");
                yield break;
            }

            // 声明waitTime变量，确保在所有作用域内都可访问
            float waitTime;

            // 检查audioClip.length是否异常 (超过10分钟可能有问题)
            if (audioClip.length > 600)
            {
                Debug.LogWarning($"[TTSManager] Audio clip length seems too long: {audioClip.length:F2} seconds, " +
                                 $"this might indicate a corrupted audio file. File size: {fileSize} bytes");
                
                // 根据文件大小估算合理的音频长度
                // 假设24kHz采样率,16位,单声道: 1秒 ≈ 48000字节
                float estimatedLength = fileSize / 48000.0f;
                Debug.Log($"[TTSManager] Estimated audio length based on file size: {estimatedLength:F2} seconds");
                
                // 使用估算的长度,但不超过60秒
                waitTime = Mathf.Min(estimatedLength + 0.5f, 60.0f);
                Debug.LogWarning($"[TTSManager] Using estimated wait time: {waitTime:F2} seconds");
            }
            else
            {
                // 正常情况,使用audioClip.length
                waitTime = audioClip.length + 0.5f;
                Debug.Log($"[TTSManager] Using audio clip length: {waitTime:F2} seconds");
                
                // 额外验证:如果audioClip.length与文件大小不匹配,使用估算值
                float expectedLength = fileSize / 48000.0f;
                if (Mathf.Abs(audioClip.length - expectedLength) > expectedLength * 0.5f)
                {
                    Debug.LogWarning($"[TTSManager] Audio clip length ({audioClip.length:F2}s) differs significantly " +
                                   $"from expected ({expectedLength:F2}s). Using estimated length.");
                    waitTime = expectedLength + 0.5f;
                }
                
                // 确保waitTime在合理范围内
                waitTime = Mathf.Clamp(waitTime, 1.0f, 60.0f);
            }

            audioSource.clip = audioClip;
            audioSource.Play();

            var startedEvent = new TTSSpeakStartedEvent
            {
                SessionId = "tts-session",
                Timestamp = DateTime.Now,
                Text = messageContent,
                Emotion = _currentEmotion ?? "neutral",
                Duration = audioClip.length
            };
            TTSEventPublisher.Instance?.PublishSpeakStarted(startedEvent);

            yield return new WaitForSeconds(waitTime);

            var endedEvent = new TTSSpeakEndedEvent
            {
                SessionId = "tts-session",
                Timestamp = DateTime.Now,
                Text = messageContent,
                WasCompleted = true
            };
            TTSEventPublisher.Instance?.PublishSpeakEnded(endedEvent);

            Debug.Log("[TTSManager] Audio playback completed");
        }
        else
        {
            Debug.LogError($"[TTSManager] Audio file loading error: {www.error}, Response code: {www.responseCode}");
        }

        if (deleteCachedFiles && File.Exists(filePath))
        {
            while (audioSource.isPlaying) yield return null;
            try
            {
                File.Delete(filePath);
                Debug.Log("[TTSManager] Deleted cached audio file");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TTSManager] Failed to delete cached audio file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Qwen TTS响应数据结构 - 支持新版API格式
    /// </summary>
    [Serializable]
    private class QwenTTSResponse
    {
        public Output output;
    }

    /// <summary>
    /// 输出数据结构 - 支持新版API格式 (audio对象包含url和data)
    /// </summary>
    [Serializable]
    private class Output
    {
        public string audio_data;
        public AudioInfo audio;
    }

    /// <summary>
    /// 音频信息数据结构 (新版API)
    /// </summary>
    [Serializable]
    private class AudioInfo
    {
        public string data;
        public string url;
        public string id;
        public long expires_at;
    }
}
