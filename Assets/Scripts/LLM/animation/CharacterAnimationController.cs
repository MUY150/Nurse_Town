using UnityEngine;
using System.Collections;

public class CharacterAnimationController : MonoBehaviour, ICharacterAnimation
{
    [Header("配置")]
    [SerializeField] private string characterId = "hypertensionPatient";
    [SerializeField] private bool loadFromFile = true;
    
    [Header("血液效果（可选）")]
    [SerializeField] private BloodEffectController bloodEffectController;
    [SerializeField] private BloodTextController bloodTextController;
    
    private Animator animator;
    private AnimationConfig config;
    private int motionState = 0;
    
    public AnimationConfig Config => config;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        LoadConfig();
        UpdateAnimationState(motionState);
    }
    
    private void LoadConfig()
    {
        if (loadFromFile)
        {
            config = AnimationConfigLoader.LoadFromFile(characterId);
        }
        else
        {
            config = AnimationConfigLoader.LoadFromFile(characterId);
        }

        Debug.Log($"[CharacterAnimationController] Loaded config: {config.characterType}, maxEmotionCode: {config.maxEmotionCode}");
    }

    public void SetConfig(string newCharacterId)
    {
        characterId = newCharacterId;
        LoadConfig();
    }
    
    public void UpdateAnimationState(int newState)
    {
        motionState = newState;
        animator.SetInteger("Motion", motionState);
    }
    
    public void PlayIdle()
    {
        UpdateAnimationState(0);
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
    
    private void TriggerBloodEffect()
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
        animator.SetTrigger(triggerName);
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
