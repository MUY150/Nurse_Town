using System;
using System.Collections.Generic;

[Serializable]
public class PatientProfile
{
    public string scenarioName;
    public string patientName;
    public string emotionInstructions;
    public int maxEmotionCode = 5;
    public bool useTimelineEmotion = true;
    public bool useAnimatorEmotion = false;
    public bool enableChatUI = false;
    public string[] variantFiles;
    
    public string baseSkillContent;
    public string patientSkillContent;
    
    public List<string> patientInstructionsList;
    
    public PatientProfile()
    {
        patientInstructionsList = new List<string>();
    }
}

[Serializable]
public class ScenarioConfig
{
    public string scenarioName;
    public string patientName;
    public int maxEmotionCode = 5;
    public bool useTimelineEmotion = true;
    public bool useAnimatorEmotion = false;
    public bool enableChatUI = false;
    public string[] variantFiles;
}
