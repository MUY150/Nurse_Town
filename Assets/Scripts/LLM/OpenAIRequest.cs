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

public class OpenAIRequest : MonoBehaviour
{
    public static OpenAIRequest Instance; // Singleton instance
    public string apiUrl = "https://api.openai.com/v1/chat/completions";
    public string apiKey;
    public string currentScenario = "Therapy"; // New scenario selector
    private string currentPatientResponse = "";
    public string lostResponse = "I......I.........no.........fast......";
    public float maxSpeechSpeed = 170f;
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem(); // For scoring system
    private EmotionController emotionController;
    private float currentSpeechSpeed;

    private string basePath;
    private List<string> patientInstructionsList;
    private List<Dictionary<string, string>> chatMessages;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Uncomment if you want this object to persist across scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 网络安全设置 - 在所有平台应用（除了WebGL）
#if !UNITY_WEBGL
        try
        {
            // 完全跳过SSL证书验证
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };

            // 设置多种安全协议支持
            System.Net.ServicePointManager.SecurityProtocol =
                (System.Net.SecurityProtocolType)3072 | // Tls12
                (System.Net.SecurityProtocolType)768 |  // Tls11  
                (System.Net.SecurityProtocolType)192;   // Tls10

            // 禁用HTTP/2，强制使用HTTP/1.1
            System.Net.ServicePointManager.DefaultConnectionLimit = 10;
            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.ServicePointManager.UseNagleAlgorithm = false;

            Debug.Log("✓ Enhanced SSL/TLS settings configured");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to configure SSL/TLS: {e.Message}");
        }
#endif

        // 开始网络诊断
        StartCoroutine(NetworkDiagnostics());

        // 加载API密钥
        LoadApiKey();

        // 初始化路径和组件
        basePath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario);
        ScoreManager.Instance.Initialize(currentScenario);

        // Initialize patient instructions and chat
        InitializeChat();

        // 获取组件
        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
        emotionController = GetComponent<EmotionController>();

        if (emotionController == null)
        {
            Debug.LogError("EmotionController component not found on the GameObject.");
        }
    }

    private void LoadApiKey()
    {
        Debug.Log("=== API KEY LOADING ===");

        // 方法1: 从环境变量加载
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");

        if (!string.IsNullOrEmpty(apiKey))
        {
            Debug.Log("✓ API key loaded from environment variables");
            return;
        }

        // 方法2: 从StreamingAssets配置文件加载
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        Debug.Log($"Looking for config file at: {configPath}");

        if (File.Exists(configPath))
        {
            try
            {
                string configContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(configContent);

                if (config != null && config.ContainsKey("openai_api_key"))
                {
                    apiKey = config["openai_api_key"];
                    Debug.Log("✓ API key loaded from config file");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading config file: {e.Message}");
            }
        }

        // 方法3: 检查是否直接在Inspector中设置了
        if (!string.IsNullOrEmpty(apiKey))
        {
            Debug.Log("✓ API key found in Inspector");
            return;
        }

        Debug.LogError("✗ No API key found! Please set it via environment variable, config file, or Inspector");
    }

    private IEnumerator NetworkDiagnostics()
    {
        Debug.Log("=== NETWORK DIAGNOSTICS START ===");

        // 测试1: 基础网络连接
        Debug.Log("Testing basic internet connectivity...");
        UnityWebRequest testRequest = UnityWebRequest.Get("https://www.google.com");
        testRequest.timeout = 10; // 10秒超时
        yield return testRequest.SendWebRequest();

        if (testRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✓ Basic internet connection: OK");
        }
        else
        {
            Debug.LogError("✗ Basic internet connection: FAILED");
            Debug.LogError($"Error: {testRequest.error}");
            Debug.LogError($"Response Code: {testRequest.responseCode}");
        }

        // 测试2: HTTPS连接
        Debug.Log("Testing HTTPS connection...");
        UnityWebRequest httpsTest = UnityWebRequest.Get("https://httpbin.org/get");
        httpsTest.timeout = 10;
        yield return httpsTest.SendWebRequest();

        if (httpsTest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✓ HTTPS connection: OK");
        }
        else
        {
            Debug.LogError("✗ HTTPS connection: FAILED");
            Debug.LogError($"Error: {httpsTest.error}");
            Debug.LogError($"Response Code: {httpsTest.responseCode}");
        }


        // 等待API密钥加载完成
        yield return new WaitForSeconds(1f);

        // 测试4: API密钥验证
        if (!string.IsNullOrEmpty(apiKey))
        {
            Debug.Log("Testing API key validity...");
            UnityWebRequest keyTest = UnityWebRequest.Get("https://api.openai.com/v1/models");
            keyTest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            keyTest.timeout = 15;
            yield return keyTest.SendWebRequest();

            if (keyTest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✓ API key validation: OK");
                Debug.Log($"Available models response length: {keyTest.downloadHandler.text.Length}");
            }
            else
            {
                Debug.LogError("✗ API key validation: FAILED");
                Debug.LogError($"Error: {keyTest.error}");
                Debug.LogError($"Response Code: {keyTest.responseCode}");
                Debug.LogError($"Response: {keyTest.downloadHandler.text}");
            }
        }
        else
        {
            Debug.LogError("✗ API key: NOT FOUND - Skipping API key validation");
        }

        Debug.Log("=== NETWORK DIAGNOSTICS END ===");
    }

    private string LoadPromptFromFile(string fileName)
    {
        string filePath = Path.Combine(basePath, fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError("Prompt file not found: " + filePath);
            return "";
        }
        return File.ReadAllText(filePath);
    }

    private void InitializePatientInstructions()
    {
        string baseInstructions = LoadPromptFromFile("baseInstructions.txt");
        
    }

    private void InitializeChat()
    {
        string baseInstructions = LoadPromptFromFile("baseInstructions.txt");
        string emotionInstructions = @"
            IMPORTANT: You will analysis your emotion based on the conversation. Then end EVERY response with corresponding emotion codes:
            - Use [0] for neutral responses or statements
            - Use [1] for responses involving minor pain or discomfort
            - Use [2] for positive responses, gratitude, or when feeling better
            - Use [3] for pain
            - Use [4] for sad
            - Use [5] for anger
            - Use [6] for frustration due to speech block or not being understood
            - Use [7] for thinking or processing information
            - Use [8] for an apologetic grimace
            - Use [9] for crying in frustration when unable to communicate";

        string motionInstructions = @"
            IMPORTANT: You will use the following animations based on the conversation. Then end EVERY response with corresponding motion codes after the emotion code:
            - [0] for neutral responses or statements
            - [1] when unable to answer or feeling lost after doctor’s question
            - [2] for strong affirmative or emphatic agreement
            - [3] for agreement with an attempt to add clarification
            - [4] for actively listening or confirming understanding
            - [5] for passive or minimal acknowledgment
            - [6] for disagreement, denial, or inability to answer
            - [7] for intense frustration when unable to express words
            - [8] for expressing impatience, agitation, or urging the doctor during conversation
            - [9] for struggling to recall words or thinking of a response";
        
        string systemPrompt = $"{baseInstructions}\n{emotionInstructions}\n{motionInstructions}";
        chatMessages = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", systemPrompt }
            }
        };
        Debug.Log("System: " + systemPrompt);
    }

    public void ReceiveNurseTranscription(string transcribedText, float speechWpm)
    {
        NurseResponds(transcribedText, speechWpm);
    }

    private void NurseResponds(string nurseMessage, float speechWpm)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        PrintChatMessage(chatMessages);
        currentSpeechSpeed = speechWpm;
        Debug.Log("speech speed:" + currentSpeechSpeed);

        StartCoroutine(PostRequest());

        // Evaluate nurse's response
        ScoreManager.Instance.RecordTurn(currentPatientResponse, nurseMessage);
    }

    IEnumerator PostRequest()
    {
        Debug.Log("=== STARTING API REQUEST ===");
        Debug.Log($"API URL: {apiUrl}");
        Debug.Log($"API Key exists: {!string.IsNullOrEmpty(apiKey)}");

        if (!string.IsNullOrEmpty(apiKey))
        {
            Debug.Log($"API Key preview: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
        }

        if (currentSpeechSpeed > maxSpeechSpeed)
        {
            Debug.Log($"Speech too fast ({currentSpeechSpeed} > {maxSpeechSpeed}), using lost response");
            HandlePatientResponse(lostResponse, 5, 1);
            yield break;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Cannot make API request: No API key available");
            yield break;
        }

        string requestBody = BuildRequestBody();
        Debug.Log($"Request Body Length: {requestBody.Length}");

        // 使用UnityWebRequest.Post方法替代自定义创建
        var request = UnityWebRequest.PostWwwForm(apiUrl, "");

        // 手动设置请求体
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 设置请求头 - 简化版本
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // 设置超时
        request.timeout = 45;

        Debug.Log("Sending request to OpenAI...");
        yield return request.SendWebRequest();

        Debug.Log("=== API RESPONSE ===");
        Debug.Log($"Response Code: {request.responseCode}");
        Debug.Log($"Request Result: {request.result}");
        Debug.Log($"Error: {request.error ?? "None"}");
        Debug.Log($"Response Body Length: {request.downloadHandler?.text?.Length ?? 0}");

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("=== API REQUEST FAILED ===");
            Debug.LogError($"Error Type: {request.result}");
            Debug.LogError($"Error Message: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode}");
            Debug.LogError($"Response Body: {request.downloadHandler.text}");

            // 如果还是421错误，尝试备用方案
            if (request.responseCode == 421)
            {
                Debug.LogWarning("Got 421 error, trying alternative request...");
                yield return StartCoroutine(TryAlternativeRequest(requestBody));
                yield break;
            }

            ShowDetailedError(request);
        }
        else if (request.responseCode == 200)
        {
            Debug.Log("=== API REQUEST SUCCESS ===");
            try
            {
                var jsonResponse = JObject.Parse(request.downloadHandler.text);
                var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();
                Debug.Log($"Received message: {messageContent.Substring(0, Math.Min(100, messageContent.Length))}...");

                var match = Regex.Match(messageContent, @"\[(\d+)\]\[(\d+)\]");
                if (!match.Success)
                {
                    Debug.LogWarning("No emotion code found in response");
                    yield break;
                }

                if (emotionController == null)
                {
                    Debug.LogError("EmotionController is null");
                    yield break;
                }

                int emotionCode = int.Parse(match.Groups[1].Value);
                int motionCode = int.Parse(match.Groups[2].Value);
                Debug.Log($"Extracted emotion code: {emotionCode}, motion code: {motionCode}");

                HandlePatientResponse(messageContent, emotionCode, motionCode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing API response: {e.Message}");
                Debug.LogError($"Response was: {request.downloadHandler.text}");
            }
        }
        else
        {
            Debug.LogError($"Unexpected response code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }

    // 备用请求方法
    private IEnumerator TryAlternativeRequest(string requestBody)
    {
        Debug.Log("=== TRYING ALTERNATIVE REQUEST METHOD ===");

        // 尝试使用WWWForm方法
        var form = new WWWForm();
        form.AddField("data", requestBody);

        var request = UnityWebRequest.Post(apiUrl, form);

        // 重新设置为JSON请求
        request.uploadHandler.Dispose();
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));

        // 重新设置请求头
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("User-Agent", "UnityPlayer");

        request.timeout = 45;

        yield return request.SendWebRequest();

        Debug.Log($"Alternative request result: {request.result}");
        Debug.Log($"Alternative response code: {request.responseCode}");

        if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
        {
            Debug.Log("=== ALTERNATIVE REQUEST SUCCESS ===");
            try
            {
                var jsonResponse = JObject.Parse(request.downloadHandler.text);
                var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();

                var match = Regex.Match(messageContent, @"\[(\d+)\]\[(\d+)\]");
                if (match.Success && emotionController != null)
                {
                    int emotionCode = int.Parse(match.Groups[1].Value);
                    int motionCode = int.Parse(match.Groups[2].Value);
                    Debug.Log($"Extracted emotion code: {emotionCode}, motion code: {motionCode}");
                    HandlePatientResponse(messageContent, emotionCode, motionCode);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing alternative response: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("Alternative request also failed");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }

    private void ShowDetailedError(UnityWebRequest request)
    {
        string errorDetails = "=== DETAILED ERROR ANALYSIS ===\n";

        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                errorDetails += "CONNECTION ERROR - Possible causes:\n";
                errorDetails += "• No internet connection\n";
                errorDetails += "• Firewall blocking the application\n";
                errorDetails += "• Antivirus software blocking network access\n";
                errorDetails += "• DNS resolution issues\n";
                break;

            case UnityWebRequest.Result.ProtocolError:
                errorDetails += "PROTOCOL ERROR - Possible causes:\n";
                if (request.responseCode == 401)
                    errorDetails += "• Invalid or expired API key\n";
                else if (request.responseCode == 403)
                    errorDetails += "• API access forbidden (check billing/limits)\n";
                else if (request.responseCode == 429)
                    errorDetails += "• Rate limit exceeded\n";
                else if (request.responseCode >= 500)
                    errorDetails += "• Server error (try again later)\n";
                else
                    errorDetails += $"• HTTP {request.responseCode} error\n";
                break;
        }

        errorDetails += "\nTROUBLESHOoting STEPS:\n";
        errorDetails += "1. Check your internet connection\n";
        errorDetails += "2. Verify your API key is correct\n";
        errorDetails += "3. Try running the application as administrator\n";
        errorDetails += "4. Check Windows Firewall/antivirus settings\n";
        errorDetails += "5. Try again in a few minutes\n";

        Debug.LogError(errorDetails);
    }

    private void HandlePatientResponse(string responseText, int emotionCode, int motionCode)
    {
        currentPatientResponse = responseText; // for scoring

        chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", responseText } });
        PrintChatMessage(chatMessages);

        if (TTSManager.Instance != null)
        {
            TTSManager.Instance.ConvertTextToSpeech(responseText);
        }
        else
        {
            Debug.LogError("TTSManager instance not found.");
        }

        if (emotionController != null)
        {
            emotionController.HandleEmotionCode(emotionCode, motionCode);
        }
    }

    private string BuildRequestBody()
    {
        var requestObject = new
        {
            model = "gpt-4o-2024-08-06",
            messages = chatMessages,
            temperature = 0.8f
        };
        return JsonConvert.SerializeObject(requestObject, Formatting.Indented);
    }

    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        if (messages.Count == 0)
            return;

        var latestMessage = messages[messages.Count - 1];
        string role = latestMessage["role"];
        string content = latestMessage["content"];

        // Extract emotion code if present
        string emotionCode = "";
        string motionCode = "";
        var match = Regex.Match(content, @"\[(\d+)\]\[(\d+)\]");
        if (match.Success)
        {
            emotionCode = $" (Emotion: {match.Groups[1].Value})";
            motionCode = $" (Motion: {match.Groups[2].Value})";
        }

        Debug.Log($"[{role.ToUpper()}]{emotionCode}{motionCode}\n{content}\n");
    }
}