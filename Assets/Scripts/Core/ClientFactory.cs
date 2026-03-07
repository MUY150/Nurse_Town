using UnityEngine;
using System;

[Obsolete("Use LlmScene enum with LlmClient instead")]
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
    [Obsolete("Use new LlmClient(LlmScene scene, string systemPrompt) instead. This method will be removed in a future version.")]
    public static ILlmClient CreateLLMClient(LLMProvider provider, MonoBehaviour owner, string systemPrompt)
    {
        Debug.LogWarning("[ClientFactory] CreateLLMClient is deprecated. Use new LlmClient(LlmScene, systemPrompt) instead.");
        
        LlmScene scene = LlmScene.Custom;
        return new LlmClient(scene, systemPrompt);
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

    [Obsolete("Use new LlmClient(LlmScene.Evaluation, systemPrompt, enableLogging: false) instead. This method will be removed in a future version.")]
    public static ILlmClient CreateEvaluationClient(MonoBehaviour owner)
    {
        Debug.LogWarning("[ClientFactory] CreateEvaluationClient is deprecated. Use new LlmClient(LlmScene.Evaluation, systemPrompt, false) instead.");
        
        return new LlmClient(LlmScene.Evaluation, "You are an expert evaluator.", enableLogging: false);
    }
}
