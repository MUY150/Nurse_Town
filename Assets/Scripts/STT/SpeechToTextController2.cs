using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// 语音转文本控制器V2，负责录制音频并使用OpenAI Whisper API进行语音识别
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - async/await异步编程：用于异步HTTP请求
/// - Task异步操作：表示异步操作
/// - using语句：自动资源管理（HttpClient和FileStream）
/// - 异常处理：try-catch块
/// - 字符串插值：$""语法
/// - Unity生命周期方法：Start()、Update()
/// - 输入系统：Input.GetKeyDown/Up检测按键
/// - 文件I/O：Path.Combine、File.Delete
/// - JSON序列化：JsonConvert处理JSON数据
/// - 泛型：HttpClient<T>、Task<T>
/// </remarks>
public class SpeechToTextController2 : MonoBehaviour
{
    public TextMeshProUGUI transcriptText;
    private bool isRecording = false;
    private AudioClip recordedClip;
    private string openAiApiKey;

    void Start()
    {
        openAiApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartRecording();
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            StopRecordingAndTranscribe();
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    private void StartRecording()
    {
        if (!isRecording)
        {
            recordedClip = Microphone.Start(null, false, 10, 44100);
            isRecording = true;
        }
    }

    /// <summary>
    /// 停止录音并转录
    /// </summary>
    private void StopRecordingAndTranscribe()
    {
        if (isRecording)
        {
            Microphone.End(null);
            isRecording = false;
            _ = TranscribeAudio();
        }
    }

    /// <summary>
    /// 转录音频的异步方法
    /// </summary>
    private async Task TranscribeAudio()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "recordedAudio.wav");
        SavWav.Save("recordedAudio.wav", recordedClip);

        string transcription = await SendToWhisperAPI(filePath, "whisper-1", "en", "json", 0.2f);

        transcriptText.text = transcription;

        File.Delete(filePath);

        if (OpenAIRequest.Instance != null)
        {
            OpenAIRequest.Instance.ReceiveNurseTranscription(transcription);
        }
        else
        {
            Debug.LogError("OpenAIRequest instance not found.");
        }
    }

    /// <summary>
    /// 发送音频到Whisper API进行转录
    /// </summary>
    private async Task<string> SendToWhisperAPI(string filePath, string model, string language, string responseFormat, float temperature)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAiApiKey);

            using (var form = new MultipartFormDataContent())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var audioContent = new StreamContent(fileStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                form.Add(audioContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent(model), "model");

                if (!string.IsNullOrEmpty(language))
                    form.Add(new StringContent(language), "language");

                form.Add(new StringContent(responseFormat), "response_format");
                form.Add(new StringContent(temperature.ToString()), "temperature");

                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transcriptionResponse = JsonConvert.DeserializeObject<TranscriptionResponse>(responseContent);
                    return transcriptionResponse.text;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError("Transcription failed: " + response.ReasonPhrase + " - " + errorContent);
                    return "Error in transcription";
                }
            }
        }
    }

    /// <summary>
    /// 转录响应数据类
    /// </summary>
    private class TranscriptionResponse
    {
        public string text { get; set; }
    }
}
