// ============================================================================
// 文件名: AC_ICUDoctor.cs
// 功能描述: ICU医生动画控制器
// 作者: AI Assistant
// 创建日期: 2026-01-11
// 修改记录: 添加详细中文注释，标注C#特性和Unity API
// ============================================================================

using UnityEngine;
using System.Collections;

/// <summary>
/// ICU医生动画控制器
/// 负责管理ICU医生角色的动画状态和播放
/// </summary>
/// <remarks>
/// C#特性说明:
/// - MonoBehaviour: Unity所有脚本的基础类，提供生命周期方法
/// - [SerializeField]: Unity序列化特性，让私有字段在Inspector中可编辑
/// - 协程(Coroutine): Unity的异步执行机制
/// </remarks>
public class AIDoctorAnimationController : MonoBehaviour
{
    // ============================================================================
    // Unity特性说明: [SerializeField]
    // ============================================================================
    // [SerializeField]是Unity的序列化特性
    // 让私有字段可以在Inspector面板中编辑
    // 类似C++中需要手动实现编辑器UI，C#通过特性自动处理
    // ============================================================================
    
    private Animator animator;
    [SerializeField] private int motionState = 0;
    
    // ============================================================================
    // C#特性说明: 注释和字段说明
    // ============================================================================
    // Priority: Gesturing (eg. wristband) - 手势优先级
    // isHighPriorityAnimationPlaying - 标记高优先级动画是否正在播放
    // highPriorityDuration - 高优先级动画的持续时间
    // ============================================================================
    
    // Priority: Gesturing (eg. wristband)
    private bool isHighPriorityAnimationPlaying = false;
    
    // Adjust the duration of priority animation 
    [SerializeField] private float highPriorityDuration = 1.5f; 
    
    // ============================================================================
    // Unity生命周期方法: Start()
    // ============================================================================
    // Start在Awake之后、第一帧更新之前调用
    // 适合用于初始化组件和设置初始状态
    // ============================================================================
    
    void Start()
    {
        // GetComponent获取对象上的组件
        animator = GetComponent<Animator>();
        
        // default setting: Frustrated（沮丧）
        PlayFrustrated();
    }

    // 
    public void UpdateAnimationState(int newState)
    {
        motionState = newState;
        animator.SetInteger("Motion", motionState);
    }

    // play idle: code 0
    public void PlayIdle()
    {
        isHighPriorityAnimationPlaying = false;
        UpdateAnimationState(0);
    }

    /// <summary>
    /// trigger gesturing according to the doctor's speech，eg. "wrist band" or "yes" (priority)
    /// </summary>
    /// <param name="speech">doctor's speech</param>
    public void ProcessDoctorSpeech(string speech)
    {
        string lowerSpeech = speech.ToLower();

        if (lowerSpeech.Contains("wrist band"))
        {
            StartCoroutine(PlayHighPriorityAnimation("wrist_band"));
        }
        else if (lowerSpeech.Contains("yes") || lowerSpeech.Contains("yea") || lowerSpeech.Contains("yeah"))
        {
            StartCoroutine(PlayHighPriorityAnimation("head_nod"));
        }
    }

    private IEnumerator PlayHighPriorityAnimation(string triggerName, float delay = 0.0f)
    {
        isHighPriorityAnimationPlaying = true;
        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
        // after completing the priority animation, flag it to false.
        yield return new WaitForSeconds(highPriorityDuration);
        isHighPriorityAnimationPlaying = false;
    }

    /// <summary>
    /// before playing animation, check whether there are gesturing
    /// </summary>
    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        if (isHighPriorityAnimationPlaying)
            yield break;

        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
    }

    // interface for WD_EmotionAnimation

    public void PlaySad()
    {
        if (isHighPriorityAnimationPlaying)
            return;
        StartCoroutine(PlayAnimationWithDelay("sad"));
    }

    public void PlayHappy()
    {
        if (isHighPriorityAnimationPlaying)
            return;
        StartCoroutine(PlayAnimationWithDelay("happy"));
    }

    public void PlayFrustrated()
    {
        if (isHighPriorityAnimationPlaying)
            return;
        StartCoroutine(PlayAnimationWithDelay("frustrated"));
    }
}