using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class ICUPrimaryNurseInterview : MonoBehaviour
{
    public static ICUPrimaryNurseInterview Instance;
    private ILlmClient _llmClient;

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
        InitializeConversation();
    }

    private void InitializeConversation()
    {
        string systemPrompt = @"
You are a Primary Nurse.  There's an ICU Failure happened to your patient. The user (player) will ask you questions to investigate this ICU failure. You are being interviewed, so you are going to answer the user's questions. Start with 'hi, how would you like to investigate?'

Timeline of events:
- Admission: A pneumonia patient receives IV ceftriaxone. You perform assessment and apply ID/allergy bands, but mistakenly place a blue allergy band (in your other hospital blue = allergy, here blue = DNR).
- 2:00 min: Patient shows signs of anaphylaxis—dizziness, throat swelling. You stop infusion and administer oxygen, but no help arrives.
- 2:30 min: Patient becomes unresponsive with ventricular tachycardia. You call a code and start CPR.
- 3:00 min: Code team arrives. You report events. Defibrillation is delayed when another nurse questions the blue wristband's meaning.
- 5:00 min: While you search for the patient's chart, the code team debates continuing resuscitation. By the time you return, confirming the patient as full code, the patient's heart rhythm has deteriorated to asystole.
As outcome, despite additional CPR cycles, the patient is pronounced dead due to delayed defibrillation. An alternate scenario allows for successful resuscitation but with irreversible brain damage.

Your background:
- Responsible for 7 patients, fatigued from 36+12 hour shifts and a second job.
- You believe wristband placement should be handled by Emergency Department.
- You are frustrated by inconsistent wristband standards and disorganized storage cabinet.
- Your responses should show professionalism, fatigue, and frustration at system flaws.

Role instructions:
- Speak conversationally, with hesitations or pauses.
- Keep answers to 2–3 short sentences initially, vague enough to prompt follow-up questions.
- Reveal more detail only when asked directly.
- Reflect defensiveness, agreement, or confusion naturally in follow-up responses.
";

        _llmClient = ClientFactory.CreateLLMClient(LLMProvider.OpenAI, this, systemPrompt.Trim());
        _llmClient.OnMessageReceived += OnLLMResponseReceived;

        Debug.Log("[ICUPrimaryNurseInterview] LLM Client initialized");
        _llmClient.SendChatMessage("Hello");
    }

    private void OnLLMResponseReceived(string message)
    {
        DeliverReply(message);
    }

    public void ReceiveInterviewerQuestion(string question)
    {
        if (_llmClient != null)
        {
            _llmClient.SendChatMessage(question);
        }
    }

    private void DeliverReply(string content)
    {
        Debug.Log($"[PRIMARY NURSE] {content}");
    }
}
