using System;
using System.Collections.Generic;

[Serializable]
public class EmotionMappingConfig
{
    public List<EmotionTtsMapping> mappings;

    private Dictionary<string, EmotionTtsMapping> _mappingDict;

    public EmotionTtsMapping GetMapping(string emotion)
    {
        if (_mappingDict == null)
        {
            _mappingDict = new Dictionary<string, EmotionTtsMapping>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in mappings)
            {
                _mappingDict[mapping.emotion] = mapping;
            }
        }

        return _mappingDict.TryGetValue(emotion, out var result) ? result : GetMapping("neutral");
    }
}

[Serializable]
public class EmotionTtsMapping
{
    public string emotion;
    public string description;
    public float speedModifier;
    public float pitchModifier;
    public float volumeModifier;
    public string voiceSuggestion;
    public string[] exampleTexts;
}
