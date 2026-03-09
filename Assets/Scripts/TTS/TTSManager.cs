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
using System.Text.RegularExpressions;

/// <summary>
/// 文本转语音管理器，负责将文本转换为语音并播放，支持Qwen TTS API和Audio2Face集成
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
/// </remarks>
public class TTSManager : MonoBehaviour, ITTSProvider
{
    public static TTSManager Instance { get; private set; }
    
    public bool IsAvailable => audioSource != null;

    [Header("Audio Settings")]
    [Tooltip("Reference to the AudioSource where the speech will be played")]
    public AudioSource audioSource;

    [Header("TTS Configuration")]
    [Tooltip("API key for DashScope (Qwen TTS) - loaded from environment variable by default")]
    [SerializeField] private string dashScopeApiKey;

    [Tooltip("Voice name for Qwen TTS (e.g., longxiaochun, zhihao)")]
    public string voice = "longxiaochun";

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

    // API endpoints
    private static readonly string ttsEndpoint = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";

    // Component references
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private BloodTextController bloodTextController;
    private Audio2FaceManager audio2FaceManager;
    public EmotionController emotionController;

    // 静态HttpClient实例，用于HTTP请求
    private static readonly HttpClient httpClient = new HttpClient();

    void Awake()
    {
        // 单例模式：确保只有一个TTSManager实例
        if (Instance == null)
        {
            Instance = this;
            // 如果需要在场景切换时保持此对象，请取消注释以下行
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果已存在实例，销毁当前对象
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 如果未手动设置API密钥，则从环境变量加载
        if (string.IsNullOrEmpty(dashScopeApiKey))
        {
            dashScopeApiKey = EnvironmentLoader.GetEnvVariable("DASHSCOPE_API_KEY");
            Debug.Log("TTS Manager: API key loaded from environment variable (DASHSCOPE_API_KEY)");
        }

        // 获取所需组件的引用
        animationController = GetComponent<CharacterAnimationController>();

        // 如果使用Audio2Face，查找Audio2FaceManager
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

        // 在UI中查找血液效果控制器
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

        emotionController = FindObjectOfType<EmotionController>();
        if (emotionController == null)
        {
            Debug.LogWarning("[TTSManager] EmotionController not found in the scene. Emotion animations will be disabled.");
        }
    }

    /// <summary>
    /// 将文本转换为语音的公共方法
    /// </summary>
    /// <param name="text">要转换为语音的文本</param>
    public async void ConvertTextToSpeech(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("TTS Manager: No text provided for TTS");
            return;
        }

        // 去除情绪代码用于TTS，但保留原始文本用于动画
        string ttsText = text;
        Match match = Regex.Match(text.Trim(), @"\[\s*(10|[0-9])\s*\]\s*$");
        if (match.Success)
        {
            ttsText = text.Substring(0, match.Index).Trim(); // 提取情绪代码之前的文本
        }

        // 从Qwen TTS服务获取音频数据
        byte[] audioData = await GetQwenTTSAudio(ttsText);

        if (audioData != null)
        {
            if (useAudio2Face && audio2FaceManager != null)
            {
                // 使用Audio2Face处理
                ProcessWithAudio2Face(audioData, text);
            }
            else
            {
                // 回退到直接音频播放并处理情绪代码
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
    /// <returns>音频字节数组</returns>
    private async Task<byte[]> GetQwenTTSAudio(string inputText)
    {
        // 匿名类型：创建请求体对象
        var requestBody = new
        {
            model = "qwen3-tts-flash",
            task = "text_to_speech",
            input = new { text = inputText },
            parameters = new
            {
                voice = voice,
                language_type = "Chinese",
                format = "wav",           // Qwen返回完整的WAV
                sample_rate = 24000,      // Qwen的默认采样率
                speed = speed,            // 现有的速度滑块
                volume = 1.0f,
                pitch = 1.0f
            }
        };

        // JSON序列化：将对象转换为JSON字符串
        string jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dashScopeApiKey.Trim());

        try
        {
            // 异步HTTP POST请求
            HttpResponseMessage response = await httpClient.PostAsync(ttsEndpoint, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"Qwen TTS Error: {response.StatusCode}\n{responseBody}");
                return null;
            }

            // 解析JSON响应
            var jsonResponse = JsonConvert.DeserializeObject<QwenTTSResponse>(responseBody);
            
            // 优先尝试新版API格式 (audio.url)
            if (jsonResponse?.output?.audio?.url != null)
            {
                string audioUrl = jsonResponse.output.audio.url;
                Debug.Log($"[TTSManager] Got audio URL from new API format: {audioUrl}");
                
                // 如果 data 字段有值，优先使用 Base64 数据
                if (!string.IsNullOrEmpty(jsonResponse.output.audio.data))
                {
                    Debug.Log("[TTSManager] Using Base64 data from new API format");
                    return Convert.FromBase64String(jsonResponse.output.audio.data);
                }
                
                // 否则从 URL 下载音频
                Debug.Log("[TTSManager] Downloading audio from URL...");
                return await DownloadAudioFromUrl(audioUrl);
            }
            
            // 兼容旧版API格式 (audio_data)
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
                byte[] audioData = await httpClient.GetByteArrayAsync(url);
                Debug.Log($"[TTSManager] Successfully downloaded audio from URL: {audioData.Length} bytes");
                return audioData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TTSManager] Failed to download audio from URL: {ex.Message}");
            return null;
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

            // 保存音频副本用于直接播放（我们需要它来触发情绪）
            string filePath = Path.Combine(Application.persistentDataPath, $"{Guid.NewGuid()}.wav");
            File.WriteAllBytes(filePath, audioData); // Qwen返回完整的WAV

            // 使用Audio2Face处理音频
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
        // 将音频数据保存为本地.wav文件
        string filePath = Path.Combine(Application.persistentDataPath, $"{Guid.NewGuid()}.wav");
        File.WriteAllBytes(filePath, audioData); // Qwen返回完整的WAV

        // 启动协程加载并播放音频文件
        StartCoroutine(LoadAndPlayAudio(filePath, messageContent));
    }

    /// <summary>
    /// 加载并播放音频文件的协程
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="messageContent">消息内容</param>
    private IEnumerator LoadAndPlayAudio(string filePath, string messageContent)
    {
        // 创建UnityWebRequest加载音频文件
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            // 如果文件成功加载，获取音频剪辑并播放
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = audioClip;
            audioSource.Play();

            // 如果文件成功加载，播放情绪动画
            if (emotionController != null)
                emotionController.PlayEmotion();
            else
                Debug.LogWarning("EmotionController missing during playback!");

            // 根据情绪代码更新动画
            UpdateAnimation(messageContent);

            float waitTime = audioClip.length + 0.5f;
            Debug.Log($"Audio playing, will wait {waitTime} seconds for completion");
            yield return new WaitForSeconds(waitTime);

            Debug.Log("Audio playback completed");
        }
        else
        {
            // 如果文件加载失败，记录错误
            Debug.LogError("Audio file loading error: " + www.error);
        }

        // 可选：播放后删除文件
        if (deleteCachedFiles && File.Exists(filePath))
        {
            // 等待音频播放完成后再删除
            while (audioSource.isPlaying) yield return null;
            try
            {
                File.Delete(filePath);
                Debug.Log("Deleted cached audio file");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete cached audio file: {ex.Message}");
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
        public string audio_data; // 旧版API: Base64字符串
        public AudioInfo audio;   // 新版API: 音频信息对象
    }

    /// <summary>
    /// 音频信息数据结构 (新版API)
    /// </summary>
    [Serializable]
    private class AudioInfo
    {
        public string data;       // Base64数据 (可能为空)
        public string url;        // 音频URL
        public string id;         // 音频ID
        public long expires_at;   // 过期时间戳
    }

    /// <summary>
    /// 根据消息内容更新动画
    /// </summary>
    /// <param name="message">消息内容</param>
    public void UpdateAnimation(string message)
    {
        if (animationController == null)
        {
            Debug.LogWarning("Cannot update animation: animationController is null");
            return;
        }

        // 使用正则表达式提取情绪代码
        Match match = Regex.Match(message, @"\[\s*(10|[0-9])\s*\]\s*$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch (emotionCode)
            {
                case 0:
                    animationController.PlayIdle();
                    break;
                case 1:
                    animationController.PlayHeadPain();
                    Debug.Log("changing to pain");
                    break;
                case 2:
                    animationController.PlayHappy();
                    break;
                case 3:
                    animationController.PlayShrug();
                    break;
                case 4:
                    animationController.PlayHeadNod();
                    break;
                case 5:
                    animationController.PlayHeadShake();
                    break;
                case 6:
                    animationController.PlayWrithingInPain();
                    break;
                case 7:
                    animationController.PlaySad();
                    break;
                case 8:
                    animationController.PlayArmStretch();
                    break;
                case 9:
                    animationController.PlayNeckStretch();
                    break;
                case 10:
                    animationController.PlayBloodPressure();
                    if (bloodEffectController != null)
                        bloodEffectController.SetBloodVisibility(true);
                    if (bloodTextController != null)
                        bloodTextController.SetBloodTextVisibility(true);
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"No emotion code found: {message}");
            animationController.PlayIdle();
        }
    }
}
