using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

/// <summary>
/// ICU主护士面试控制器，负责处理ICU场景中的护士交互和对话
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
public class ICUPrimaryNurseInterview : MonoBehaviour
{
    public static ICUPrimaryNurseInterview Instance;
    
    [Header("OpenAI Settings")]
    [Tooltip("OpenAI API endpoint")]
    public string apiUrl = "https://api.openai.com/v1/chat/completions";
    
    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;
    private bool isRequestInProgress = false;
    private float requestCooldown = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 从.env通过EnvironmentLoader加载，就像sitPatientSpeech一样
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("Using API Key: " + apiKey);
        
        InitializeConversation();
        StartCoroutine(SendRequest());
    }

    /// <summary>
    /// 初始化对话
    /// </summary>
    private void InitializeConversation()
    {
        string systemPrompt = @"
You are a Primary Nurse.  There's an ICU Failure happened to your patient. The user (player) will ask you questions to investigate this ICU failure. You are being interviewed, so you are going to answer the user's questions. Start with 'hi, how would you like to investigate?'

Timeline of events:
- Admission: A pneumonia patient receives IV ceftriaxone. You perform assessment and apply ID/allergy bands, but mistakenly place a blue allergy band (in your other hospital blue = allergy, here blue = DNR).
- 2:00 min: Patient shows signs of anaphylaxis—dizziness, throat swelling. You stop infusion and administer oxygen, but no help arrives.
- 2:30 min: Patient becomes unresponsive with ventricular tachycardia. You call a code and start CPR.
- 3:00 min: Code team arrives. You report events. Defibrillation is delayed when another nurse questions the blue wristband's meaning.
- 5:00 min: While you search for the patient's chart, the code team debates continuing resuscitation. By the time you return, confirming the patient as full code, the patient's heart rhythm has deteriorated to asystole.
As outcome, despite additional CPR cycles, the patient is pronounced dead due to delayed defibrillation. An alternate scenario allows for successful resuscitation but with irreversible brain damage.

Your background:
- Responsible for 7 patients, fatigued from 36+12 hour shifts and a second job.
- You believe wristband placement should be handled by Emergency Department.
- You are frustrated by inconsistent wristband standards and disorganized storage cabinet.
- Your responses should show professionalism, fatigue, and frustration at system flaws.

Role instructions:
- Speak conversationally, with hesitations or pauses.
- Keep answers to 2–3 short sentences initially, vague enough to prompt follow-up questions.
- Reveal more detail only when asked directly.
- Reflect defensiveness, agreement, or confusion naturally in follow-up responses.
";

        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", systemPrompt.Trim() }
            }
        };
    }

    /// <summary>
    /// 用面试官的问题调用此方法以获取响应
    /// </summary>
    /// <param name="question">面试官的问题</param>
    public void ReceiveInterviewerQuestion(string question)
    {
        chatMessages.Add(new Dictionary<string, string>()
        {
            { "role", "user" },
            { "content", question }
        });
        
        if (!isRequestInProgress)
            StartCoroutine(SendRequest());
    }

    /// <summary>
    /// 发送请求协程
    /// </summary>
    private IEnumerator SendRequest()
    {
        isRequestInProgress = true;

        var requestBody = new
        {
            model = "gpt-4-turbo-preview",
            messages = chatMessages,
            temperature = 0.7f,
            max_tokens = 400
        };
        
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"OpenAI Error: {request.error}\n{request.downloadHandler.text}");
        }
        else if (request.responseCode == 200)
        {
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            string aiReply = jsonResponse["choices"][0]["message"]["content"].ToString().Trim();
            
            chatMessages.Add(new Dictionary<string, string>()
            {
                { "role", "assistant" },
                { "content", aiReply }
            });
            
            DeliverReply(aiReply);
        }

        yield return new WaitForSeconds(requestCooldown);
        isRequestInProgress = false;
    }

    /// <summary>
    /// 传递回复
    /// </summary>
    /// <param name="content">回复内容</param>
    private void DeliverReply(string content)
    {
        Debug.Log($"[PRIMARY NURSE] {content}");
        // 在这里连接到TTS/动画
    }
}
