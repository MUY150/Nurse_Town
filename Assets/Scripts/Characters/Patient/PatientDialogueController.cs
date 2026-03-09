using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PatientDialogueController : MonoBehaviour, ITTSProvider
{
    public static PatientDialogueController Instance;
    
    [Header("配置")]
    [SerializeField] private string scenarioName = "brocaAphasia";
    [SerializeField] private bool loadFromFile = true;
    
    [Header("组件引用 - 站立角色")]
    [SerializeField] private EmotionController emotionController;
    
    [Header("组件引用 - 坐姿角色")]
    [SerializeField] private sitCharacterAnimationController animController;
    [SerializeField] private BloodEffectController bloodEffectController;
    
    [Header("组件引用 - 评分系统")]
    [SerializeField] private ScoringSystem scoringSystem;
    
    private PatientProfile profile;
    private ILlmClient _llmClient;
    private List<string> patientInstructionsList;
    private string currentPatientResponse = "";
    private CurrentChatUI chatUI;
    
    private ITTSProvider ttsProvider;
    
    public bool IsAvailable => ttsProvider != null;
    
    void Awake()
    {
        Debug.Log($"[PatientDialogueController] Awake called on {gameObject.name}");
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[PatientDialogueController] Instance set to {gameObject.name}");
        }
        else
        {
            Debug.Log($"[PatientDialogueController] Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log($"[PatientDialogueController] Start called on {gameObject.name}");
        
        if (Instance != this)
        {
            Debug.Log($"[PatientDialogueController] Skipping Start on {gameObject.name}");
            return;
        }
        
        ApiConfig.Initialize();
        
        LoadProfile();
        InitializeTTSProvider();
        InitializeLLMClient();
        SetupChatUI();
        InitializeScoringSystem();
    }
    
    private void LoadProfile()
    {
        if (loadFromFile)
        {
            profile = PatientConfigLoader.LoadFromFile(scenarioName);
        }
        else
        {
            profile = PatientConfigLoader.LoadDefault();
        }
        
        patientInstructionsList = profile.patientInstructionsList;
        Debug.Log($"[PatientDialogueController] Loaded profile for scenario: {profile.scenarioName}");
    }
    
    private void InitializeTTSProvider()
    {
        if (profile.useAnimatorEmotion)
        {
            if (sitTTSManager.Instance != null)
            {
                ttsProvider = sitTTSManager.Instance;
                Debug.Log("[PatientDialogueController] Using sitTTSManager");
            }
            else
            {
                Debug.LogWarning("[PatientDialogueController] sitTTSManager.Instance is null");
            }
        }
        else
        {
            if (TTSManager.Instance != null)
            {
                ttsProvider = TTSManager.Instance;
                Debug.Log("[PatientDialogueController] Using TTSManager");
            }
            else
            {
                Debug.LogWarning("[PatientDialogueController] TTSManager.Instance is null");
            }
        }
    }
    
    private void InitializeLLMClient()
    {
        if (patientInstructionsList == null || patientInstructionsList.Count == 0)
        {
            Debug.LogError("[PatientDialogueController] No patient instructions loaded");
            return;
        }
        
        string emotionInstructions = GetEmotionInstructions();
        
        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];
        string systemPrompt = $"{selectedPatientInstructions}\n\n{emotionInstructions}";
        
        _llmClient = new LlmClient(LlmScene.Patient, systemPrompt);
        _llmClient.OnMessageReceived += OnLLMResponseReceived;
        
        Debug.Log($"[PatientDialogueController] LLM Client initialized for scenario: {scenarioName}");
        _llmClient.SendChatMessage("Hello");
    }
    
    private string GetEmotionInstructions()
    {
        if (profile.useAnimatorEmotion)
        {
            return @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements (plays bend animation)
            - Use [1] for responses showing physical discomfort (plays rub arm animation)
            - Use [2] for sad or negative emotional responses (plays sad animation)
            - Use [3] for positive responses or agreement, and appreciation (plays thumbs up animation)
            - Use [4] for blood pressureing, if the nurse asks to measure your blood pressure (plays arm raise animation)
            ";
        }
        else
        {
            return @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements
            - Use [1] for responses involving minor pain or discomfort
            - Use [2] for positive responses, gratitude, or when feeling better
            - Use [3] for pain
            - Use [4] for sad
            - Use [5] for anger or frustration
            ";
        }
    }
    
    private void SetupChatUI()
    {
        if (!profile.enableChatUI) return;
        
        chatUI = FindObjectOfType<CurrentChatUI>();
        if (chatUI != null)
        {
            chatUI.SetCurrentLlmClient(_llmClient);
            Debug.Log("[PatientDialogueController] ChatUI linked");
        }
        else
        {
            Debug.LogWarning("[PatientDialogueController] CurrentChatUI not found");
        }
    }
    
    private void InitializeScoringSystem()
    {
        if (scoringSystem == null)
        {
            scoringSystem = new ScoringSystem();
        }
        scoringSystem.Initialize(this);
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Initialize(scenarioName);
        }
    }
    
    private void OnLLMResponseReceived(string message)
    {
        currentPatientResponse = message;
        
        if (ttsProvider != null)
        {
            ttsProvider.ConvertTextToSpeech(message);
        }
        else
        {
            Debug.LogWarning("[PatientDialogueController] No TTS provider available");
        }
        
        HandleEmotionCode(message);
    }
    
    private void HandleEmotionCode(string message)
    {
        var match = Regex.Match(message, @"\[(\d+)\]");
        if (!match.Success) return;
        
        int emotionCode = int.Parse(match.Groups[1].Value);
        
        if (profile.useTimelineEmotion && emotionController != null)
        {
            emotionController.HandleEmotionCode(emotionCode);
            emotionController.PlayEmotion();
        }
        else if (profile.useAnimatorEmotion && animController != null)
        {
            HandleAnimatorEmotion(emotionCode);
        }
    }
    
    private void HandleAnimatorEmotion(int emotionCode)
    {
        switch (emotionCode)
        {
            case 0:
                animController.PlayBend();
                break;
            case 1:
                animController.PlayRubArm();
                break;
            case 2:
                animController.PlaySad();
                break;
            case 3:
                animController.PlayThumbUp();
                break;
            case 4:
                animController.PlayBloodPressure();
                bloodEffectController?.SetBloodVisibility(true);
                break;
            default:
                animController.PlayIdle();
                break;
        }
    }
    
    public void ReceiveNurseTranscription(string transcribedText)
    {
        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            Debug.LogWarning("[PatientDialogueController] Received empty transcription");
            return;
        }
        
        NurseResponds(transcribedText);
    }
    
    private void NurseResponds(string nurseMessage)
    {
        Debug.Log($"[PatientDialogueController] NurseResponds: {nurseMessage}");
        
        if (_llmClient != null)
        {
            _llmClient.SendChatMessage(nurseMessage);
        }
        
        if (profile.useAnimatorEmotion)
        {
            scoringSystem?.EvaluateNurseResponse(nurseMessage);
        }
        else
        {
            ScoreManager.Instance?.RecordTurn(currentPatientResponse, nurseMessage);
        }
    }
    
    public void ConvertTextToSpeech(string text)
    {
        if (ttsProvider != null)
        {
            ttsProvider.ConvertTextToSpeech(text);
        }
    }
    
    public ILlmClient GetLlmClient()
    {
        return _llmClient;
    }
    
    public PatientProfile GetProfile()
    {
        return profile;
    }
}
