using Newtonsoft.Json.Linq;
using UnityEngine;

public class SpeakTool : ITool
{
    public string Name => "speak";
    
    public string Description => "Speak to the nurse with specified emotional tone and speech parameters. The emotion parameter controls voice tone, NOT body animation. Use 'act' tool for body animations.";

    public JObject ParametersSchema => new JObject
    {
        ["type"] = "object",
        ["properties"] = new JObject
        {
            ["text"] = new JObject
            {
                ["type"] = "string",
                ["description"] = "The text to speak in Chinese (中文)"
            },
            ["emotion"] = new JObject
            {
                ["type"] = "string",
                ["enum"] = new JArray(
                    "neutral",
                    "anxious",
                    "painful",
                    "relieved",
                    "worried",
                    "grateful",
                    "frustrated",
                    "hopeful"
                ),
                ["description"] = "Emotional tone for voice synthesis. This affects HOW you speak, not body movements. Use 'act' tool for body animations."
            },
            ["speech_rate"] = new JObject
            {
                ["type"] = "number",
                ["minimum"] = 0.5,
                ["maximum"] = 1.5,
                ["default"] = 1.0,
                ["description"] = "Speech speed. 0.7=slow (painful, elderly), 1.0=normal, 1.2=fast (anxious, excited)"
            }
        },
        ["required"] = new JArray("text", "emotion")
    };

    public ToolResult Execute(JObject parameters)
    {
        try
        {
            string text = parameters["text"]?.ToString();
            string emotion = parameters["emotion"]?.ToString() ?? "neutral";
            float speechRate = parameters["speech_rate"]?.Value<float>() ?? 1.0f;
            
            if (string.IsNullOrEmpty(text))
            {
                return ToolResult.ErrorResult("Text parameter is required");
            }

            float adjustedSpeechRate = GetAdjustedSpeechRate(emotion, speechRate);

            if (TTSManager.Instance != null)
            {
                TTSManager.Instance.ConvertTextToSpeech(text, emotion, adjustedSpeechRate);
                Debug.Log($"[SpeakTool] TTS: {text.Substring(0, System.Math.Min(50, text.Length))}... [emotion={emotion}, speed={adjustedSpeechRate}]");
            }
            else
            {
                Debug.LogWarning("[SpeakTool] TTSManager not available");
            }

            var speakEvent = new SpeakExecutedEvent
            {
                Timestamp = System.DateTime.Now,
                Text = text,
                Emotion = emotion,
                SpeechRate = adjustedSpeechRate
            };
            LlmEventBus.Publish(speakEvent);

            return ToolResult.SuccessResult($"Spoke with emotion '{emotion}' at rate {adjustedSpeechRate}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SpeakTool] Error: {e.Message}");
            return ToolResult.ErrorResult(e.Message);
        }
    }

    private float GetAdjustedSpeechRate(string emotion, float baseRate)
    {
        float emotionModifier = emotion switch
        {
            "painful" => 0.8f,
            "anxious" => 1.15f,
            "worried" => 0.95f,
            "relieved" => 1.0f,
            "grateful" => 1.0f,
            "frustrated" => 0.9f,
            "hopeful" => 1.05f,
            _ => 1.0f
        };

        return Mathf.Clamp(baseRate * emotionModifier, 0.5f, 1.5f);
    }
}
