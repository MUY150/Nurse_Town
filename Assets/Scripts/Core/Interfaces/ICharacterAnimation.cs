/// <summary>
/// 动画控制器接口，统一站立和坐姿角色的动画调用
/// </summary>
public interface ICharacterAnimation
{
    /// <summary>
    /// 播放空闲动画
    /// </summary>
    void PlayIdle();
    
    /// <summary>
    /// 播放指定动画
    /// </summary>
    /// <param name="triggerName">动画触发器名称</param>
    void PlayAnimation(string triggerName);
    
    /// <summary>
    /// 更新动画状态
    /// </summary>
    /// <param name="state">状态值</param>
    void UpdateAnimationState(int state);
    
    /// <summary>
    /// 根据情绪代码播放动画
    /// </summary>
    /// <param name="emotionCode">情绪代码</param>
    void PlayByEmotionCode(int emotionCode);
    
    /// <summary>
    /// 根据动画名称播放动画
    /// </summary>
    /// <param name="animationName">动画名称</param>
    void PlayByName(string animationName);
    
    /// <summary>
    /// 触发血液效果
    /// </summary>
    void TriggerBloodEffect();
}
