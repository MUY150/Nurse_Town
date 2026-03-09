using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class PatientConfigLoader
{
    private static Dictionary<string, PatientProfile> configCache = new Dictionary<string, PatientProfile>();
    
    public static PatientProfile LoadFromFile(string scenarioName)
    {
        if (configCache.ContainsKey(scenarioName))
        {
            return configCache[scenarioName];
        }
        
        string basePath = Path.Combine(Application.streamingAssetsPath, "Prompts", scenarioName);
        
        if (!Directory.Exists(basePath))
        {
            Debug.LogError($"Scenario directory not found: {basePath}");
            return LoadDefault();
        }
        
        PatientProfile profile = new PatientProfile();
        profile.scenarioName = scenarioName;
        
        string configPath = Path.Combine(basePath, "config.json");
        if (File.Exists(configPath))
        {
            try
            {
                string configJson = File.ReadAllText(configPath);
                ScenarioConfig config = JsonConvert.DeserializeObject<ScenarioConfig>(configJson);
                
                profile.patientName = config.patientName;
                profile.maxEmotionCode = config.maxEmotionCode;
                profile.useTimelineEmotion = config.useTimelineEmotion;
                profile.useAnimatorEmotion = config.useAnimatorEmotion;
                profile.enableChatUI = config.enableChatUI;
                profile.variantFiles = config.variantFiles;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load config.json: {e.Message}");
            }
        }
        
        string baseInstructionsPath = Path.Combine(basePath, "baseInstructions.txt");
        if (File.Exists(baseInstructionsPath))
        {
            profile.emotionInstructions = File.ReadAllText(baseInstructionsPath);
        }
        
        if (profile.variantFiles != null && profile.variantFiles.Length > 0)
        {
            foreach (string variantFile in profile.variantFiles)
            {
                string variantPath = Path.Combine(basePath, variantFile);
                if (File.Exists(variantPath))
                {
                    string variantContent = File.ReadAllText(variantPath);
                    string fullPrompt = $"{profile.emotionInstructions}\n\n{variantContent}";
                    profile.patientInstructionsList.Add(fullPrompt);
                }
            }
        }
        
        if (profile.patientInstructionsList.Count == 0)
        {
            Debug.LogWarning($"No patient variants found for scenario: {scenarioName}");
        }
        
        configCache[scenarioName] = profile;
        return profile;
    }
    
    public static PatientProfile LoadDefault()
    {
        PatientProfile profile = new PatientProfile();
        profile.scenarioName = "default";
        profile.patientName = "Default Patient";
        profile.maxEmotionCode = 5;
        profile.useTimelineEmotion = true;
        profile.useAnimatorEmotion = false;
        profile.enableChatUI = false;
        profile.emotionInstructions = "You are a patient. Respond to the nurse's questions.";
        profile.patientInstructionsList.Add(profile.emotionInstructions);
        return profile;
    }
    
    public static void ClearCache()
    {
        configCache.Clear();
    }
}
