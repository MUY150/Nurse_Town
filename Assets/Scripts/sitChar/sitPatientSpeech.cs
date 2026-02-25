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
using uLipSync;

/// <summary>
/// 坐姿病人语音控制器，负责处理坐姿病人的AI对话、情绪检测和评分系统
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 单例模式（Singleton）：使用静态Instance字段确保全局唯一
/// - Unity生命周期方法：Awake()、Start()
/// - [SerializeField]特性：序列化字段，在Inspector中可编辑
/// - 字符串插值：$""语法构建字符串
/// - 泛型：List<T>、Dictionary<K,V>、GetComponent<T>()
/// - 正则表达式（Regex）：提取情绪代码
/// - UnityWebRequest：Unity的HTTP请求类
/// - 协程（Coroutine）：使用IEnumerator和yield return实现异步网络请求
/// - PlayerPrefs：Unity持久化存储
/// - 异常处理：try-catch块
/// - 字符串操作：Substring()、Trim()、Split()等
/// - 匿名类型：创建临时对象
/// - 空条件运算符：?. 避免空引用异常
/// - LINQ查询：Where()、Select()等
/// - System.Random：随机数生成
/// - 字符串插值：string.Format()
/// - JsonConvert：JSON序列化库
/// - StringBuilder：高效字符串构建
/// - [Serializable]特性：标记类可被序列化
/// </remarks>
public class sitPatientSpeech : MonoBehaviour
{
    // 单例模式：静态实例，确保全局唯一
    public static sitPatientSpeech Instance; // Singleton instance

    // 公共字段：API配置
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string apiKey;
    
    // 组件引用
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem(); // For scoring system

    // 聊天消息列表
    private List<Dictionary<string, string>> chatMessages;

    // Variables for multiple patients
    private List<string> patientInstructionsList;
    private string patient1Instructions;
    private string patient2Instructions;
    private string patient3Instructions;

    // solve 429 too many requests error
    private bool isRequestInProgress = false;
    private float requestCooldown = 1.0f;

    private string transcript = "";

    /// <summary>
    /// Unity生命周期方法：对象创建时调用
    /// </summary>
    void Awake()
    {
        // 单例模式：确保只有一个sitPatientSpeech实例
        if (Instance == null)
        {
            Instance = this;
            // 如果需要对象在场景切换时保持存在，请取消注释以下行
            // DontDestroyOnLoad(gameObject);
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
        // 如果Inspector没有配置apiKey，尝试从环境变量获取
        if (string.IsNullOrEmpty(apiKey))
        {
            // 从环境变量获取API密钥
            apiKey = EnvironmentLoader.GetEnvVariable("DEEPSEEK_API_KEY")
                     ?? EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY"); // 兜底策略
        }

        // 验证API密钥
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[sitPatientSpeech] API Key is missing! Please set it in the Inspector or provide a .env file with DEEPSEEK_API_KEY.");
            enabled = false; // 禁用脚本防止继续执行
            return;
        }

        // 显示API密钥前缀用于调试
        Debug.Log($"Using API Key (first 8 chars): {apiKey.Substring(0, Mathf.Min(8, apiKey.Length))}...");

        // 初始化病人指令和聊天系统
        InitializePatientInstructions();
        InitializeChat();

        // 获取组件引用
        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
    }

    /// <summary>
    /// 初始化病人指令，定义不同类型的病人角色
    /// </summary>
    private void InitializePatientInstructions()
    {
        // 基础指令：病人的医疗背景和症状
        string baseInstructions = @"
            You are strictly playing the role of Mrs. Johnson. 
            Background:
            - Mrs. Johnson is a 62-year-old female admitted to the hospital with severe headache and dizziness.
            - She has a 5-year history of hypertension and occasionally misses doses due to forgetfulness.
            - Family history includes hypertension and heart disease (mother and brother).
            - Works as a school teacher and lives with her husband.
            - Leads a sedentary lifestyle and enjoys watching TV in her spare time.

            Clinical Presentation and Responses:
            - Symptoms: Constant, throbbing headache in the temples; dizziness worsens upon standing quickly. No vision changes, nausea, or confusion.
            - Medical History: Openly shares hypertension history; mentions sometimes forgetting medication.
            - Current Medications: Tries to recall antihypertensive medication name (e.g., 'I think it's called lisinopril...').
            - Lifestyle: Admits to a sedentary routine; doesn't exercise regularly; occasionally eats salty foods and drinks coffee daily.
            - Family History: Mentions mother and brother with high blood pressure; adds that mother had heart disease if prompted.
            ";

        // 病人1：正常性格（原始版本）
        patient1Instructions = baseInstructions + @"

            Tone and Personality:
            - Polite and cooperative tone; generally compliant and concerned about her health.
            - Expresses mild anxiety about current symptoms; headaches and dizziness are more severe than usual.
            - Occasionally shows forgetfulness or hesitation when recalling medication details.

            Emotional Response:
            - Displays concern when discussing family history but reassures that such symptoms are unusual for her.
            - Open to lifestyle changes or medication adherence strategies but hesitant about drastic changes.

            As Mrs. Johnson, please initiate the conversation by greeting the nurse and mentioning how you're feeling. 
            If off-topic, guide the conversation back to your health concerns.
            Please keep responses concise.
            ";

        // 病人2：很少说话，给出模糊描述
        patient2Instructions = baseInstructions + @"

            Tone and Personality:
            - Reserved and speaks very little.
            - Provides brief and sometimes vague answers, saying something like 'i don't remember.../i am not sure'
            - Requires the nurse to ask more probing questions to obtain information.

            Emotional Response:
            - Appears indifferent or slightly detached.
            - Does not volunteer additional information unless specifically asked.
            - May give one-word answers or simple acknowledgments.

            As Mrs. Johnson, please initiate the conversation by saying minimal words like 'hi nurse'.
            ";

        // 病人3：情绪激动，使用"我觉得我要死了"等表达
        patient3Instructions = baseInstructions + @"

            Tone and Personality:
            - Highly emotional and anxious.
            - Responses are intense and be exaggerated.
            - Frequently uses emotional phrases like 'I feel I am dying. I cannot stand it!!!!!!'

            Emotional Response:
            - Displays significant anxiety and distress about her condition.
            - May interrupt the nurse or speak rapidly.
            - Finds it difficult to be consoled.

            As Mrs. Johnson, please initiate the conversation by expressing your extreme distress.
            ";

        // 创建病人指令列表
        patientInstructionsList = new List<string>()
        {
            patient1Instructions,
            patient2Instructions,
            patient3Instructions
        };
    }

    /// <summary>
    /// 初始化聊天系统，随机选择病人类型并开始对话
    /// </summary>
    private void InitializeChat()
    {
        // 情绪指令：要求AI在每个回复后添加情绪代码
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements (plays bend animation)
            - Use [1] for responses showing physical discomfort (plays rub arm animation)
            - Use [2] for sad or negative emotional responses (plays sad animation)
            - Use [3] for positive responses or agreement, and appreciation (plays thumbs up animation)
            - Use [4] for blood pressureing, if the nurse asks to measure your blood pressure (plays arm raise animation)";

        // 随机选择病人指令
        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];

        // 初始化聊天消息列表
        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", $"{selectedPatientInstructions}\n\n{emotionInstructions}" }
            }
        };

        // 打印初始消息并开始对话
        PrintChatMessage(chatMessages);
        Debug.Log("Starting PostRequest");
        StartCoroutine(PostRequest());
        Debug.Log("Finished PostRequest");
    }

    /// <summary>
    /// 接收护士的语音转文本结果并处理
    /// </summary>
    /// <param name="transcribedText">护士的语音转文本结果</param>
    public void ReceiveNurseTranscription(string transcribedText)
    {
        NurseResponds(transcribedText);
    }

    /// <summary>
    /// 处理护士的回复并生成病人响应
    /// </summary>
    /// <param name="nurseMessage">护士的文本消息</param>
    private void NurseResponds(string nurseMessage)
    {
        Debug.Log("NurseResponds: Starting...");
        
        // 将护士消息添加到聊天历史
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        PrintChatMessage(chatMessages);

        // 将用户输入追加到转录文本
        transcript += $"User:\n{nurseMessage}\n\n";

        // 仅当没有正在进行的请求时才开始新请求
        if (!isRequestInProgress)
        {
            StartCoroutine(PostRequest());
        }
        else
        {
            Debug.Log("Request in progress. Waiting for cooldown...");
        }

        // 评估护士的回复
        scoringSystem.EvaluateNurseResponse(nurseMessage);
    }

    /// <summary>
    /// 向AI API发送请求的协程方法
    /// </summary>
    IEnumerator PostRequest()
    {
        isRequestInProgress = true;
        Debug.Log("=== [DEBUG] Starting PostRequest ===");

        // 1. 打印当前使用的URL和Key（遮盖后）
        Debug.Log($"[DEBUG] Target API URL: {apiUrl}");
        if (!string.IsNullOrEmpty(apiKey))
        {
            string maskedKey = apiKey.Length > 8
                ? $"sk-...{apiKey.Substring(apiKey.Length - 4)}"
                : "INVALID_KEY";
            Debug.Log($"[DEBUG] Using API Key (masked): {maskedKey}");
        }
        else
        {
            Debug.LogError("[DEBUG] API Key is NULL or empty!");
        }

        // 2. 构建请求体并打印（只显示前200个字符用于刷新）
        string requestBody = BuildRequestBody();
        Debug.Log($"[DEBUG] Request Body (first 300 chars):\n{requestBody.Substring(0, Mathf.Min(300, requestBody.Length))}");

        // 3. 创建请求并设置Header
        var request = CreateRequest(requestBody);

        // 4. 发送请求
        yield return request.SendWebRequest();

        // 5. 处理响应
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ERROR] WebRequest failed:");
            Debug.LogError($"  - Result: {request.result}");
            Debug.LogError($"  - Error Message: {request.error}");
            Debug.LogError($"  - HTTP Status Code: {request.responseCode}");
            Debug.LogError($"  - Response Body:\n{request.downloadHandler?.text ?? "(null)"}");

            // 错误诊断：检查是否是URL错误或401/404
            if (request.responseCode == 404)
            {
                Debug.LogError(">>> Likely cause: Missing '/v1/' in API URL!");
            }
            else if (request.responseCode == 401)
            {
                Debug.LogError(">>> Likely cause: Invalid API Key OR wrong service (e.g., using OpenAI key on DeepSeek)");
            }
        }
        else
        {
            Debug.Log("[SUCCESS] Received response from server.");
            var responseText = request.downloadHandler.text;
            Debug.Log($"[RESPONSE] Full response:\n{responseText}");

            try
            {
                // 解析JSON响应
                var jsonResponse = JObject.Parse(responseText);
                var messageContent = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrEmpty(messageContent))
                {
                    Debug.LogError("[ERROR] No 'content' found in AI response!");
                    Debug.Log($"[DEBUG] Parsed JSON keys: {string.Join(", ", jsonResponse.Properties().Select(p => p.Name))}");
                }
                else
                {
                    // 将AI回复添加到聊天历史
                    chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });
                    PrintChatMessage(chatMessages);

                    // 保存到转录文本
                    transcript += $"Patient:\n{messageContent}\n\n";
                    PlayerPrefs.SetString("interviewScripts", transcript);
                    PlayerPrefs.Save();

                    // 调用TTS生成语音
                    if (sitTTSManager.Instance != null)
                    {
                        sitTTSManager.Instance.ConvertTextToSpeech(messageContent);
                    }
                    else
                    {
                        Debug.LogWarning("sitTTSManager not found – skipping TTS.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JSON PARSE ERROR]: {ex.Message}\nResponse was:\n{responseText}");
            }
        }

        // 等待冷却时间
        yield return new WaitForSeconds(requestCooldown);
        isRequestInProgress = false;
        Debug.Log("=== [DEBUG] PostRequest finished ===");
    }

    /// <summary>
    /// 构建请求体
    /// </summary>
    /// <returns>JSON格式的请求体字符串</returns>
    private string BuildRequestBody()
    {
        // 匿名类型：创建请求对象
        var requestObject = new
        {
            model = "deepseek-chat", // 模型名称
            messages = chatMessages,
            temperature = 0.7f,
            max_tokens = 1500
        };
        return JsonConvert.SerializeObject(requestObject);
    }

    /// <summary>
    /// 创建HTTP请求
    /// </summary>
    /// <param name="requestBody">请求体字符串</param>
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
    /// 打印聊天消息到调试控制台
    /// </summary>
    /// <param name="messages">聊天消息列表</param>
    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        if (messages.Count == 0)
            return;

        // 获取最新消息
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

        // 输出到控制台
        Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
    }
}
