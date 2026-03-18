using Newtonsoft.Json.Linq;
using UnityEngine;

public class CompleteSessionTool : ITool
{
    public string Name => "complete_session";
    
    public string Description => "Call this when the nurse has correctly identified the patient's condition and provided appropriate care. This ends the session.";

    public JObject ParametersSchema => new JObject
    {
        ["type"] = "object",
        ["properties"] = new JObject
        {
            ["diagnosis_correct"] = new JObject
            {
                ["type"] = "boolean",
                ["description"] = "Whether the nurse's diagnosis was correct"
            },
            ["summary"] = new JObject
            {
                ["type"] = "string",
                ["description"] = "Summary of what the nurse did well and areas for improvement (in Chinese)"
            },
            ["key_observations"] = new JObject
            {
                ["type"] = "array",
                ["items"] = new JObject { ["type"] = "string" },
                ["description"] = "Key observations the nurse made"
            },
            ["missed_points"] = new JObject
            {
                ["type"] = "array",
                ["items"] = new JObject { ["type"] = "string" },
                ["description"] = "Important points the nurse missed"
            }
        },
        ["required"] = new JArray("diagnosis_correct", "summary")
    };

    public ToolResult Execute(JObject parameters)
    {
        try
        {
            bool diagnosisCorrect = parameters["diagnosis_correct"]?.Value<bool>() ?? false;
            string summary = parameters["summary"]?.ToString();
            var keyObservations = parameters["key_observations"]?.ToObject<string[]>() ?? new string[0];
            var missedPoints = parameters["missed_points"]?.ToObject<string[]>() ?? new string[0];

            var sessionEvent = new SessionCompleteEvent
            {
                Timestamp = System.DateTime.Now,
                DiagnosisCorrect = diagnosisCorrect,
                Summary = summary,
                KeyObservations = keyObservations,
                MissedPoints = missedPoints
            };

            LlmEventBus.Publish(sessionEvent);

            Debug.Log($"[CompleteSessionTool] Session complete. Diagnosis correct: {diagnosisCorrect}");

            return ToolResult.SuccessResult("Session completed", JObject.FromObject(new
            {
                diagnosis_correct = diagnosisCorrect,
                summary = summary
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CompleteSessionTool] Error: {e.Message}");
            return ToolResult.ErrorResult(e.Message);
        }
    }
}
