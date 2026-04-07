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
    
    public EmotionMapping GetMappingByTriggerName(string triggerName)
    {
        if (emotionMappings == null) return null;
        foreach (var mapping in emotionMappings)
        {
            if (mapping.triggerName == triggerName)
            {
                return mapping;
            }
        }
        return null;
    }

    public string GetAnimationDescription(string name)
    {
        if (animationDescriptions == null) return null;
        return animationDescriptions.TryGetValue(name, out var desc) ? desc : null;
    }

    public string GetEffectIdForAnimation(string animationName)
    {
        if (namedAnimations == null || emotionMappings == null) return null;

        string triggerName = namedAnimations.TryGetValue(animationName, out var t) ? t : null;
        if (string.IsNullOrEmpty(triggerName)) return null;

        foreach (var mapping in emotionMappings)
        {
            if (mapping.triggerName == triggerName && !string.IsNullOrEmpty(mapping.effectId))
            {
                return mapping.effectId;
            }
        }
        return null;
    }

    public float GetEffectDelayForAnimation(string animationName)
    {
        if (namedAnimations == null || emotionMappings == null) return 0f;

        string triggerName = namedAnimations.TryGetValue(animationName, out var t) ? t : null;
        if (string.IsNullOrEmpty(triggerName)) return 0f;

        foreach (var mapping in emotionMappings)
        {
            if (mapping.triggerName == triggerName)
            {
                return mapping.effectDelay;
            }
        }
        return 0f;
    }
}

[Serializable]
public class EmotionMapping
{
    public int emotionCode;
    public string triggerName;
    public bool isIdle;
    public bool triggerBloodEffect;
    public string effectId;
    public float effectDelay = 0f;
}
