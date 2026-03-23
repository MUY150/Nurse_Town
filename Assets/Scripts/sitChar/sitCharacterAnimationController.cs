using UnityEngine;
using System;

[System.Obsolete("Use CharacterAnimationController with sitting.json config instead. This class will be removed in v2.0")]
public class sitCharacterAnimationController : MonoBehaviour, ICharacterAnimation
{
    [SerializeField] private BloodEffectController bloodEffectController;
    [SerializeField] private BloodTextController bloodTextController;
    
    private CharacterAnimationController _innerController;
    
    void Awake()
    {
        _innerController = gameObject.AddComponent<CharacterAnimationController>();
        _innerController.SetConfig("sitting");
        
        var bloodField = typeof(CharacterAnimationController).GetField("bloodEffectController", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bloodTextField = typeof(CharacterAnimationController).GetField("bloodTextController", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        bloodField?.SetValue(_innerController, bloodEffectController);
        bloodTextField?.SetValue(_innerController, bloodTextController);
        
        Debug.Log("[sitCharacterAnimationController] Migrated to CharacterAnimationController with sitting config");
    }
    
    void Start()
    {
        enabled = false;
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
