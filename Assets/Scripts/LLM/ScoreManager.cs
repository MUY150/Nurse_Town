using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 评分管理器，负责记录对话回合并使用GPT-4进行完整对话评估
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 单例模式（Singleton）：使用静态Instance字段
/// - 协程（Coroutine）：使用IEnumerator和yield return
/// - 列表集合（List）：存储对话回合
/// - StringBuilder：高效字符串构建
/// - [Serializable]特性：标记类可被序列化
/// - 属性（Property）：自动属性
/// - 泛型：List<T>、Dictionary<K,V>
/// - Unity生命周期方法：Awake()、Update()
/// - 输入系统：Input.GetKeyDown检测按键
/// - JSON序列化：JsonConvert处理JSON数据
/// - 字符串插值：$""语法
/// </remarks>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private string scoringPrompt = "";
    private string currentScenario = "";
    private List<ConversationTurn> conversationTurns = new List<ConversationTurn>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E key pressed — submitting full evaluation.");
            SubmitEvaluation();
        }
    }

    /// <summary>
    /// 初始化评分管理器
    /// </summary>
    /// <param name="scenario">场景名称</param>
    public void Initialize(string scenario)
    {
        currentScenario = scenario;
        LoadScoringPrompt();
    }

    /// <summary>
    /// 从文件加载评分提示词
    /// </summary>
    private void LoadScoringPrompt()
    {
        string promptPath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario, "scoringPrompt.txt");
        if (!File.Exists(promptPath))
        {
            Debug.LogError("Scoring prompt file not found: " + promptPath);
            scoringPrompt = "";
            return;
        }
        scoringPrompt = File.ReadAllText(promptPath);
        Debug.Log("Scoring prompt loaded successfully.");
    }

    /// <summary>
    /// 记录对话回合
    /// </summary>
    /// <param name="patientResponse">病人响应</param>
    /// <param name="nurseResponse">护士响应</param>
    public void RecordTurn(string patientResponse, string nurseResponse)
    {
        conversationTurns.Add(new ConversationTurn
        {
            Patient = patientResponse,
            Nurse = nurseResponse
        });
        Debug.Log($"Turn {conversationTurns.Count} recorded.");
    }

    /// <summary>
    /// 提交完整对话进行评估
    /// </summary>
    public void SubmitEvaluation()
    {
        if (conversationTurns.Count == 0)
        {
            Debug.LogWarning("No conversation turns recorded.");
            return;
        }
        StartCoroutine(EvaluateFullConversationCoroutine());
    }

    /// <summary>
    /// 评估完整对话的协程
    /// </summary>
    private IEnumerator EvaluateFullConversationCoroutine()
    {
        if (string.IsNullOrEmpty(scoringPrompt))
        {
            Debug.LogWarning("Scoring prompt not loaded.");
            yield break;
        }

        StringBuilder conversationBuilder = new StringBuilder();
        for (int i = 0; i < conversationTurns.Count; i++)
        {
            conversationBuilder.AppendLine($"Turn {i + 1}:");
            conversationBuilder.AppendLine($"Patient: \"{conversationTurns[i].Patient}\"");
            conversationBuilder.AppendLine($"Nursing Student: \"{conversationTurns[i].Nurse}\"");
            conversationBuilder.AppendLine();
        }

        string fullPrompt = $"{scoringPrompt}\n\nNow analyze the following full simulated conversation between the patient and nursing student:\n{conversationBuilder}";
        Debug.Log($"Prompt length: {fullPrompt.Length} characters");
        Debug.Log($"Estimated tokens (rough): {fullPrompt.Length / 4} tokens");
        var requestBody = new
        {
            model = "gpt-4",
            messages = new List<Dictionary<string, string>>()
            {
                new Dictionary<string, string>() { { "role", "user" }, { "content", fullPrompt } }
            },
            temperature = 0.0,
            max_tokens = 1500
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        var request = new UnityWebRequest(OpenAIRequest.Instance.apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + OpenAIRequest.Instance.apiKey);

        Debug.Log("Submitting full conversation for evaluation...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("OpenAI Request Error: " + request.error);
            yield break;
        }

        var jsonResponse = JObject.Parse(request.downloadHandler.text);
        string responseContent = jsonResponse["choices"][0]["message"]["content"].ToString();

        try
        {
            var evaluation = JsonConvert.DeserializeObject<DynamicEvaluationResult>(responseContent);
            Debug.Log("===== FINAL EVALUATION =====");
            foreach (var criterion in evaluation.criteria)
            {
                Debug.Log($"[{criterion.name}] Score: {criterion.score}/{criterion.maxScore} — {criterion.explanation}");
            }
            Debug.Log($"Total Score: {evaluation.totalScore}");
            Debug.Log($"Performance Level: {evaluation.performanceLevel}");
            Debug.Log($"Overall Summary: {evaluation.overallExplanation}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse evaluation JSON: " + ex.Message);
        }
    }
}

/// <summary>
/// 对话回合数据类
/// </summary>
[Serializable]
public class ConversationTurn
{
    public string Patient;
    public string Nurse;
}

/// <summary>
/// 动态评估结果数据类
/// </summary>
[Serializable]
public class DynamicEvaluationResult
{
    public List<CriterionScore> criteria;
    public int totalScore;
    public string performanceLevel;
    public string overallExplanation;
}

/// <summary>
/// 评分标准数据类
/// </summary>
[Serializable]
public class CriterionScore
{
    public string name;
    public int score;
    public int maxScore;
    public string explanation;
}
