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
/// 支持站立和坐姿角色类型，通过CharacterType配置
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
public class TTSManager : MonoBehaviour, ITTSProvider
{
    public static TTSManager Instance { get; private set; }
    
    public bool IsAvailable => audioSource != null;

    [Header("Audio Settings")]
    [Tooltip("Reference to the AudioSource where the speech will be played")]
    public AudioSource audioSource;

    [Header("角色配置")]
    [Tooltip("角色类型，决定动画控制器和情绪代码范围")]
    [SerializeField] private CharacterType characterType = CharacterType.Standing;

    [Header("动画控制器（可选，留空则自动查找）")]
    [Tooltip("自定义动画控制器，留空则根据角色类型自动查找")]
    [SerializeField] private MonoBehaviour customAnimationController;

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

    private static readonly string ttsEndpoint = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";

    private ICharacterAnimation animationController;
    private BloodEffectController bloodEffectController;
    private BloodTextController bloodTextController;
    private Audio2FaceManager audio2FaceManager;
    public EmotionController emotionController;

    private static readonly HttpClient httpClient = new HttpClient();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (string.IsNullOrEmpty(dashScopeApiKey))
        {
            dashScopeApiKey = EnvironmentLoader.GetEnvVariable("DASHSCOPE_API_KEY");
            Debug.Log("TTS Manager: API key loaded from environment variable (DASHSCOPE_API_KEY)");
        }

        ResolveAnimationController();

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

        emotionController = FindObjectOfType<EmotionController>();
        if (emotionController == null)
        {
            Debug.LogWarning("[TTSManager] EmotionController not found in the scene. Emotion animations will be disabled.");
        }
    }

    /// <summary>
    /// 根据角色类型解析动画控制器
    /// </summary>
    private void ResolveAnimationController()
    {
        if (customAnimationController != null)
        {
            animationController = customAnimationController as ICharacterAnimation;
            if (animationController == null)
            {
                Debug.LogError("[TTSManager] Custom animation controller does not implement ICharacterAnimation");
            }
            return;
        }
        
        animationController = GetComponent<ICharacterAnimation>();
        
        if (animationController == null)
        {
            Debug.LogWarning("[TTSManager] No ICharacterAnimation found, trying legacy controllers...");
            
            var standingController = GetComponent<CharacterAnimationController>();
            var sittingController = GetComponent<sitCharacterAnimationController>();
            
            if (standingController != null)
                animationController = standingController as ICharacterAnimation;
            else if (sittingController != null)
                animationController = sittingController as ICharacterAnimation;
        }
        
        if (animationController == null)
        {
            Debug.LogError("[TTSManager] No valid animation controller found!");
        }
        else
        {
            Debug.Log($"[TTSManager] Animation controller resolved for {characterType} character");
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

        string ttsText = text;
        Match match = Regex.Match(text.Trim(), @"\[\s*(10|[0-9])\s*\]\s*$");
        if (match.Success)
        {
            ttsText = text.Substring(0, match.Index).Trim();
        }

        byte[] audioData = await GetQwenTTSAudio(ttsText);

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
    /// <returns>音频字节数组</returns>
    private async Task<byte[]> GetQwenTTSAudio(string inputText)
    {
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
                speed = speed,
                volume = 1.0f,
                pitch = 1.0f
            }
        };

        string jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dashScopeApiKey.Trim());

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(ttsEndpoint, content);
            string responseBody = await response.Content.ReadAsStringAsync();

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
                    Debug.Log("[TTSManager] Using Base64 data from new API format");
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

            string filePath = Path.Combine(Application.persistentDataPath, $"{Guid.NewGuid()}.wav");
            File.WriteAllBytes(filePath, audioData);

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
        File.WriteAllBytes(filePath, audioData);

        StartCoroutine(LoadAndPlayAudio(filePath, messageContent));
    }

    /// <summary>
    /// 加载并播放音频文件的协程
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="messageContent">消息内容</param>
    private IEnumerator LoadAndPlayAudio(string filePath, string messageContent)
    {
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = audioClip;
            audioSource.Play();

            if (emotionController != null)
                emotionController.PlayEmotion();
            else
                Debug.LogWarning("EmotionController missing during playback!");

            UpdateAnimation(messageContent);

            float waitTime = audioClip.length + 0.5f;
            Debug.Log($"Audio playing, will wait {waitTime} seconds for completion");
            yield return new WaitForSeconds(waitTime);

            Debug.Log("Audio playback completed");
        }
        else
        {
            Debug.LogError("Audio file loading error: " + www.error);
        }

        if (deleteCachedFiles && File.Exists(filePath))
        {
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

    /// <summary>
    /// 根据消息内容更新动画
    /// </summary>
    /// <param name="message">消息内容</param>
    public void UpdateAnimation(string message)
    {
        if (animationController == null)
        {
            Debug.LogWarning("[TTSManager] Cannot update animation: animationController is null");
            return;
        }

        Match match = Regex.Match(message, @"\[\s*(\d+)\s*\]\s*$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            
            int maxCode = GetMaxEmotionCode();
            if (emotionCode > maxCode)
            {
                Debug.LogWarning($"[TTSManager] Emotion code {emotionCode} out of range for {characterType} (max: {maxCode})");
                animationController.PlayIdle();
                return;
            }
            
            if (characterType == CharacterType.Sitting)
            {
                PlaySittingAnimation(emotionCode);
            }
            else
            {
                PlayStandingAnimation(emotionCode);
            }
        }
        else
        {
            Debug.LogWarning($"[TTSManager] No emotion code found: {message}");
            animationController.PlayIdle();
        }
    }

    /// <summary>
    /// 播放坐姿角色动画
    /// </summary>
    /// <param name="emotionCode">情绪代码 (0-4)</param>
    private void PlaySittingAnimation(int emotionCode)
    {
        switch (emotionCode)
        {
            case 0: 
                animationController.PlayAnimation("bend"); 
                break;
            case 1: 
                animationController.PlayAnimation("rub_arm"); 
                break;
            case 2: 
                animationController.PlayAnimation("sad"); 
                break;
            case 3: 
                animationController.PlayAnimation("thumb_up"); 
                break;
            case 4:
                animationController.PlayAnimation("BP");
                if (bloodEffectController != null)
                    bloodEffectController.SetBloodVisibility(true);
                if (bloodTextController != null)
                    bloodTextController.SetBloodTextVisibility(true);
                break;
            default: 
                animationController.PlayIdle(); 
                break;
        }
    }

    /// <summary>
    /// 播放站立角色动画
    /// </summary>
    /// <param name="emotionCode">情绪代码 (0-10)</param>
    private void PlayStandingAnimation(int emotionCode)
    {
        switch (emotionCode)
        {
            case 0: 
                animationController.PlayIdle(); 
                break;
            case 1: 
                animationController.PlayAnimation("pain"); 
                Debug.Log("changing to pain");
                break;
            case 2: 
                animationController.PlayAnimation("happy"); 
                break;
            case 3: 
                animationController.PlayAnimation("shrug"); 
                break;
            case 4: 
                animationController.PlayAnimation("head_nod"); 
                break;
            case 5: 
                animationController.PlayAnimation("head_shake"); 
                break;
            case 6: 
                animationController.PlayAnimation("writhing_pain"); 
                break;
            case 7: 
                animationController.PlayAnimation("sad"); 
                break;
            case 8: 
                animationController.PlayAnimation("arm_stretch"); 
                break;
            case 9: 
                animationController.PlayAnimation("neck_stretch"); 
                break;
            case 10:
                animationController.PlayAnimation("blood_pre");
                if (bloodEffectController != null)
                    bloodEffectController.SetBloodVisibility(true);
                if (bloodTextController != null)
                    bloodTextController.SetBloodTextVisibility(true);
                break;
        }
    }

    /// <summary>
    /// 获取当前角色类型的最大情绪代码
    /// </summary>
    /// <returns>最大情绪代码值</returns>
    private int GetMaxEmotionCode()
    {
        return characterType switch
        {
            CharacterType.Standing => 10,
            CharacterType.Sitting => 4,
            _ => 10
        };
    }

    /// <summary>
    /// 设置角色类型并重新解析动画控制器
    /// </summary>
    /// <param name="type">角色类型</param>
    public void SetCharacterType(CharacterType type)
    {
        characterType = type;
        ResolveAnimationController();
        Debug.Log($"[TTSManager] Character type set to {type}, max emotion code: {GetMaxEmotionCode()}");
    }

    /// <summary>
    /// 获取当前角色类型
    /// </summary>
    /// <returns>当前角色类型</returns>
    public CharacterType GetCharacterType()
    {
        return characterType;
    }
}
