using UnityEngine;
using System.Collections;

/// <summary>
/// 坐姿角色动画控制器，管理坐姿角色的各种动画状态和触发器
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 私有字段：private成员变量
/// - Unity生命周期方法：Start()
/// - [SerializeField]特性：序列化字段，在Inspector中可编辑
/// - Animator动画系统：SetInteger()、SetTrigger()
/// - 协程（Coroutine）：使用IEnumerator和yield return实现延迟动画
/// - 泛型：GetComponent<T>()
/// - Unity API：Debug.Log()
/// - 整数参数：motionState控制动画状态
/// - WaitForSeconds：协程等待方法
/// - 动画触发器：string类型的触发器名称
/// </remarks>
public class sitCharacterAnimationController : MonoBehaviour, ICharacterAnimation
{
    // 私有字段：存储Animator组件
    private Animator animator;
    
    // [SerializeField]特性：序列化字段，在Inspector中可编辑
    [SerializeField] private int motionState = 0;
    
    /// <summary>
    /// Unity生命周期方法：初始化时调用
    /// </summary>
    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();
        // 初始化动画状态
        UpdateAnimationState(motionState);
    }

    /// <summary>
    /// 更新动画状态
    /// </summary>
    /// <param name="newState">新的动画状态值</param>
    public void UpdateAnimationState(int newState)
    {
        // 更新状态值
        motionState = newState;
        // 设置Animator的整数参数
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
    /// 播放弯腰动画
    /// </summary>
    public void PlayBend()
    {
        // 协程：使用延迟播放动画
        StartCoroutine(PlayAnimationWithDelay("bend"));
    }

    /// <summary>
    /// 播放坐姿对话动画
    /// </summary>
    public void PlaySittingTalking()
    {
        // 协程：使用延迟播放动画
        StartCoroutine(PlayAnimationWithDelay("sitting_talking"));
    }

    /// <summary>
    /// 播放悲伤动画
    /// </summary>
    public void PlaySad()
    {
        // 协程：使用延迟播放动画
        StartCoroutine(PlayAnimationWithDelay("sad"));
    }

    /// <summary>
    /// 播放竖大拇指动画
    /// </summary>
    public void PlayThumbUp()
    {
        // 协程：使用延迟播放动画
        StartCoroutine(PlayAnimationWithDelay("thumb_up"));
    }

    /// <summary>
    /// 播放揉手臂动画
    /// </summary>
    public void PlayRubArm()
    {
        // 协程：使用延迟播放动画
        StartCoroutine(PlayAnimationWithDelay("rub_arm"));
    }

    /// <summary>
    /// 播放血压测量动画
    /// </summary>
    public void PlayBloodPressure()
    {
        // 协程：使用延迟播放动画
        StartCoroutine(PlayAnimationWithDelay("BP"));
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
    /// 延迟播放动画的协程方法
    /// </summary>
    /// <param name="triggerName">动画触发器名称</param>
    /// <param name="delay">延迟时间（秒）</param>
    /// <returns>IEnumerator，用于协程</returns>
    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        // 协程：等待指定时间
        yield return new WaitForSeconds(delay);
        // 触发动画
        animator.SetTrigger(triggerName);
    }

}
