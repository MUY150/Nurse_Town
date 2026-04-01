using UnityEngine;
using System;
using System.Collections;

[System.Obsolete(
    "This class is deprecated and will be removed in v2.0. " +
    "Use AnimationStateMachine instead. " +
    "See migration guide: docs/migration/animation-system-v2.md",
    true)]
public class sitCharacterAnimationController : MonoBehaviour, ICharacterAnimation
{
    [SerializeField] private BloodEffectController bloodEffectController;
    [SerializeField] private BloodTextController bloodTextController;
    
    private CharacterAnimationController _innerController;
    
    void Awake()
    {
        _innerController = gameObject.AddComponent<CharacterAnimationController>();
    }
    
    void Start()
    {
        enabled = false;
        
        AnimationService.Instance.OnConfigChanged += OnAnimationConfigChanged;
        
        StartCoroutine(InitializeConfig());
    }
    
    void OnAnimationConfigChanged(AnimationConfig config)
    {
        if (config != null)
        {
            _innerController.SetConfig(config);
            Debug.Log("[sitCharacterAnimationController] Config loaded from AnimationService event");
        }
    }
    
    void OnDestroy()
    {
        if (AnimationService.Instance != null)
        {
            AnimationService.Instance.OnConfigChanged -= OnAnimationConfigChanged;
        }
    }
    
    public void InitializeFromAnimationService(string configName)
    {
        AnimationService.Instance.SetCharacter(_innerController, configName);
        Debug.Log($"[sitCharacterAnimationController] Initialized with config: {configName}");
    }
    
    IEnumerator InitializeConfig()
    {
        yield return null;
        
        var sittingConfig = AnimationService.Instance.CurrentConfig;
        _innerController.SetConfig(sittingConfig);
        
        var bloodField = typeof(CharacterAnimationController).GetField("bloodEffectController", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bloodTextField = typeof(CharacterAnimationController).GetField("bloodTextController", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        bloodField?.SetValue(_innerController, bloodEffectController);
        bloodTextField?.SetValue(_innerController, bloodTextController);
        
        Debug.Log("[sitCharacterAnimationController] Migrated to CharacterAnimationController with sitting config from AnimationService");
    }
    
    public void UpdateAnimationState(int newState)
    {
        _innerController?.UpdateAnimationState(newState);
    }
    
    public void PlayIdle()
    {
        _innerController?.PlayIdle();
    }
    
    public void PlayAnimation(string triggerName)
    {
        _innerController?.PlayAnimation(triggerName);
    }
    
    public void PlayByEmotionCode(int emotionCode)
    {
        _innerController?.PlayByEmotionCode(emotionCode);
    }
    
    public void PlayByName(string animationName)
    {
        _innerController?.PlayByName(animationName);
    }
    
    public void TriggerBloodEffect()
    {
        _innerController?.TriggerBloodEffect();
    }
    
    public void PlayBend() => PlayAnimation("bend");
    public void PlaySittingTalking() => PlayAnimation("sitting_talking");
    public void PlaySad() => PlayAnimation("sad");
    public void PlayThumbUp() => PlayAnimation("thumb_up");
    public void PlayRubArm() => PlayAnimation("rub_arm");
    
    public void PlayBloodPressure()
    {
        PlayAnimation("BP");
        bloodEffectController?.SetBloodVisibility(true);
    }
}
