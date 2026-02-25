using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 身体动作控制器，负责处理病人的手势和表情动画
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 协程（Coroutine）：使用IEnumerator和yield return
/// - 字典集合（Dictionary）：存储聊天消息
/// - 正则表达式（Regex）：提取情绪代码
/// - JSON序列化：使用JsonConvert处理JSON数据
/// - 字符串插值：$""语法
/// - Unity生命周期方法：Start()
/// - 匿名类型：创建临时对象
/// - using语句：自动资源管理（UnityWebRequest）
/// - 泛型：List<T>、Dictionary<K,V>
/// </remarks>
public class BodyMove : MonoBehaviour
{
    private string chatApiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;
    private CharacterAnimationController animationController;

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        // Debug.Log("APIKey:" + apiKey);
        animationController = GetComponent<CharacterAnimationController>();
        InitializeChat();
    }

    /// <summary>
    /// 初始化聊天消息
    /// </summary>
    private void InitializeChat()
    {
        string emotionInstructions = 
            "IMPORTANT: You must end EVERY response with one of these emotion codes: [0], [1], or [2]\n" +
            "- Use [0] for neutral responses or statements\n" +
            "- Use [1] for responses involving pain, discomfort, symptoms, or negative feelings\n" +
            "- Use [2] for positive responses, gratitude, or when feeling better\n";

        string baseInstructions = 
            "You are a patient NPC in a hospital, interacting with a nursing student. " +
            "Respond with brief, natural answers about your symptoms or feelings. " +
            "Keep responses concise and focused on your condition.";
        string scenario = 
            "Background\n" +
            "you are Mrs. Johnson, a 62-year-old female who was admitted to the hospital with severeheadache and dizziness.\n" +
            "She has a 5-year history of hypertension and has been on antihypertensive medications.though she occasionally misses doses due to forgetfulness.\n" +
            "Family history includes hypertension and heart disease (mother and brother)\n" +
            "She works as a school teacher and lives with her husband.\n" +
            "She has a sedentary lifestyle and enjoys watching TV in her spare time\n" +
            "Tone and Personality\n" +
            "Speak with a polite and cooperative tone, as Mrs. Johnson is generally compliant and\n" +
            "concerned about her health.\n" +
            "Express some mild anxiety about her current symptoms, as the headache and dizzinessare more severe than what she usually experiences.\n" +
            "Occasionally show a bit of forgetfuiness or hesitation when recaling specific medicationdetails, indicating a realistic portrayal of a patient who isn't fully adherent to herprescribed regimen.";

        chatMessages = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", $"{baseInstructions}\n\n{emotionInstructions}\n\n{scenario}" }
            },
        };
        
        PrintChatMessage(chatMessages);
    }

    /// <summary>
    /// 玩家响应处理
    /// </summary>
    /// <param name="playerMessage">玩家消息</param>
    public void PlayerResponds(string playerMessage)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", playerMessage } });
        StartCoroutine(SendChatRequest());
        PrintChatMessage(chatMessages);
        
    }

    /// <summary>
    /// 发送聊天请求的协程
    /// </summary>
    private IEnumerator SendChatRequest()
    {
        var requestObject = new
        {
            model = "gpt-4",
            messages = chatMessages,
            max_tokens = 150,
            temperature = 0.7f
        };

        string requestBody = JsonConvert.SerializeObject(requestObject);
        
        using (UnityWebRequest request = new UnityWebRequest(chatApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var jsonResponse = JObject.Parse(request.downloadHandler.text);
                var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();
                
                UpdateAnimation(messageContent);
                chatMessages.Add(new Dictionary<string, string>() 
                { 
                    { "role", "assistant" }, 
                    { "content", messageContent } 
                });
                PrintChatMessage(chatMessages);
            }
            else
            {
                Debug.LogError($"Chat API Error: {request.error}");
            }
        }
    }

    /// <summary>
    /// 根据消息内容更新动画
    /// </summary>
    /// <param name="message">消息内容</param>
    private void UpdateAnimation(string message)
    {
        Match match = Regex.Match(message, @"\[([012])\]$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch(emotionCode)
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
            }
        }
        else
        {
            Debug.LogWarning($"No emotion code found: {message}");
            animationController.PlayIdle();
        }
    }

    /// <summary>
    /// 打印聊天消息
    /// </summary>
    /// <param name="messages">消息列表</param>
    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        Debug.Log("══════════════ Chat Messages Log ══════════════");
        
        foreach (var message in messages)
        {
            string role = message["role"];
            string content = message["content"];
            
            // 提取情绪代码（如果存在）
            string emotionCode = "";
            var match = System.Text.RegularExpressions.Regex.Match(content, @"\[([012])\]$");
            if (match.Success)
            {
                emotionCode = $" (Emotion: {match.Groups[1].Value})";
            }
            
            Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
        }
        
        Debug.Log("══════════════ End Chat Log ══════════════");
    }
}
