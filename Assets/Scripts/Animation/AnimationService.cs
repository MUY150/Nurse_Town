using UnityEngine;

public class AnimationService : Singleton<AnimationService>
{
    private CharacterAnimationController _currentController;
    private AnimationConfig _currentConfig;
    private string _currentCharacterId;

    public CharacterAnimationController CurrentController => _currentController;
    public AnimationConfig CurrentConfig => _currentConfig;
    public string CurrentCharacterId => _currentCharacterId;

    public void SetCharacter(CharacterAnimationController controller, string configName)
    {
        _currentController = controller;
        _currentConfig = AnimationConfigLoader.LoadFromFile(configName);
        _currentCharacterId = configName;
        
        Debug.Log($"[AnimationService] Character set: {configName}");
    }

    public void PlayAnimation(string animationName)
    {
        if (_currentController == null)
        {
            Debug.LogWarning("[AnimationService] No controller set");
            return;
        }

        _currentController.PlayByName(animationName);
    }

    public void PlayByEmotionCode(int emotionCode)
    {
        if (_currentController == null)
        {
            Debug.LogWarning("[AnimationService] No controller set");
            return;
        }

        _currentController.PlayByEmotionCode(emotionCode);
    }

    public string[] GetAvailableAnimations()
    {
        if (_currentConfig?.namedAnimations == null)
        {
            return new string[0];
        }

        var keys = new string[_currentConfig.namedAnimations.Count];
        _currentConfig.namedAnimations.Keys.CopyTo(keys, 0);
        return keys;
    }
}
