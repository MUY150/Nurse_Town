using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 评分系统，负责实时评估护士的响应并生成评分报告
/// </summary>
/// <remarks>
/// C#特性说明：
/// - 普通类（非MonoBehaviour）：不继承Unity基类的独立类
/// - 协程（Coroutine）：使用IEnumerator和yield return
/// - 列表集合（List）：存储评分原因
/// - 异常处理：try-catch块
/// - JSON序列化：JsonConvert处理JSON数据
/// - 字符串插值：$""语法
/// - 属性（Property）：自动属性
/// - 泛型：List<T>
/// - 匿名类型：创建临时对象
/// </remarks>
public class ScoringSystem
{
    public int totalScore = 5;
    public int interactionCount = 0;

    // 存储加分/扣分的原因
    private List<string> pointsAddedReasons = new List<string>();
    private List<string> pointsDeductedReasons = new List<string>();

    /// <summary>
    /// 评估护士的响应
    /// </summary>
    /// <param name="nurseResponse">护士响应内容</param>
    public void EvaluateNurseResponse(string nurseResponse)
    {
        // 使用OpenAIRequest.Instance
        OpenAIRequest.Instance.StartCoroutine(EvaluateResponseCoroutine(nurseResponse));
    }

    /// <summary>
    /// 评估响应的协程
    /// </summary>
    private IEnumerator EvaluateResponseCoroutine(string nurseResponse)
    {

        string prompt = $"You are an expert nursing instructor. Evaluate the following nurse's response based on the criteria provided. Provide a JSON object with the evaluation results.\n\n" +
                        $"Nurse's Response: \"{nurseResponse}\"\n\n" +
                        "Scoring Criteria:\n" +
                        "- Deduct 1 point if medical jargon was used without explanation.\n" +
                        "- Add 2 points if the nurse mentions printing a list or sending an email.\n" +
                        "- Add 2 points if the nurse mentions the \"0-10 scale\" of pain or other discomfort.\n\n" +
                        "Provide the output in the following JSON format:\n" +
                        "{\n" +
                        "  \"pointsAdded\": <number>,\n" +
                        "  \"pointsDeducted\": <number>,\n" +
                        "  \"reason\": \"<detailed explanation of what the nurse did well or could improve>\"\n" +
                        "}";

        // 创建OpenAI请求
        var requestBody = new
        {
            model = "gpt-4",
            messages = new List<Dictionary<string, string>>()
            {
                new Dictionary<string, string>() { { "role", "user" }, { "content", prompt } }
            },
            temperature = 0.0,
            max_tokens = 150
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        var request = new UnityWebRequest(OpenAIRequest.Instance.apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + OpenAIRequest.Instance.apiKey);

        yield return request.SendWebRequest();

        interactionCount++; 

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response Body: " + request.downloadHandler.text);
        }
        else
        {
            
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            var responseContent = jsonResponse["choices"][0]["message"]["content"].ToString();

            
            try
            {
                var evaluationResult = JsonConvert.DeserializeObject<EvaluationResult>(responseContent);
                int pointsAdded = evaluationResult.pointsAdded;
                int pointsDeducted = evaluationResult.pointsDeducted;
                string reason = evaluationResult.reason;

                totalScore += pointsAdded - pointsDeducted;

                
                if (pointsAdded > 0)
                {
                    pointsAddedReasons.Add(reason);
                    Debug.Log($"Well done! {pointsAdded} points added because {reason}");
                }
                if (pointsDeducted > 0)
                {
                    pointsDeductedReasons.Add(reason);
                    Debug.Log($"{pointsDeducted} points deducted because {reason}");
                }
                if (pointsAdded == 0 && pointsDeducted == 0)
                {
                    Debug.Log("Good! Keep going!");
                }

                
                Debug.Log($"This was your {interactionCount} response.");

            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse evaluation result: " + ex.Message);
            }
        }

        // 如果对话回合数>=5，生成报告
        if (interactionCount >= 5)
        {
            GenerateReport();
        }
    }

    /// <summary>
    /// 生成评估报告
    /// </summary>
    public void GenerateReport()
    {
        Debug.Log("===== Evaluation Report =====");
        Debug.Log($"Total Score: {totalScore}");

        
        Debug.Log("Things you did well:");
        foreach (var reason in pointsAddedReasons)
        {
            Debug.Log("- " + reason);
        }

        
        Debug.Log("Things you could improve:");
        foreach (var reason in pointsDeductedReasons)
        {
            Debug.Log("- " + reason);
        }

        Debug.Log("=============================");

        // 重置
        totalScore = 5;
        pointsAddedReasons.Clear();
        pointsDeductedReasons.Clear();
        interactionCount = 0;
    }
}


/// <summary>
/// 评估结果数据类
/// </summary>
public class EvaluationResult
{
    public int pointsAdded;
    public int pointsDeducted;
    public string reason;
}
