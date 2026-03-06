using UnityEngine;
using System;

public enum LLMProvider
{
    OpenAI,
    DeepSeek
}

public enum TTSProvider
{
    Qwen
}

public enum STTProvider
{
    Whisper,
    SenseVoice,
    Paraformer
}

public static class ClientFactory
{
    public static ILlmClient CreateLLMClient(LLMProvider provider, MonoBehaviour owner, string systemPrompt)
    {
        GameObject clientObject = new GameObject($"{provider}LLMClient");
        ILlmClient client = null;

        switch (provider)
        {
            case LLMProvider.OpenAI:
                var openaiClient = clientObject.AddComponent<OpenAILLMClient>();
                ((OpenAILLMClient)openaiClient).SetOwner(owner);
                openaiClient.Initialize(systemPrompt);
                client = openaiClient;
                break;

            case LLMProvider.DeepSeek:
                var deepseekClient = clientObject.AddComponent<DeepSeekLLMClient>();
                ((DeepSeekLLMClient)deepseekClient).SetOwner(owner);
                deepseekClient.Initialize(systemPrompt);
                client = deepseekClient;
                break;
        }

        return client;
    }

    public static ISTTClient CreateSTTClient(STTProvider provider = STTProvider.SenseVoice, MonoBehaviour owner = null)
    {
        GameObject clientObject = new GameObject($"{provider}STTClient");
        ISTTClient client = null;

        switch (provider)
        {
            case STTProvider.SenseVoice:
                var sensevoiceClient = clientObject.AddComponent<SenseVoiceSTTClient>();
                sensevoiceClient.Initialize();
                client = sensevoiceClient;
                break;

            case STTProvider.Whisper:
                var whisperClient = clientObject.AddComponent<WhisperSTTClient>();
                whisperClient.Initialize();
                client = whisperClient;
                break;

            case STTProvider.Paraformer:
                var paraformerClient = clientObject.AddComponent<ParaformerSTTClient>();
                paraformerClient.Initialize();
                client = paraformerClient;
                break;
        }

        return client;
    }

    public static ITTSClient CreateTTSClient(TTSProvider provider, MonoBehaviour owner)
    {
        GameObject clientObject = new GameObject($"{provider}TTSClient");
        ITTSClient client = null;

        switch (provider)
        {
            case TTSProvider.Qwen:
                var qwenClient = clientObject.AddComponent<QwenTTSClient>();
                qwenClient.Initialize();
                client = qwenClient;
                break;
        }

        return client;
    }

    public static ILlmClient CreateEvaluationClient(MonoBehaviour owner)
    {
        var config = ApiConfig.Instance;
        string provider = config.EvaluationProvider?.ToLower() ?? "deepseek";
        string model = config.EvaluationModel;

        GameObject clientObject = new GameObject($"Evaluation{provider.ToUpper()}Client");
        ILlmClient client = null;

        switch (provider)
        {
            case "deepseek":
                var deepseekClient = clientObject.AddComponent<DeepSeekLLMClient>();
                ((DeepSeekLLMClient)deepseekClient).SetOwner(owner);
                deepseekClient.Initialize(null, model);
                client = deepseekClient;
                break;

            case "openai":
            default:
                var openaiClient = clientObject.AddComponent<OpenAILLMClient>();
                ((OpenAILLMClient)openaiClient).SetOwner(owner);
                openaiClient.Initialize(null, model);
                client = openaiClient;
                break;
        }

        Debug.Log($"[ClientFactory] Created evaluation client: provider={provider}, model={model}");
        return client;
    }
}
