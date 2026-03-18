using System;
using System.Collections.Generic;

[Serializable]
public class AnimationConfig
{
    public string characterType;
    public string characterId;
    public string description;
    public int maxEmotionCode;
    public List<EmotionMapping> emotionMappings;
    public Dictionary<string, string> namedAnimations;
    public Dictionary<string, string> animationDescriptions;
    
    private Dictionary<int, EmotionMapping> _emotionMap;
    
    public EmotionMapping GetMappingByEmotionCode(int emotionCode)
    {
        if (_emotionMap == null)
        {
            _emotionMap = new Dictionary<int, EmotionMapping>();
            foreach (var mapping in emotionMappings)
            {
                _emotionMap[mapping.emotionCode] = mapping;
            }
        }
        
        return _emotionMap.TryGetValue(emotionCode, out var result) ? result : null;
    }
    
    public string GetTriggerByName(string name)
    {
        if (namedAnimations == null) return null;
        return namedAnimations.TryGetValue(name, out var trigger) ? trigger : null;
    }

    public string GetAnimationDescription(string name)
    {
        if (animationDescriptions == null) return null;
        return animationDescriptions.TryGetValue(name, out var desc) ? desc : null;
    }
}

[Serializable]
public class EmotionMapping
{
    public int emotionCode;
    public string triggerName;
    public bool isIdle;
    public bool triggerBloodEffect;
}
