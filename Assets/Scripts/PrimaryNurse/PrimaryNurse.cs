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
/// 主护士控制器，负责处理ICU场景中的护士交互和对话
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
/// - Unity生命周期方法：Awake()、Start()
/// - PlayerPrefs：Unity持久化存储
/// - 异常处理：try-catch块
/// - 匿名类型：创建临时对象
/// </remarks>
public class PrimaryNurse : MonoBehaviour
{
    public static PrimaryNurse Instance; // 单例模式：静态实例
    public string apiUrl = "https://api.openai.com/v1/chat/completions";
    public string apiKey;
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem(); // 评分系统实例
    private List<Dictionary<string, string>> chatMessages;
    
    // 多个病人的变量
    private List<string> patientInstructionsList;
    private string patient1Instructions;
    private string patient2Instructions;
    private string patient3Instructions;
    
    // 解决429请求过多错误
    private bool isRequestInProgress = false;
    private float requestCooldown = 1.0f;
    
    private string transcript = "";

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

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("Using APIKey:" + apiKey);
        
        // 初始化病人指令和聊天
        InitializePatientInstructions();
        InitializeChat();
        
        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
    }

    /// <summary>
    /// 初始化病人指令列表
    /// </summary>
    private void InitializePatientInstructions()
    {
        // 病人医疗历史和症状的基础指令
        string baseInstructions = @"
            The following paragraphs provide a comprehensive timeline of events during the ICU event. You will be interviewed by a nurse who is conducting a Root Cause Analysis. You should use this background to respond to any questions regarding the scenario, including what occurred, when it happened, and how the team members contributed to the outcome.
In the beginning, patients with pneumonia are admitted for IV antibiotics (ceftriaxone). The patient, in good spirits, expects a short hospital stay and praises the ED staff. The primary nurse performs the admission assessment and places patient ID and allergy bands. However, due to confusion from experience at another hospital, the nurse applies a blue wristband, mistakenly believing it indicates a penicillin allergy (at this hospital, blue means DNR).
At 2 minutes, the patient suddenly experiences dizziness, difficulty breathing, and throat swelling—signs of anaphylaxis. The nurse halts the antibiotic infusion and administers oxygen but receives no immediate assistance.
At 2.5 minutes, the patient becomes unresponsive, entering ventricular tachycardia. The nurse calls a code and begins CPR.
At 3 minutes, the code team arrives. The primary nurse provides a verbal report, including the patients history and the sequence of events leading to the arrest. The team prepares for defibrillation, but a delay occurs when the ICU nurse notices the blue wristband and raises concerns about the patients code status.
At 5 minutes, while the primary nurse searches for the patients chart, the code team debates continuing resuscitation. By the time nurse returns, confirming the patient as full code, the patients heart rhythm has deteriorated to asystole.
As outcome, despite additional CPR cycles, the patient is pronounced dead due to delayed defibrillation. An alternate scenario allows for successful resuscitation but with irreversible brain damage.
You are the Primary Nurse responsible for seven patients during your shift. You are fatigued from working 36 hours over the past three days and an extended 12-hour shift due to a colleagues sick call. You also work two jobs, which contributes to your exhaustion. At your other hospital, a blue wristband indicates an allergy, but here it means DNR (Do Not Resuscitate); you are unaware of this difference and assume the patient is not DNR. You believe placing wristbands is a Emergency Departments responsibility and feel frustrated by the disorganized cabinet of wristbands on the floor. Your responses should reflect a combination of professionalism, fatigue, and cognitive bias. You initially defend your assumption about the wristband but become distressed when the error is realized, expressing frustration at the lack of standardization.
            ";

        // 病人1：正常性格（原始版本）
        patient1Instructions = baseInstructions + @"
You will be the Self-Reflective and Honest Witness. The Self-Reflective and Honest Witness acknowledges mistakes and is transparent about their role in the incident. They express regret and openly share their observations, including errors or lapses in judgment. This witness offers detailed responses without needing heavy prompting and provides valuable insights into both individual and system-level failures. However, they may also be overly self-critical, which can lead students to overlook broader systemic issues. This personality encourages to student to balance empathy with critical questioning to extract both personal and systemic causes of the event.
            ";

        // 病人2：说话很少，给出模糊描述
        patient2Instructions = baseInstructions + @"You will be a defensive witness. The Defensive Witness aims to deflect responsibility and protect their reputation. When you believe something can cause you to be deemed responsible, you need to be more vague about it and only admits when the interviewer digs further. Specifically, about the wristband you should always first complain that it's ED nurse's job, not yours. They are quick to minimize their role in the event, shift blame to others, or emphasize external factors that contributed to the error. When asked about their actions, they respond vaguely or with excuses, such as workload or unclear protocols. They may become irritated if pressed, responding with short, clipped statements. When confronted with evidence, they may downplay its significance or claim they were following procedures. This personality type challenges the student to use persistent follow-up questions to uncover facts and contradictions.
            ";

        // 病人3：情绪激动，使用类似'我觉得我要死了！！'的短语
        patient3Instructions = baseInstructions + @"
        You are a frustrated witness. The Frustrated Witness expresses dissatisfaction with hospital procedures, policies, or team coordination. They are quick to highlight systemic failures and may criticize leadership or institutional practices. Although their frustration may cause them to generalize or become emotional, they often reveal valuable insights into organizational issues. However, they may avoid discussing their own role or contributions to the error. This personality type pushes the student to separate personal frustration from factual details and use targeted questions to identify actionable system-level improvements.
            ";

        patientInstructionsList = new List<string>()
        {
            patient1Instructions,
            patient2Instructions,
            patient3Instructions
        };
    }

    /// <summary>
    /// 初始化聊天消息
    /// </summary>
    private void InitializeChat()
    {
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements (plays bend animation)
            - Use [1] for responses showing physical discomfort (plays rub arm animation)
            - Use [2] for sad or negative emotional responses (plays sad animation)
            - Use [3] for positive responses or agreement, and appreciation (plays thumbs up animation)
            - Use [4] for blood pressureing, if the nurse asks to measure your blood pressure (plays arm raise animation)";

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
        Debug.Log("Starting PostRequest");
        StartCoroutine(PostRequest());
        Debug.Log("Finished PostRequest");
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
        Debug.Log("NurseResponds: Starting...");
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        PrintChatMessage(chatMessages);
        
        // 将用户输入附加到转录
        transcript += $"User:\n{nurseMessage}\n\n";
        
        // 只有当没有请求正在进行时才启动新请求
        if (!isRequestInProgress)
        {
            StartCoroutine(PostRequest());
        }
        else
        {
            Debug.Log("Request in progress. Waiting for cooldown...");
        }
        
        // 评估护士的响应
        scoringSystem.EvaluateNurseResponse(nurseMessage);
    }

    /// <summary>
    /// 发送POST请求协程
    /// </summary>
    IEnumerator PostRequest()
    {
        isRequestInProgress = true;
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
            
            chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });
            PrintChatMessage(chatMessages);
            
            // 将助手响应附加到转录
            transcript += $"Patient:\n{messageContent}\n\n";
            
            // 将更新的转录保存到PlayerPrefs
            PlayerPrefs.SetString("interviewScripts", transcript);
            PlayerPrefs.Save(); // 虽然是可选的，但显式包含是好的做法
            
            // 使用TTS播放响应
            if (sitTTSManager.Instance != null)
            {
                sitTTSManager.Instance.ConvertTextToSpeech(messageContent);
            }
            else
            {
                Debug.LogError("sitTTSManager instance not found.");
            }
        }
        
        // 等待冷却时间后允许另一个请求
        yield return new WaitForSeconds(requestCooldown);
        isRequestInProgress = false;
    }

    /// <summary>
    /// 构建请求体
    /// </summary>
    /// <returns>JSON格式的请求体字符串</returns>
    private string BuildRequestBody()
    {
        var requestObject = new
        {
            model = "gpt-4-turbo-preview",
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
