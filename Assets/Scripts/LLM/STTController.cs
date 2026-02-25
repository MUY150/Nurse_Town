using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 语音转文本控制器，负责录制音频并使用OpenAI Whisper API进行语音识别
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
public class STTController : MonoBehaviour
{
    public TextMeshProUGUI transcriptText;
    private bool isRecording = false;
    private AudioClip recordedClip;
    private BodyMove bodyMove;
    private string openAiApiKey;
    private string lastTranscription;

    void Start()
    {
        openAiApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        bodyMove = FindObjectOfType<BodyMove>();
        // Debug.Log("APIKey loaded (hidden for security)");
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
            transcriptText.text = "Recording...";
        }
    }

    /// <summary>
    /// 停止录音并开始转录
    /// </summary>
    private void StopRecordingAndTranscribe()
    {
        if (isRecording)
        {
            Microphone.End(null);
            isRecording = false;
            transcriptText.text = "Processing...";
            _ = TranscribeAudio();
        }
    }

    /// <summary>
    /// 转录音频为文本（异步方法）
    /// </summary>
    private async Task TranscribeAudio()
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "recordedAudio.wav");
            SavWav.Save("recordedAudio.wav", recordedClip);

            lastTranscription = await SendToWhisperAPI(filePath, "whisper-1");
            transcriptText.text = "You: " + lastTranscription;
            
            if (bodyMove != null)
            {
                bodyMove.PlayerResponds(lastTranscription);
            }
            
            File.Delete(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in transcription: {e.Message}");
            transcriptText.text = "Error in transcription. Please try again.";
        }
    }

    /// <summary>
    /// 发送音频到Whisper API进行转录
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="model">模型名称</param>
    /// <returns>转录后的文本</returns>
    private async Task<string> SendToWhisperAPI(string filePath, string model)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiApiKey}");

            using (var form = new MultipartFormDataContent())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var audioContent = new StreamContent(fileStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                
                form.Add(audioContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent(model), "model");
                form.Add(new StringContent("en"), "language");
                form.Add(new StringContent("json"), "response_format");
                form.Add(new StringContent("0.2"), "temperature");

                var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transcriptionResponse = JsonConvert.DeserializeObject<TranscriptionResponse>(responseContent);
                    return transcriptionResponse.text;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Transcription failed: {response.ReasonPhrase} - {errorContent}");
                    throw new System.Exception("Failed to transcribe audio");
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
