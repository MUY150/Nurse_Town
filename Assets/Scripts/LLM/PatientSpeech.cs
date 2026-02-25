using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// OpenAI请求管理器，负责与DeepSeek API进行通信，处理病人语音生成
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 单例模式（Singleton）：使用静态Instance字段确保全局唯一
/// - 协程（Coroutine）：使用IEnumerator和yield return实现异步操作
/// - 字典集合（Dictionary）：存储聊天消息
/// - 列表集合（List）：存储病人指令
/// - 正则表达式（Regex）：提取情绪代码
/// - JSON序列化：使用JsonConvert处理JSON数据
/// - 字符串插值：$""语法构建字符串
/// - Lambda表达式：在LINQ查询中使用
/// - 异步编程：async/await关键字
/// - Unity生命周期方法：Awake()、Start()
/// - 序列化特性：[SerializeField]让私有字段在Inspector中可编辑
/// </remarks>
public class OpenAIRequest : MonoBehaviour
{
    public static OpenAIRequest Instance; // 单例模式：静态实例
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string apiKey;
    public string currentScenario = "brocaAphasia"; // 场景选择器
    private string currentPatientResponse = "";
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem(); // 评分系统实例
    private EmotionController emotionController;
    private string basePath;
    private List<string> patientInstructionsList;
    private List<Dictionary<string, string>> chatMessages;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 如果需要跨场景保持对象，取消下面注释
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 从文件加载提示词内容
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件内容</returns>
    private string LoadPromptFromFile(string fileName)
    {
        string filePath = Path.Combine(basePath, fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError("Prompt file not found: " + filePath);
            return "";
        }
        return File.ReadAllText(filePath);
    }

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("DEEPSEEK_API_KEY") ?? EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        basePath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario);
        ScoreManager.Instance.Initialize(currentScenario);
        // 初始化病人指令和聊天
        InitializePatientInstructions();
        InitializeChat();
        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
        emotionController = GetComponent<EmotionController>();
        if (emotionController == null)
        {
            // 如果不在同一GameObject上，尝试在场景中查找
            emotionController = FindObjectOfType<EmotionController>();
            if (emotionController == null)
            {
                Debug.LogError("EmotionController component not found on the GameObject or in the scene.");
            }
        }
    }

    /// <summary>
    /// 初始化病人指令列表
    /// </summary>
    private void InitializePatientInstructions()
    {
        string baseInstructions = LoadPromptFromFile("baseInstructions.txt");
        string caseHistoryPrompt = LoadPromptFromFile("caseHistory.txt");
        patientInstructionsList = new List<string>();

        for (int i = 1; i <= 3; i++)
        {
            string patientFile = $"patient{i}.txt";
            string patientSpecific = LoadPromptFromFile(patientFile);
            if (string.IsNullOrEmpty(patientSpecific))
            {
                Debug.LogError("Failed to load patient file: " + patientFile);
                continue;
            }
            string fullPrompt = $"{baseInstructions}\n{caseHistoryPrompt}\n{patientSpecific}";
            patientInstructionsList.Add(fullPrompt);
        }
        if (patientInstructionsList.Count == 0)
        {
            Debug.LogError("No patient instructions loaded for scenario: " + currentScenario);
        }
    }

    /// <summary>
    /// 初始化聊天消息
    /// </summary>
    private void InitializeChat()
    {
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements
            - Use [1] for responses involving minor pain or discomfort
            - Use [2] for positive responses, gratitude, or when feeling better
            - Use [3] for pain
            - Use [4] for sad
            - Use [5] for anger or frustration";

        // 随机选择一个病人指令
        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];

        // 组合选中的病人指令和情绪指令
        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", $"{selectedPatientInstructions}\n\n{emotionInstructions}" }
            }
        };

        PrintChatMessage(chatMessages);
        StartCoroutine(PostRequest());
    }

    /// <summary>
    /// 接收护士的转录文本
    /// </summary>
    /// <param name="transcribedText">转录的文本</param>
    public void ReceiveNurseTranscription(string transcribedText)
    {
        NurseResponds(transcribedText);
    }

    /// <summary>
    /// 护士响应处理
    /// </summary>
    /// <param name="nurseMessage">护士消息</param>
    private void NurseResponds(string nurseMessage)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        PrintChatMessage(chatMessages);
        StartCoroutine(PostRequest());

        // 评估护士的响应
        // scoringSystem.EvaluateNurseResponse(nurseMessage);
        ScoreManager.Instance.RecordTurn(currentPatientResponse, nurseMessage);
    }

    /// <summary>
    /// 发送POST请求协程
    /// </summary>
    IEnumerator PostRequest()
    {
        Debug.Log("Building request body for chat completion...");

        string requestBody = BuildRequestBody();
        var request = CreateRequest(requestBody);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response Body: " + request.downloadHandler.text);
        }
        else if (request.responseCode == 200)
        {
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();
            currentPatientResponse = messageContent;

            chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });
            PrintChatMessage(chatMessages);

            // 使用TTS播放响应
            if (TTSManager.Instance != null)
            {
                TTSManager.Instance.ConvertTextToSpeech(messageContent);
            }
            else
            {
                Debug.LogError("TTSManager instance not found.");
            }
            
            var match = Regex.Match(messageContent, @"\[(\d+)\]");
            if (!match.Success || emotionController == null) yield break;
            int emotionCode = int.Parse(match.Groups[1].Value);
            emotionController.HandleEmotionCode(emotionCode);
        }
    }

    /// <summary>
    /// 构建请求体
    /// </summary>
    /// <returns>JSON格式的请求体字符串</returns>
    private string BuildRequestBody()
    {
        var requestObject = new
        {
            model = "deepseek-chat",
            messages = chatMessages,
            temperature = 0.7f,
            max_tokens = 1500
        };
        return JsonConvert.SerializeObject(requestObject);
    }

    /// <summary>
    /// 创建Unity Web请求
    /// </summary>
    /// <param name="requestBody">请求体</param>
    /// <returns>UnityWebRequest对象</returns>
    private UnityWebRequest CreateRequest(string requestBody)
    {
        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        return request;
    }

    /// <summary>
    /// 打印聊天消息
    /// </summary>
    /// <param name="messages">消息列表</param>
    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        if (messages.Count == 0)
            return;

        var latestMessage = messages[messages.Count - 1];
        string role = latestMessage["role"];
        string content = latestMessage["content"];

        // 提取情绪代码（如果存在）
        string emotionCode = "";
        var match = Regex.Match(content, @"\[(\d+)\]$");
        if (match.Success)
        {
            emotionCode = $" (Emotion: {match.Groups[1].Value})";
        }

        Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
    }
}
