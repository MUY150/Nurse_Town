using UnityEngine;

public class ApiConfig : Singleton<ApiConfig>
{
    private static readonly string DefaultOpenAIModel = "gpt-4-turbo-preview";
    private static readonly string DefaultDeepSeekModel = "deepseek-chat";
    private static readonly string DefaultQwenVoice = "longxiaochun";

    public string OpenAIApiKey { get; private set; }
    public string DeepSeekApiKey { get; private set; }
    public string AlibabaApiKey { get; private set; }
    public string NvidiaApiKey { get; private set; }

    public string OpenAIChatUrl => "https://api.openai.com/v1/chat/completions";
    public string OpenAISTTUrl => "https://api.openai.com/v1/audio/transcriptions";
    public string OpenAIModel => DefaultOpenAIModel;

    public string DeepSeekChatUrl => "https://api.deepseek.com/v1/chat/completions";
    public string DeepSeekModel => DefaultDeepSeekModel;

    public string QwenTTSUrl => "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-to-speech";
    public string QwenMultiModalUrl => "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";
    public string QwenVoice => DefaultQwenVoice;

    public string ParaformerSTTUrl => "https://dashscope.aliyuncs.com/api/v1/services/audio/asr/transcription";
    public string ParaformerModel => "paraformer-realtime-v2";

    public string NvidiaA2FUrl => "https://grpc.nvcf.nvidia.com:443";

    public string EvaluationProvider { get; private set; }
    public string EvaluationModel { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        LoadApiKeys();
    }

    private void LoadApiKeys()
    {
        OpenAIApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        DeepSeekApiKey = EnvironmentLoader.GetEnvVariable("DEEPSEEK_API_KEY") ?? OpenAIApiKey;
        AlibabaApiKey = EnvironmentLoader.GetEnvVariable("DASHSCOPE_API_KEY") 
                      ?? EnvironmentLoader.GetEnvVariable("ALIBABA_API_KEY")
                      ?? EnvironmentLoader.GetEnvVariable("QWEN_API_KEY");
        NvidiaApiKey = EnvironmentLoader.GetEnvVariable("NVIDIA_API_KEY");

        EvaluationProvider = EnvironmentLoader.GetEnvVariable("EVALUATION_PROVIDER") ?? "deepseek";
        EvaluationModel = EnvironmentLoader.GetEnvVariable("EVALUATION_MODEL") ?? DefaultDeepSeekModel;

        if (string.IsNullOrEmpty(OpenAIApiKey))
        {
            Debug.LogWarning("[ApiConfig] OPENAI_API_KEY not found in environment variables");
        }
        if (string.IsNullOrEmpty(DeepSeekApiKey))
        {
            Debug.LogWarning("[ApiConfig] DEEPSEEK_API_KEY not found, falling back to OPENAI_API_KEY");
        }
        if (string.IsNullOrEmpty(AlibabaApiKey))
        {
            Debug.LogWarning("[ApiConfig] ALIBABA_API_KEY not found in environment variables");
        }
        if (string.IsNullOrEmpty(NvidiaApiKey))
        {
            Debug.LogWarning("[ApiConfig] NVIDIA_API_KEY not found in environment variables");
        }

        Debug.Log("[ApiConfig] API configuration loaded");
    }

    public static void Initialize()
    {
        if (Instance == null)
        {
            GameObject configObject = new GameObject("ApiConfig");
            configObject.AddComponent<ApiConfig>();
            DontDestroyOnLoad(configObject);
        }
    }
}
