using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ScoringSystem
{
    public int totalScore = 5;
    public int interactionCount = 0;

    private List<string> pointsAddedReasons = new List<string>();
    private List<string> pointsDeductedReasons = new List<string>();

    private ILlmClient _llmClient;
    private MonoBehaviour _owner;
    private string _pendingResponse;
    private bool _isEvaluating = false;
    private string _mainSessionId;

    public void Initialize(ILlmClient llmClient, MonoBehaviour owner)
    {
        _llmClient = llmClient;
        _owner = owner;
        _mainSessionId = llmClient?.SessionId;
    }

    public void Initialize(MonoBehaviour owner)
    {
        _owner = owner;
        _llmClient = new LlmClient(LlmScene.Evaluation, "You are an expert nursing instructor. Evaluate nurse responses based on the provided criteria.", enableLogging: true);
    }

    public void EvaluateNurseResponse(string nurseResponse)
    {
        if (_owner == null)
        {
            Debug.LogError("[ScoringSystem] Owner is null. Cannot evaluate.");
            return;
        }

        _pendingResponse = nurseResponse;
        _owner.StartCoroutine(EvaluateResponseCoroutine(nurseResponse));
    }

    private IEnumerator EvaluateResponseCoroutine(string nurseResponse)
    {
        while (_isEvaluating)
        {
            yield return null;
        }

        _isEvaluating = true;

        string prompt = $"You are an expert nursing instructor. Evaluate the following nurse's response based on the criteria provided. Provide a JSON object with the evaluation results.\n\n" +
                        $"Nurse's Response: \"{nurseResponse}\"\n\n" +
                        "Scoring Criteria:\n" +
                        "- Deduct 1 point if medical jargon was used without explanation.\n" +
                        "- Add 2 points if the nurse mentions printing a list or sending an email.\n" +
                        "- Add 2 points if the nurse mentions the \"0-10 scale\" of pain or other discomfort.\n\n" +
                        "Provide the output in the following JSON format:\n" +
                        "{\n" +
                        "  \"pointsAdded\": <number>,\n" +
                        "  \"pointsDeducted\": <number>,\n" +
                        "  \"reason\": \"<detailed explanation of what the nurse did well or could improve>\"\n" +
                        "}";

        if (_llmClient == null)
        {
            Debug.LogError("[ScoringSystem] LLM Client is null. Creating a new one...");
            _llmClient = new LlmClient(LlmScene.Evaluation, "You are an expert nursing instructor. Evaluate nurse responses based on the provided criteria.", enableLogging: true);
        }

        Action<string> onResponse = null;
        onResponse = (response) =>
        {
            ProcessEvaluationResponse(response);
            _llmClient.OnMessageReceived -= onResponse;
        };

        _llmClient.OnMessageReceived += onResponse;
        _llmClient.SendChatMessage(prompt);

        interactionCount++;

        yield return new WaitForSeconds(2.0f);
        _isEvaluating = false;

        if (interactionCount >= 5)
        {
            GenerateReport();
        }
    }

    private void ProcessEvaluationResponse(string responseContent)
    {
        try
        {
            var evaluationResult = JsonConvert.DeserializeObject<EvaluationResult>(responseContent);
            int pointsAdded = evaluationResult.pointsAdded;
            int pointsDeducted = evaluationResult.pointsDeducted;
            string reason = evaluationResult.reason;

            totalScore += pointsAdded - pointsDeducted;

            if (pointsAdded > 0)
            {
                pointsAddedReasons.Add(reason);
                Debug.Log($"Well done! {pointsAdded} points added because {reason}");
            }
            if (pointsDeducted > 0)
            {
                pointsDeductedReasons.Add(reason);
                Debug.Log($"{pointsDeducted} points deducted because {reason}");
            }
            if (pointsAdded == 0 && pointsDeducted == 0)
            {
                Debug.Log("Good! Keep going!");
            }

            Debug.Log($"This was your {interactionCount} response.");
            
            if (_llmClient != null)
            {
                SessionAggregator.Instance.AddMessage(_llmClient.SessionId, "assistant", responseContent);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse evaluation result: " + ex.Message);
        }
    }

    public void GenerateReport()
    {
        Debug.Log("===== Evaluation Report =====");
        Debug.Log($"Total Score: {totalScore}");

        Debug.Log("Things you did well:");
        foreach (var reason in pointsAddedReasons)
        {
            Debug.Log("- " + reason);
        }

        Debug.Log("Things you could improve:");
        foreach (var reason in pointsDeductedReasons)
        {
            Debug.Log("- " + reason);
        }

        Debug.Log("=============================");
        
        string report = $"Total Score: {totalScore}\n\nWell done:\n{string.Join("\n", pointsAddedReasons)}\n\nImprove:\n{string.Join("\n", pointsDeductedReasons)}";
        
        if (_llmClient != null)
        {
            SessionAggregator.Instance.SetScoringResult(_llmClient.SessionId, report);
        }

        totalScore = 5;
        pointsAddedReasons.Clear();
        pointsDeductedReasons.Clear();
        interactionCount = 0;
    }
}

public class EvaluationResult
{
    public int pointsAdded;
    public int pointsDeducted;
    public string reason;
}
