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
using Newtonsoft.Json.Linq;

/// <summary>
/// 坐姿角色文本转语音管理器，负责将文本转换为语音并播放，集成Qwen TTS API和情绪动画
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 单例模式（Singleton）：使用静态Instance属性确保全局唯一
/// - Unity生命周期方法：Awake()、Start()
/// - [SerializeField]特性：序列化字段，在Inspector中可编辑
/// - [Tooltip]特性：在Inspector中显示提示文本
/// - async/await异步编程：用于异步HTTP请求
/// - Task异步操作：表示异步操作
/// - using语句：自动资源管理（HttpClient、FileStream、UnityWebRequest）
/// - 异常处理：try-catch块
/// - 正则表达式（Regex）：提取情绪代码
/// - JSON序列化：使用JsonConvert和JObject处理JSON数据
/// - 字符串插值：$""语法构建字符串
/// - 泛型：HttpClient<T>、Task<T>、FindObjectOfType<T>()、GetComponent<T>()
/// - [Serializable]特性：标记类可被序列化
/// - 空条件运算符：?. 避免空引用异常
/// - Unity音频系统：AudioSource、AudioClip
/// - Unity音频类型：AudioType.MPEG
/// - 协程（Coroutine）：使用IEnumerator和yield return实现异步操作
/// - 文件I/O：Path.Combine、File.WriteAllBytes、File.Delete
/// - UnityWebRequestMultimedia：Unity多媒体HTTP请求
/// - Environment.GetEnvironmentVariable：获取环境变量
/// </remarks>
public class sitTTSManager : MonoBehaviour, ITTSProvider
{
    public static sitTTSManager Instance { get; private set; }
    
    public bool IsAvailable => audioSource != null;

    // 公共字段：音频源组件
    public AudioSource audioSource;

    // API配置
    [Tooltip("Your Alibaba Cloud Qwen API Key (sk-xxxx)")]
    [SerializeField] private string qwenApiKey = "";

    // API端点和配置
    private const string TtsEndpoint = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-to-speech";

    [Tooltip("是否在播放后删除缓存的音频文件")]
    [SerializeField] private bool deleteCachedFile = true;

    // 动画控制器组件引用
    private sitCharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private BloodTextController bloodTextController;

    /// <summary>
    /// Unity生命周期方法：对象创建时调用
    /// </summary>
    void Awake()
    {
        // 单例模式：确保只有一个sitTTSManager实例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 如果已存在实例，销毁当前对象
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Unity生命周期方法：初始化时调用
    /// </summary>
    void Start()
    {
        // 如果Inspector没有配置，尝试从环境变量加载（可选）
        if (string.IsNullOrEmpty(qwenApiKey))
        {
            qwenApiKey = Environment.GetEnvironmentVariable("QWEN_API_KEY");
        }

        // 验证API密钥
        if (string.IsNullOrEmpty(qwenApiKey))
        {
            Debug.LogError("TTS Manager: Qwen API key is missing! Please set it in the Inspector or as QWEN_API_KEY environment variable.");
            return;
        }

        // 获取组件引用
        animationController = GetComponent<sitCharacterAnimationController>();
        bloodEffectController = FindObjectOfType<BloodEffectController>();
        bloodTextController = FindObjectOfType<BloodTextController>();

        // 验证必要组件
        if (bloodEffectController == null)
            Debug.LogError("BloodEffectController not found!");
        if (bloodTextController == null)
            Debug.LogError("BloodTextController not found!");
    }

    /// <summary>
    /// 公共方法：使用可选情绪代码将文本转换为语音，如"Hello [3]"
    /// </summary>
    /// <param name="text">要转换的文本</param>
    public async void ConvertTextToSpeech(string text)
    {
        // 输入验证
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("TTS Manager: Empty text provided.");
            return;
        }

        // 提取情绪代码用于动画，但将其从TTS输入中剥离
        string ttsText = text;
        Match match = Regex.Match(text, @"\[([0-9]|10)\]$");
        if (match.Success)
        {
            ttsText = text.Substring(0, text.Length - match.Value.Length).Trim();
        }

        // 从Qwen TTS获取音频数据
        byte[] audioData = await GetQwenTTSAudio(ttsText, voice: "longxiaochun", speed: 1.0f);

        if (audioData != null)
        {
            ProcessAudioBytes(audioData, text); // 传递原始文本用于动画
        }
        else
        {
            Debug.LogError("TTS Manager: Failed to get audio from Qwen TTS.");
        }
    }

    /// <summary>
    /// 调用Qwen TTS API
    /// </summary>
    /// <param name="text">要转换的文本</param>
    /// <param name="voice">语音名称</param>
    /// <param name="speed">播放速度</param>
    /// <returns>音频字节数组</returns>
    private async Task<byte[]> GetQwenTTSAudio(string text, string voice = "Cherry", float speed = 1.0f)
    {
        // ✅ 使用aigc端点
        var url = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";

        string apiKey = qwenApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("TTS Manager: Missing API key!");
            return null;
        }

        // using语句：自动资源管理
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            // ✅ 完整参数：包括language_type（Python示例中有）
            var payload = new
            {
                model = "qwen3-tts-flash",
                task = "text_to_speech", // 添加这一行
                input = new
                {
                    text = text.Trim()
                },
                parameters = new
                {
                    voice = voice,
                    language_type = "Chinese",
                    speed = speed
                }
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(url, content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"TTS Request Failed: {response.StatusCode} - {responseText}");
                    return null;
                }

                // ✅ 解析JSON获取audio.url
                var responseObject = JObject.Parse(responseText);
                var audioUrl = responseObject["output"]?["audio"]?["url"]?.ToString();

                if (string.IsNullOrEmpty(audioUrl))
                {
                    Debug.LogError("TTS: No audio URL in response. Full response: " + responseText);
                    return null;
                }

                // ✅ 第二次请求：下载音频
                Debug.Log($"Downloading audio from: {audioUrl}");
                return await httpClient.GetByteArrayAsync(audioUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"TTS Exception: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }

    /// <summary>
    /// 处理音频字节数据并启动播放
    /// </summary>
    /// <param name="audioData">音频字节数组</param>
    /// <param name="messageContent">消息内容（用于情绪动画）</param>
    private void ProcessAudioBytes(byte[] audioData, string messageContent)
    {
        if (this == null || gameObject == null)
        {
            Debug.LogWarning("[sitTTSManager] Object has been destroyed, skipping audio processing.");
            return;
        }
        string filePath = Path.Combine(Application.persistentDataPath, $"tts_output_{Guid.NewGuid()}.mp3");
        File.WriteAllBytes(filePath, audioData);
        Debug.Log($"[sitTTSManager] Audio saved to: {filePath}");
        StartCoroutine(LoadAndPlayAudio(filePath, messageContent));
    }

    /// <summary>
    /// 加载并播放音频文件的协程
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="messageContent">消息内容（用于情绪动画）</param>
    private IEnumerator LoadAndPlayAudio(string filePath, string messageContent)
    {
        // UnityWebRequestMultimedia：Unity多媒体HTTP请求
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            audioSource.Play();
            UpdateAnimation(messageContent);
        }
        else
        {
            Debug.LogError("Failed to load audio file: " + www.error);
        }

        // 可选：删除缓存文件
        if (deleteCachedFile && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // --- Qwen TTS数据模型 ---
    /// <summary>
    /// Qwen TTS请求数据结构
    /// </summary>
    [Serializable]
    public class QwenTTSRequest
    {
        public string model = "qwen3-tts-flash";
        public Input input;
        public Parameters parameters;
    }

    /// <summary>
    /// 输入数据结构
    /// </summary>
    [Serializable]
    public class Input
    {
        public string text;
    }

    /// <summary>
    /// 参数数据结构
    /// </summary>
    [Serializable]
    public class Parameters
    {
        public string voice = "Chrrey";
        public float speed = 1.0f;
        // Note: Qwen does NOT support stability/similarity/style
    }

    /// <summary>
    /// Qwen TTS响应数据结构
    /// </summary>
    [Serializable]
    public class QwenTTSResponse
    {
        public Output output;
    }

    /// <summary>
    /// 输出数据结构
    /// </summary>
    [Serializable]
    public class Output
    {
        public string audio; // base64-encoded MP3
    }

    // --- 动画逻辑 ---
    /// <summary>
    /// 根据消息内容更新动画
    /// </summary>
    /// <param name="message">消息内容</param>
    private void UpdateAnimation(string message)
    {
        // 正则表达式：提取情绪代码
        Match match = Regex.Match(message, @"\[([0-5])\]$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch (emotionCode)
            {
                case 0: animationController.PlayBend(); break;
                case 1: animationController.PlayRubArm(); break;
                case 2: animationController.PlaySad(); break;
                case 3: animationController.PlayThumbUp(); break;
                case 4:
                    animationController.PlayBloodPressure();
                    bloodEffectController?.SetBloodVisibility(true);
                    bloodTextController?.SetBloodTextVisibility(true);
                    break;
                default:
                    animationController.PlayIdle();
                    break;
            }
        }
        else
        {
            animationController.PlayIdle();
        }
    }
}
