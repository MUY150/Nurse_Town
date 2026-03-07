using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class OpenAIRequest : MonoBehaviour
{
    public static OpenAIRequest Instance;
    private ILlmClient _llmClient;
    public string currentScenario = "brocaAphasia";
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string apiKey;
    private string currentPatientResponse = "";
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem();
    private EmotionController emotionController;
    private string basePath;
    private List<string> patientInstructionsList;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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

    void Start()
    {
        ApiConfig.Initialize();
        apiKey = EnvironmentLoader.GetEnvVariable("DEEPSEEK_API_KEY") ?? EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        basePath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario);
        ScoreManager.Instance.Initialize(currentScenario);
        InitializePatientInstructions();
        InitializeLLMClient();

        scoringSystem.Initialize(this);

        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
        emotionController = GetComponent<EmotionController>();
        if (emotionController == null)
        {
            emotionController = FindObjectOfType<EmotionController>();
            if (emotionController == null)
            {
                Debug.LogError("EmotionController component not found on the GameObject or in the scene.");
            }
        }
    }

    private void InitializePatientInstructions()
    {
        string baseInstructions = LoadPromptFromFile("baseInstructions.txt");
        string caseHistoryPrompt = LoadPromptFromFile("caseHistory.txt");
        patientInstructionsList = new List<string>();

        for (int i = 1; i <= 3; i++)
        {
            string patientFile = $"patient{i}.txt";
            string patientSpecific = LoadPromptFromFile(patientFile);
            if (string.IsNullOrEmpty(patientSpecific))
            {
                Debug.LogError("Failed to load patient file: " + patientFile);
                continue;
            }
            string fullPrompt = $"{baseInstructions}\n{caseHistoryPrompt}\n{patientSpecific}";
            patientInstructionsList.Add(fullPrompt);
        }
        if (patientInstructionsList.Count == 0)
        {
            Debug.LogError("No patient instructions loaded for scenario: " + currentScenario);
        }
    }

    private void InitializeLLMClient()
    {
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements
            - Use [1] for responses involving minor pain or discomfort
            - Use [2] for positive responses, gratitude, or when feeling better
            - Use [3] for pain
            - Use [4] for sad
            - Use [5] for anger or frustration";

        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];
        string systemPrompt = $"{selectedPatientInstructions}\n\n{emotionInstructions}";

        _llmClient = new LlmClient(LlmScene.Patient, systemPrompt);
        _llmClient.OnMessageReceived += OnLLMResponseReceived;

        Debug.Log("[OpenAIRequest] LLM Client initialized for Patient scene");
        _llmClient.SendChatMessage("Hello");
    }

    private void OnLLMResponseReceived(string message)
    {
        currentPatientResponse = message;

        if (TTSManager.Instance != null)
        {
            TTSManager.Instance.ConvertTextToSpeech(message);
        }
        else
        {
            Debug.LogError("TTSManager instance not found.");
        }

        var match = Regex.Match(message, @"\[(\d+)\]");
        if (match.Success && emotionController != null)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            emotionController.HandleEmotionCode(emotionCode);
        }
    }

    public void ReceiveNurseTranscription(string transcribedText)
    {
        NurseResponds(transcribedText);
    }

    private void NurseResponds(string nurseMessage)
    {
        if (_llmClient != null)
        {
            _llmClient.SendChatMessage(nurseMessage);
        }

        ScoreManager.Instance.RecordTurn(currentPatientResponse, nurseMessage);
    }

    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        if (messages.Count == 0)
            return;

        var latestMessage = messages[messages.Count - 1];
        string role = latestMessage["role"];
        string content = latestMessage["content"];

        string emotionCode = "";
        var match = Regex.Match(content, @"\[(\d+)\]$");
        if (match.Success)
        {
            emotionCode = $" (Emotion: {match.Groups[1].Value})";
        }

        Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
    }
}
