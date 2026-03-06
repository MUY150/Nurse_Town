using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ParaformerSTTClient : MonoBehaviour, ISTTClient
{
    private string _apiKey;
    private bool _isRecording = false;
    private AudioClip _recordedClip;
    private string _model = "paraformer-realtime-v2";
    private int _sampleRate = 16000;

    public event Action<string> OnTranscriptionComplete;

    public void Initialize()
    {
        var config = ApiConfig.Instance;
        _apiKey = config.AlibabaApiKey;

        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogWarning("[ParaformerSTTClient] Alibaba API key not found");
        }
        else
        {
            Debug.Log("[ParaformerSTTClient] Initialized with Paraformer");
        }
    }

    public void StartRecording(int durationSeconds = 10, int sampleRate = 16000)
    {
        if (!_isRecording)
        {
            _sampleRate = sampleRate;
            _recordedClip = Microphone.Start(null, false, durationSeconds, sampleRate);
            _isRecording = true;
            Debug.Log("[ParaformerSTTClient] Recording started...");
        }
    }

    public void StopRecordingAndTranscribe()
    {
        if (_isRecording)
        {
            Microphone.End(null);
            _isRecording = false;
            Debug.Log("[ParaformerSTTClient] Recording stopped, transcribing...");
            StartCoroutine(TranscribeAudio());
        }
    }

    public void StopRecording()
    {
        if (_isRecording)
        {
            Microphone.End(null);
            _isRecording = false;
        }
    }

    private IEnumerator TranscribeAudio()
    {
        if (_recordedClip == null)
        {
            Debug.LogWarning("[ParaformerSTTClient] No audio recorded - clip is null");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        int waitCount = 0;
        while (_recordedClip.samples == 0 && waitCount < 20)
        {
            yield return new WaitForSeconds(0.05f);
            waitCount++;
        }

        if (_recordedClip.samples == 0)
        {
            Debug.LogWarning("[ParaformerSTTClient] No audio recorded after waiting");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        Debug.Log($"[ParaformerSTTClient] Audio recorded - samples: {_recordedClip.samples}, channels: {_recordedClip.channels}, frequency: {_recordedClip.frequency}");

        string filePath = Path.Combine(Application.persistentDataPath, "paraformer_audio.wav");

        try
        {
            SavWav.Save("paraformer_audio.wav", _recordedClip);
            Debug.Log($"[ParaformerSTTClient] Audio saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ParaformerSTTClient] Failed to save audio: {ex.Message}");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        yield return StartCoroutine(CallWithWebSocket(filePath));

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch { }
    }

    private IEnumerator CallWithWebSocket(string filePath)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Debug.LogError("[ParaformerSTTClient] API key is missing");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        var webSocket = new System.Net.WebSockets.ClientWebSocket();
        var cancellationTokenSource = new System.Threading.CancellationTokenSource();
        string result = "";

        yield return StartCoroutine(ConnectWebSocket(webSocket, cancellationTokenSource.Token, (success) =>
        {
            if (!success)
            {
                Debug.LogError("[ParaformerSTTClient] WebSocket 连接失败");
                OnTranscriptionComplete?.Invoke("");
                return;
            }

            StartCoroutine(ExecuteRecognitionTask(webSocket, cancellationTokenSource, filePath));
        }));
    }

    private IEnumerator ExecuteRecognitionTask(System.Net.WebSockets.ClientWebSocket webSocket, System.Threading.CancellationTokenSource cts, string filePath)
    {
        string taskId = Guid.NewGuid().ToString("N");
        bool taskStarted = false;
        string result = "";

        yield return StartCoroutine(SendRunTask(webSocket, taskId, cts.Token));

        int timeout = 0;
        while (!taskStarted && timeout < 50)
        {
            JObject message = null;
            yield return StartCoroutine(ReceiveMessage(webSocket, cts.Token, (msg) =>
            {
                message = msg;
            }));

            if (message != null)
            {
                var eventValue = message["header"]?["event"]?.ToString();
                if (eventValue == "task-started")
                {
                    Debug.Log("[ParaformerSTTClient] 任务开启成功");
                    taskStarted = true;
                }
                else if (eventValue == "task-failed")
                {
                    Debug.LogError($"[ParaformerSTTClient] 任务失败：{message["header"]?["error_message"]}");
                    OnTranscriptionComplete?.Invoke("");
                    yield break;
                }
            }
            timeout++;
            yield return new WaitForSeconds(0.1f);
        }

        if (!taskStarted)
        {
            Debug.LogError("[ParaformerSTTClient] 等待任务启动超时");
            OnTranscriptionComplete?.Invoke("");
            yield break;
        }

        yield return StartCoroutine(SendAudioFile(webSocket, filePath, cts.Token));

        yield return StartCoroutine(SendFinishTask(webSocket, taskId, cts.Token));

        bool taskFinished = false;
        timeout = 0;
        while (!taskFinished && timeout < 100)
        {
            JObject message = null;
            yield return StartCoroutine(ReceiveMessage(webSocket, cts.Token, (msg) =>
            {
                message = msg;
            }));

            if (message != null)
            {
                var eventValue = message["header"]?["event"]?.ToString();
                if (eventValue == "result-generated")
                {
                    var text = message["payload"]?["output"]?["sentence"]?["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        Debug.Log($"[ParaformerSTTClient] 识别结果：{text}");
                        result = text;
                    }
                }
                else if (eventValue == "task-finished")
                {
                    Debug.Log("[ParaformerSTTClient] 任务完成");
                    taskFinished = true;
                }
                else if (eventValue == "task-failed")
                {
                    Debug.LogError($"[ParaformerSTTClient] 任务失败：{message["header"]?["error_message"]}");
                    OnTranscriptionComplete?.Invoke("");
                    yield break;
                }
            }
            timeout++;
            yield return new WaitForSeconds(0.1f);
        }

        StartCoroutine(CloseWebSocket(webSocket, cts.Token));
        cts.Cancel();

        Debug.Log($"[ParaformerSTTClient] Transcription: {result}");
        OnTranscriptionComplete?.Invoke(result);
    }

    private IEnumerator ConnectWebSocket(System.Net.WebSockets.ClientWebSocket webSocket, System.Threading.CancellationToken token, Action<bool> callback)
    {
        var connectTask = webSocket.ConnectAsync(new Uri("wss://dashscope.aliyuncs.com/api-ws/v1/inference/"), token);
        
        while (!connectTask.IsCompleted && !token.IsCancellationRequested)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (token.IsCancellationRequested)
        {
            callback(false);
        }
        else if (connectTask.IsFaulted)
        {
            Debug.LogError($"[ParaformerSTTClient] WebSocket 连接失败：{connectTask.Exception}");
            callback(false);
        }
        else
        {
            Debug.Log("[ParaformerSTTClient] WebSocket 连接成功");
            callback(true);
        }
    }

    private IEnumerator SendRunTask(System.Net.WebSockets.ClientWebSocket webSocket, string taskId, System.Threading.CancellationToken token)
    {
        var runTaskJson = GenerateRunTaskJson(taskId);
        var bytes = Encoding.UTF8.GetBytes(runTaskJson);
        var segment = new ArraySegment<byte>(bytes);
        
        var sendTask = webSocket.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Text, true, token);
        
        while (!sendTask.IsCompleted && !token.IsCancellationRequested)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!token.IsCancellationRequested && !sendTask.IsFaulted)
        {
            Debug.Log("[ParaformerSTTClient] 发送 run-task 指令");
        }
    }

    private IEnumerator SendFinishTask(System.Net.WebSockets.ClientWebSocket webSocket, string taskId, System.Threading.CancellationToken token)
    {
        var finishTaskJson = GenerateFinishTaskJson(taskId);
        var bytes = Encoding.UTF8.GetBytes(finishTaskJson);
        var segment = new ArraySegment<byte>(bytes);
        
        var sendTask = webSocket.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Text, true, token);
        
        while (!sendTask.IsCompleted && !token.IsCancellationRequested)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!token.IsCancellationRequested && !sendTask.IsFaulted)
        {
            Debug.Log("[ParaformerSTTClient] 发送 finish-task 指令");
        }
    }

    private IEnumerator SendAudioFile(System.Net.WebSockets.ClientWebSocket webSocket, string filePath, System.Threading.CancellationToken token)
    {
        byte[] audioData;
        try
        {
            audioData = File.ReadAllBytes(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ParaformerSTTClient] 读取音频文件失败：{ex.Message}");
            yield break;
        }

        Debug.Log($"[ParaformerSTTClient] 发送音频数据：{audioData.Length} bytes");

        int bufferSize = 1024;
        int offset = 0;

        while (offset < audioData.Length && !token.IsCancellationRequested)
        {
            int bytesToSend = Math.Min(bufferSize, audioData.Length - offset);
            var segment = new ArraySegment<byte>(audioData, offset, bytesToSend);
            
            var sendTask = webSocket.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Binary, true, token);
            
            while (!sendTask.IsCompleted && !token.IsCancellationRequested)
            {
                yield return new WaitForSeconds(0.01f);
            }

            if (sendTask.IsFaulted)
            {
                Debug.LogError($"[ParaformerSTTClient] 发送音频数据失败：{sendTask.Exception}");
                yield break;
            }

            offset += bytesToSend;
            yield return new WaitForSeconds(0.01f);
        }

        Debug.Log("[ParaformerSTTClient] 音频数据发送完成");
    }

    private IEnumerator ReceiveMessage(System.Net.WebSockets.ClientWebSocket webSocket, System.Threading.CancellationToken token, Action<JObject> callback)
    {
        var buffer = new byte[4096];
        var segment = new ArraySegment<byte>(buffer);
        
        var receiveTask = webSocket.ReceiveAsync(segment, token);
        
        while (!receiveTask.IsCompleted && !token.IsCancellationRequested)
        {
            yield return new WaitForSeconds(0.05f);
        }

        if (token.IsCancellationRequested)
        {
            callback(null);
        }
        else if (receiveTask.IsFaulted)
        {
            Debug.LogError($"[ParaformerSTTClient] 接收消息失败：{receiveTask.Exception}");
            callback(null);
        }
        else if (receiveTask.Result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
        {
            callback(null);
        }
        else
        {
            try
            {
                var message = Encoding.UTF8.GetString(buffer, 0, receiveTask.Result.Count);
                var jsonObject = JObject.Parse(message);
                callback(jsonObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ParaformerSTTClient] 解析消息失败：{ex.Message}");
                callback(null);
            }
        }
    }

    private IEnumerator CloseWebSocket(System.Net.WebSockets.ClientWebSocket webSocket, System.Threading.CancellationToken token)
    {
        var closeTask = webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing", token);
        
        while (!closeTask.IsCompleted && !token.IsCancellationRequested)
        {
            yield return new WaitForSeconds(0.1f);
        }

        webSocket.Dispose();
    }

    private string GenerateRunTaskJson(string taskId)
    {
        var runTask = new JObject
        {
            ["header"] = new JObject
            {
                ["action"] = "run-task",
                ["task_id"] = taskId,
                ["streaming"] = "duplex"
            },
            ["payload"] = new JObject
            {
                ["task_group"] = "audio",
                ["task"] = "asr",
                ["function"] = "recognition",
                ["model"] = _model,
                ["parameters"] = new JObject
                {
                    ["format"] = "wav",
                    ["sample_rate"] = _sampleRate,
                    ["language_hints"] = new JArray { "zh", "en" },
                    ["disfluency_removal_enabled"] = false
                },
                ["input"] = new JObject()
            }
        };
        return JsonConvert.SerializeObject(runTask);
    }

    private string GenerateFinishTaskJson(string taskId)
    {
        var finishTask = new JObject
        {
            ["header"] = new JObject
            {
                ["action"] = "finish-task",
                ["task_id"] = taskId,
                ["streaming"] = "duplex"
            },
            ["payload"] = new JObject
            {
                ["input"] = new JObject()
            }
        };
        return JsonConvert.SerializeObject(finishTask);
    }

    public bool IsRecording => _isRecording;
}
