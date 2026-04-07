using UnityEngine;
using System.Collections;

public class CharacterAnimationController : MonoBehaviour, ICharacterAnimation
{
    [Header("血液效果（可选）")]
    [SerializeField] private BloodEffectController bloodEffectController;
    [SerializeField] private BloodTextController bloodTextController;

    [Header("默认动画配置")]
    [SerializeField] private string idleTriggerName = "idle";
    [SerializeField] private string talkingTriggerName = "talking";

    private Animator animator;
    private AnimationConfig config;

    public AnimationConfig Config => config;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void SetConfig(AnimationConfig newConfig)
    {
        config = newConfig;

        ValidateAnimatorParameters();

        Debug.Log($"[CharacterAnimationController] Config set: {config?.characterType}, maxEmotionCode: {config?.maxEmotionCode}");

        PlayIdle();
    }

    private void ValidateAnimatorParameters()
    {
        if (animator == null || config == null) return;

        if (config.emotionMappings != null)
        {
            foreach (var mapping in config.emotionMappings)
            {
                if (!mapping.isIdle && !string.IsNullOrEmpty(mapping.triggerName))
                {
                    bool hasTrigger = animator.HasParameterOfType(mapping.triggerName, AnimatorControllerParameterType.Trigger);
                    if (!hasTrigger)
                    {
                        Debug.LogWarning($"[CharacterAnimationController] Missing trigger parameter: '{mapping.triggerName}' in Animator Controller");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(idleTriggerName))
        {
            bool hasIdleTrigger = animator.HasParameterOfType(idleTriggerName, AnimatorControllerParameterType.Trigger);
            if (!hasIdleTrigger)
            {
                Debug.LogWarning($"[CharacterAnimationController] Idle trigger '{idleTriggerName}' not found");
            }
        }
    }

    public void UpdateAnimationState(int newState)
    {
        PlayByEmotionCode(newState);
    }
    
    public void PlayIdle()
    {
        if (animator != null && !string.IsNullOrEmpty(idleTriggerName))
        {
            if (animator.HasParameterOfType(idleTriggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(idleTriggerName);
                return;
            }
        }

        var idleMapping = config?.emotionMappings?.Find(m => m.isIdle);
        if (idleMapping != null && !string.IsNullOrEmpty(idleMapping.triggerName))
        {
            PlayAnimation(idleMapping.triggerName);
        }
    }
    
    public void PlayAnimation(string triggerName)
    {
        StartCoroutine(PlayAnimationWithDelay(triggerName));
    }
    
    public void PlayByEmotionCode(int emotionCode)
    {
        if (config == null)
        {
            Debug.LogWarning("[CharacterAnimationController] Config not loaded");
            PlayIdle();
            return;
        }
        
        if (emotionCode > config.maxEmotionCode)
        {
            Debug.LogWarning($"[CharacterAnimationController] Emotion code {emotionCode} out of range (max: {config.maxEmotionCode})");
            PlayIdle();
            return;
        }
        
        var mapping = config.GetMappingByEmotionCode(emotionCode);
        if (mapping == null)
        {
            PlayIdle();
            return;
        }
        
        if (mapping.isIdle)
        {
            PlayIdle();
        }
        else
        {
            PlayAnimation(mapping.triggerName);
        }
        
        if (mapping.triggerBloodEffect)
        {
            TriggerBloodEffect();
        }
    }
    
    public void PlayByName(string animationName)
    {
        if (config == null)
        {
            Debug.LogWarning("[CharacterAnimationController] Config not loaded");
            return;
        }
        
        var triggerName = config.GetTriggerByName(animationName);
        if (!string.IsNullOrEmpty(triggerName))
        {
            PlayAnimation(triggerName);
            
            var mapping = config.GetMappingByTriggerName(triggerName);
            if (mapping != null && mapping.triggerBloodEffect)
            {
                TriggerBloodEffect();
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterAnimationController] Animation '{animationName}' not found in config");
        }
    }
    
    public void TriggerBloodEffect()
    {
        if (bloodEffectController != null)
        {
            bloodEffectController.SetBloodVisibility(true);
        }
        if (bloodTextController != null)
        {
            bloodTextController.SetBloodTextVisibility(true);
        }
    }
    
    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        yield return new WaitForSeconds(delay);
        
        // 验证Animator参数是否存在
        if (animator != null && animator.HasParameterOfType(triggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(triggerName);
        }
        else
        {
            Debug.LogWarning($"[CharacterAnimationController] Animator trigger '{triggerName}' not found");
        }
    }
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayHeadPain() => PlayAnimation("pain");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayHappy() => PlayAnimation("happy");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayShrug() => PlayAnimation("shrug");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayHeadNod() => PlayAnimation("head_nod");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayHeadShake() => PlayAnimation("head_shake");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayWrithingInPain() => PlayAnimation("writhing_pain");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlaySad() => PlayAnimation("sad");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayArmStretch() => PlayAnimation("arm_stretch");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayNeckStretch() => PlayAnimation("neck_stretch");
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlayBloodPressure()
    {
        PlayAnimation("blood_pre");
        TriggerBloodEffect();
    }
    
    [System.Obsolete("Use PlayByEmotionCode instead")]
    public void PlaySittingTalking() => PlayAnimation("sitting_talking");
}

/// <summary>
/// Animator扩展方法,用于检查参数是否存在
/// </summary>
public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.parameters == null)
        {
            return false;
        }
        
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
            {
                return true;
            }
        }
        return false;
    }
}
