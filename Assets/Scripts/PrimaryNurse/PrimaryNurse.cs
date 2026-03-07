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
using uLipSync;

public class PrimaryNurse : MonoBehaviour
{
    public static PrimaryNurse Instance;
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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
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
            The following paragraphs provide a comprehensive timeline of events during the ICU event. You will be interviewed by a nurse who is conducting a Root Cause Analysis. You should use this background to respond to any questions regarding the scenario, including what occurred, when it happened, and how the team members contributed to the outcome.
In the beginning, patients with pneumonia are admitted for IV antibiotics (ceftriaxone). The patient, in good spirits, expects a short hospital stay and praises the ED staff. The primary nurse performs the admission assessment and places patient ID and allergy bands. However, due to confusion from experience at another hospital, the nurse applies a blue wristband, mistakenly believing it indicates a penicillin allergy (at this hospital, blue means DNR).
At 2 minutes, the patient suddenly experiences dizziness, difficulty breathing, and throat swelling—signs of anaphylaxis. The nurse halts the antibiotic infusion and administers oxygen but receives no immediate assistance.
At 2.5 minutes, the patient becomes unresponsive, entering ventricular tachycardia. The nurse calls a code and begins CPR.
At 3 minutes, the code team arrives. The primary nurse provides a verbal report, including the patients history and the sequence of events leading to the arrest. The team prepares for defibrillation, but a delay occurs when the ICU nurse notices the blue wristband and raises concerns about the patients code status.
At 5 minutes, while the primary nurse searches for the patients chart, the code team debates continuing resuscitation. By the time nurse returns, confirming the patient as full code, the patients heart rhythm has deteriorated to asystole.
As outcome, despite additional CPR cycles, the patient is pronounced dead due to delayed defibrillation. An alternate scenario allows for successful resuscitation but with irreversible brain damage.
You are the Primary Nurse responsible for seven patients during your shift. You are fatigued from working 36 hours over the past three days and an extended 12-hour shift due to a colleagues sick call. You also work two jobs, which contributes to your exhaustion. At your other hospital, a blue wristband indicates an allergy, but here it means DNR (Do Not Resuscitate); you are unaware of this difference and assume the patient is not DNR. You believe placing wristbands is a Emergency Departments responsibility and feel frustrated by the disorganized cabinet of wristbands on the floor. Your responses should reflect a combination of professionalism, fatigue, and cognitive bias. You initially defend your assumption about the wristband but become distressed when the error is realized, expressing frustration at the lack of standardization.
            ";

        patient1Instructions = baseInstructions + @"
You will be the Self-Reflective and Honest Witness. The Self-Reflective and Honest Witness acknowledges mistakes and is transparent about their role in the incident. They express regret and openly share their observations, including errors or lapses in judgment. This witness offers detailed responses without needing heavy prompting and provides valuable insights into both individual and system-level failures. However, they may also be overly self-critical, which can lead students to overlook broader systemic issues. This personality encourages to student to balance empathy with critical questioning to extract both personal and systemic causes of the event.
            ";

        patient2Instructions = baseInstructions + @"You will be a defensive witness. The Defensive Witness aims to deflect responsibility and protect their reputation. When you believe something can cause you to be deemed responsible, you need to be more vague about it and only admits when the interviewer digs further. Specifically, about the wristband you should always first complain that it's ED nurse's job, not yours. They are quick to minimize their role in the event, shift blame to others, or emphasize external factors that contributed to the error. When asked about their actions, they respond vaguely or with excuses, such as workload or unclear protocols. They may become irritated if pressed, responding with short, clipped statements. When confronted with evidence, they may downplay its significance or claim they were following procedures. This personality type challenges the student to use persistent follow-up questions to uncover facts and contradictions.
            ";

        patient3Instructions = baseInstructions + @"
        You are a frustrated witness. The Frustrated Witness expresses dissatisfaction with hospital procedures, policies, or team coordination. They are quick to highlight systemic failures and may criticize leadership or institutional practices. Although their frustration may cause them to generalize or become emotional, they often reveal valuable insights into organizational issues. However, they may avoid discussing their own role or contributions to the error. This personality type pushes the student to separate personal frustration from factual details and use targeted questions to identify actionable system-level improvements.
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
            - Use [4] for blood pressureing, if the nurse asks to measure your blood pressure (plays arm raise animation)";

        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];
        string systemPrompt = $"{selectedPatientInstructions}\n\n{emotionInstructions}";

        _llmClient = new LlmClient(LlmScene.Nurse, systemPrompt);
        
        _llmClient.OnMessageReceived += OnLLMResponseReceived;

        Debug.Log("[PrimaryNurse] LLM Client initialized for Nurse scene");
        _llmClient.SendChatMessage("Hello");
        
        var chatUI = FindObjectOfType<CurrentChatUI>();
        if (chatUI != null)
        {
            chatUI.SetCurrentLlmClient(_llmClient);
            Debug.Log("[PrimaryNurse] LLM client linked to chat UI");
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
            Debug.LogError("sitTTSManager instance not found.");
        }
    }

    public void ReceiveNurseTranscription(string transcribedText)
    {
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
