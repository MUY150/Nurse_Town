using System;
using System.Collections.Generic;
using UnityEngine;
using NurseTown.Core.Interfaces;

public class PatientDialogueController : Singleton<PatientDialogueController>, ITTSProvider, IConversationTarget
{
    
    [Header("配置")]
    [SerializeField] private string characterId = "hypertensionPatient";
    [SerializeField] private bool loadFromFile = true;
    [SerializeField] private string posture = "sitting";
    
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
    
    void Start()
    {
        Debug.Log($"[PatientDialogueController] Start called on {gameObject.name}");
        
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
            profile = PatientConfigLoader.LoadFromFile(characterId);
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
        if (TTSManager.Instance != null)
        {
            ttsProvider = TTSManager.Instance;
            Debug.Log($"[PatientDialogueController] Using TTSManager with CharacterId: {characterId}");
        }
        else
        {
            Debug.LogWarning("[PatientDialogueController] TTSManager.Instance is null - TTS will not work");
        }
    }
    
    private void InitializeLLMClient()
    {
        if (patientInstructionsList == null || patientInstructionsList.Count == 0)
        {
            Debug.LogError("[PatientDialogueController] No patient instructions loaded");
            return;
        }
        
        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];
        string systemPrompt = selectedPatientInstructions;
        
        _llmClient = new LlmClient(LlmScene.Patient, systemPrompt);
        _llmClient.OnMessageReceived += OnLLMResponseReceived;
        
        var speakTool = new SpeakTool();
        var actTool = new ActTool();
        var completeSessionTool = new CompleteSessionTool();
        
        _llmClient.RegisterTool(speakTool);
        _llmClient.RegisterTool(actTool);
        _llmClient.RegisterTool(completeSessionTool);
        
        // 使用新的配置名格式: patient_{characterId}_{posture}
        string configName = $"patient_{characterId}_{posture}";
        
        if (animController != null)
        {
            animController.InitializeFromAnimationService(configName);
            Debug.Log($"[PatientDialogueController] Using serialized sitCharacterAnimationController with config: {configName}");
            
            var context = new ToolContext
            {
                CharacterId = characterId,
                AnimationConfig = AnimationService.Instance.CurrentConfig,
                AnimationController = animController
            };
            ToolRegistry.Instance.CurrentContext = context;
        }
        else
        {
            var sitAnimCtrl = GetComponent<sitCharacterAnimationController>();
            var charAnimController = GetComponent<CharacterAnimationController>();
            
            if (sitAnimCtrl != null)
            {
                sitAnimCtrl.InitializeFromAnimationService(configName);
                Debug.Log($"[PatientDialogueController] Using GetComponent sitCharacterAnimationController with config: {configName}");
                
                var context = new ToolContext
                {
                    CharacterId = characterId,
                    AnimationConfig = AnimationService.Instance.CurrentConfig,
                    AnimationController = sitAnimCtrl
                };
                ToolRegistry.Instance.CurrentContext = context;
            }
            else if (charAnimController != null)
            {
                AnimationService.Instance.SetCharacter(charAnimController, configName);
                Debug.Log($"[PatientDialogueController] Using CharacterAnimationController with config: {configName}");

                var context = new ToolContext
                {
                    CharacterId = characterId,
                    AnimationConfig = AnimationService.Instance.CurrentConfig,
                    AnimationController = charAnimController
                };
                ToolRegistry.Instance.CurrentContext = context;
            }
            else
            {
                Debug.LogWarning("[PatientDialogueController] No animation controller found - neither sitCharacterAnimationController nor CharacterAnimationController");
            }
        }

        LlmEventBus.Subscribe<SessionCompleteEvent>(HandleSessionComplete);

        Debug.Log($"[PatientDialogueController] LLM Client initialized with {_llmClient.GetRegisteredTools().Count} tools for scenario: {characterId}");
        _llmClient.SendChatMessage("Hello");
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
            ScoreManager.Instance.Initialize(characterId);
        }
    }
    
    private void OnLLMResponseReceived(string message)
    {
        currentPatientResponse = message;
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
    
    public ICharacterAnimation GetAnimationController()
    {
        if (animController != null)
        {
            return animController;
        }
        
        var sitAnimCtrl = GetComponent<sitCharacterAnimationController>();
        if (sitAnimCtrl != null)
        {
            return sitAnimCtrl;
        }
        
        var charAnimController = GetComponent<CharacterAnimationController>();
        if (charAnimController != null)
        {
            return charAnimController;
        }
        
        return null;
    }
    
    private void HandleSessionComplete(SessionCompleteEvent args)
    {
        Debug.Log($"[PatientDialogueController] Session complete: Correct={args.DiagnosisCorrect}");
        Debug.Log($"[PatientDialogueController] Summary: {args.Summary}");
        
        if (args.KeyObservations != null && args.KeyObservations.Length > 0)
        {
            Debug.Log($"[PatientDialogueController] Key observations: {string.Join(", ", args.KeyObservations)}");
        }
        
        if (args.MissedPoints != null && args.MissedPoints.Length > 0)
        {
            Debug.Log($"[PatientDialogueController] Missed points: {string.Join(", ", args.MissedPoints)}");
        }
    }
}
