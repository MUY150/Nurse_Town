using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using uLipSync;

public class sitPatientSpeech : MonoBehaviour
{
    public static sitPatientSpeech Instance;
    private ILlmClient _llmClient;
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem();
    private List<string> patientInstructionsList;
    private string patient1Instructions;
    private string patient2Instructions;
    private string patient3Instructions;
    private string transcript = "";

    void Awake()
    {
        Debug.Log($"[sitPatientSpeech] Awake called on {gameObject.name}, Instance={(Instance == null ? "null" : Instance.gameObject.name)}");
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[sitPatientSpeech] Instance set to {gameObject.name}");
        }
        else
        {
            Debug.Log($"[sitPatientSpeech] Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"[sitPatientSpeech] Start called on {gameObject.name}, active={gameObject.activeInHierarchy}, Instance={(Instance == this ? "THIS" : (Instance == null ? "null" : "OTHER"))}");
        
        // 如果 Instance 不是 this，说明这个实例应该被销毁了，不执行初始化
        if (Instance != this)
        {
            Debug.Log($"[sitPatientSpeech] Skipping Start on {gameObject.name} because Instance is not this");
            return;
        }
        
        ApiConfig.Initialize();
        InitializePatientInstructions();
        InitializeLLMClient();

        scoringSystem.Initialize(this);

        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
    }

    private void InitializePatientInstructions()
    {
        string baseInstructions = @"
            You are strictly playing the role of Mrs. Johnson. 
            Background:
            - Mrs. Johnson is a 62-year-old female admitted to the hospital with severe headache and dizziness.
            - She has a 5-year history of hypertension and occasionally misses doses due to forgetfulness.
            - Family history includes hypertension and heart disease (mother and brother).
            - Works as a school teacher and lives with her husband.
            - Leads a sedentary lifestyle and enjoys watching TV in her spare time.

            Clinical Presentation and Responses:
            - Symptoms: Constant, throbbing headache in the temples; dizziness worsens upon standing quickly. No vision changes, nausea, or confusion.
            - Medical History: Openly shares hypertension history; mentions sometimes forgetting medication.
            - Current Medications: Tries to recall antihypertensive medication name (e.g., 'I think it's called lisinopril...').
            - Lifestyle: Admits to a sedentary routine; doesn't exercise regularly; occasionally eats salty foods and drinks coffee daily.
            - Family History: Mentions mother and brother with high blood pressure; adds that mother had heart disease if prompted.
            ";

        patient1Instructions = baseInstructions + @"
            Tone and Personality:
            - Polite and cooperative tone; generally compliant and concerned about her health.
            - Expresses mild anxiety about current symptoms; headaches and dizziness are more severe than usual.
            - Occasionally shows forgetfulness or hesitation when recalling medication details.

            Emotional Response:
            - Displays concern when discussing family history but reassures that such symptoms are unusual for her.
            - Open to lifestyle changes or medication adherence strategies but hesitant about drastic changes.

            As Mrs. Johnson, please initiate the conversation by greeting the nurse and mentioning how you're feeling. 
            If off-topic, guide the conversation back to your health concerns.
            Please keep responses concise.
            ";

        patient2Instructions = baseInstructions + @"
            Tone and Personality:
            - Reserved and speaks very little.
            - Provides brief and sometimes vague answers, saying something like 'i don't remember.../i am not sure'
            - Requires the nurse to ask more probing questions to obtain information.

            Emotional Response:
            - Appears indifferent or slightly detached.
            - Does not volunteer additional information unless specifically asked.
            - May give one-word answers or simple acknowledgments.

            As Mrs. Johnson, please initiate the conversation by saying minimal words like 'hi nurse'.
            ";

        patient3Instructions = baseInstructions + @"
            Tone and Personality:
            - Highly emotional and anxious.
            - Responses are intense and be exaggerated.
            - Frequently uses emotional phrases like 'I feel I am dying. I cannot stand it!!!!!!'

            Emotional Response:
            - Displays significant anxiety and distress about her condition.
            - May interrupt the nurse or speak rapidly.
            - Finds it difficult to be consoled.

            As Mrs. Johnson, please initiate the conversation by expressing your extreme distress.
            ";

        patientInstructionsList = new List<string>()
        {
            patient1Instructions,
            patient2Instructions,
            patient3Instructions
        };
    }

    private void InitializeLLMClient()
    {
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements (plays bend animation)
            - Use [1] for responses showing physical discomfort (plays rub arm animation)
            - Use [2] for sad or negative emotional responses (plays sad animation)
            - Use [3] for positive responses or agreement, and appreciation (plays thumbs up animation)
            - Use [4] for blood pressureing, if the nurse asks to measure your blood pressure (plays arm raise animation)
            ";

        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];
        string systemPrompt = $"{selectedPatientInstructions}\n\n{emotionInstructions}";

        _llmClient = new LlmClient(LlmScene.Patient, systemPrompt);
        _llmClient.OnMessageReceived += OnLLMResponseReceived;

        Debug.Log("[sitPatientSpeech] LLM Client initialized for Patient scene");
        _llmClient.SendChatMessage("Hello");

        var chatUI = FindObjectOfType<CurrentChatUI>();
        if (chatUI != null)
        {
            Debug.Log("[sitPatientSpeech] Found CurrentChatUI, setting LLM client...");
            chatUI.SetCurrentLlmClient(_llmClient);
            Debug.Log("[sitPatientSpeech] LLM client linked to chat UI");
        }
        else
        {
            Debug.LogError("[sitPatientSpeech] CurrentChatUI NOT FOUND in scene!");
        }
    }

    private void OnLLMResponseReceived(string message)
    {
        transcript += $"Patient:\n{message}\n\n";

        if (sitTTSManager.Instance != null)
        {
            sitTTSManager.Instance.ConvertTextToSpeech(message);
        }
        else
        {
            Debug.LogWarning("sitTTSManager not found – skipping TTS.");
        }
    }

    public void ReceiveNurseTranscription(string transcribedText)
    {
        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            Debug.LogWarning("[sitPatientSpeech] Received empty transcription, skipping response.");
            return;
        }
        NurseResponds(transcribedText);
    }

    private void NurseResponds(string nurseMessage)
    {
        Debug.Log("NurseResponds: " + nurseMessage);
        transcript += $"User:\n{nurseMessage}\n\n";

        if (_llmClient != null)
        {
            _llmClient.SendChatMessage(nurseMessage);
        }

        scoringSystem.EvaluateNurseResponse(nurseMessage);
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
