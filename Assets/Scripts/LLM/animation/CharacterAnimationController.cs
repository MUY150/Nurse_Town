using UnityEngine;
using System.Collections;

/// <summary>
/// 角色动画控制器，负责管理角色的各种动画状态和触发器
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - [SerializeField]序列化特性：让私有字段在Inspector中可编辑
/// - 协程（Coroutine）：使用IEnumerator和yield return实现延迟动画
/// - Unity生命周期方法：Start()
/// - Unity动画系统：Animator组件
/// - Animator.SetInteger()：设置整数动画参数
/// - Animator.SetTrigger()：触发动画触发器
/// - WaitForSeconds：协程等待方法
/// </remarks>
public class CharacterAnimationController : MonoBehaviour, ICharacterAnimation
{
    private Animator animator;
    [SerializeField] private int motionState = 0;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        UpdateAnimationState(motionState);
    }

    /// <summary>
    /// 更新动画状态
    /// </summary>
    /// <param name="newState">新的动画状态</param>
    public void UpdateAnimationState(int newState)
    {
        motionState = newState;
        animator.SetInteger("Motion", motionState);
    }

    /// <summary>
    /// 播放待机动画
    /// </summary>
    public void PlayIdle()
    {
        UpdateAnimationState(0);
    }

    /// <summary>
    /// 播放头痛动画
    /// </summary>
    public void PlayHeadPain()
    {
        StartCoroutine(PlayAnimationWithDelay("pain"));
    }

    /// <summary>
    /// 播放开心动画
    /// </summary>
    public void PlayHappy()
    {
        StartCoroutine(PlayAnimationWithDelay("happy"));
    }

    /// <summary>
    /// 播放耸肩动画
    /// </summary>
    public void PlayShrug()
    {
        StartCoroutine(PlayAnimationWithDelay("shrug"));
    }

    /// <summary>
    /// 播放点头动画
    /// </summary>
    public void PlayHeadNod()
    {
        StartCoroutine(PlayAnimationWithDelay("head_nod"));
    }

    /// <summary>
    /// 播放摇头动画
    /// </summary>
    public void PlayHeadShake()
    {
        StartCoroutine(PlayAnimationWithDelay("head_shake"));
    }

    /// <summary>
    /// 播放痛苦扭动动画
    /// </summary>
    public void PlayWrithingInPain()
    {
        StartCoroutine(PlayAnimationWithDelay("writhing_pain"));
    }

    /// <summary>
    /// 播放悲伤动画
    /// </summary>
    public void PlaySad()
    {
        StartCoroutine(PlayAnimationWithDelay("sad"));
    }

    /// <summary>
    /// 播放手臂伸展动画
    /// </summary>
    public void PlayArmStretch()
    {
        StartCoroutine(PlayAnimationWithDelay("arm_stretch"));
    }

    /// <summary>
    /// 播放颈部伸展动画
    /// </summary>
    public void PlayNeckStretch()
    {
        StartCoroutine(PlayAnimationWithDelay("neck_stretch"));
    }

    /// <summary>
    /// 播放血压测量动画
    /// </summary>
    public void PlayBloodPressure()
    {
        StartCoroutine(PlayAnimationWithDelay("blood_pre"));
    }

    /// <summary>
    /// 播放坐着说话动画
    /// </summary>
    public void PlaySittingTalking()
    {
        StartCoroutine(PlayAnimationWithDelay("sitting_talking"));
    }

    /// <summary>
    /// ICharacterAnimation 接口实现：播放指定动画
    /// </summary>
    /// <param name="triggerName">动画触发器名称</param>
    public void PlayAnimation(string triggerName)
    {
        StartCoroutine(PlayAnimationWithDelay(triggerName));
    }

    /// <summary>
    /// 带延迟播放动画的协程
    /// </summary>
    /// <param name="triggerName">动画触发器名称</param>
    /// <param name="delay">延迟时间（秒）</param>
    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
    }

}
