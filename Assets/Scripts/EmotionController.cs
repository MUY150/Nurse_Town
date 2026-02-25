using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

/// <summary>
/// 情绪控制器，管理角色情绪的播放和切换
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - Unity Timeline系统：PlayableDirector、TimelineAsset
/// - Unity生命周期方法：Start()
/// - 公共字段：public成员变量
/// - [SerializeField]特性：序列化字段，在Inspector中可编辑
/// - 泛型：GetComponent<T>()、ToList()
/// - LINQ查询：ToList()
/// - foreach循环：遍历集合
/// - Unity API：Debug.Log()、Debug.LogError()
/// - 类型转换：as关键字进行安全类型转换
/// - 条件判断：if语句
/// - 字符串操作：字符串连接和格式化
/// </remarks>
public class EmotionController : MonoBehaviour
{
    // 公共字段：PlayableDirector组件，用于控制Timeline动画
    public PlayableDirector director;
    
    // 控制是否设置情绪代码
    public bool setEmotionCode = true;
    
    // 当前情绪代码
    public int currentEmotionCode;
    
    /* 情绪代码映射表：
        "Neutral", // 0
        "Discomfort", // 1
        "Happy", // 2
        "Pain", // 3
        "Sad", // 4
        "Anger" // 5
    */
    
    /// <summary>
    /// Unity生命周期方法：初始化时调用
    /// </summary>
    void Start()
    {
        // 验证PlayableDirector组件
        if (director == null)
        {
            Debug.LogError("PlayableDirector not assigned.");
        }
    }
    
    /// <summary>
    /// 处理情绪代码并选择对应的动画轨道
    /// </summary>
    /// <param name="emotionCode">情绪代码（0-5）</param>
    public void HandleEmotionCode(int emotionCode)
    {
        // 类型转换：将playableAsset安全转换为TimelineAsset
        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        
        // 根据设置更新当前情绪代码
        if (!setEmotionCode) { currentEmotionCode = emotionCode;}

        // 验证TimelineAsset是否有效
        if (timeline == null)
        {
            Debug.LogError("No TimelineAsset assigned to the PlayableDirector.");
            return;
        }
        
        // 获取所有输出轨道并转换为列表
        var allTracks = timeline.GetOutputTracks().ToList();

        // 检查轨道索引是否越界
        if (currentEmotionCode < 0 || currentEmotionCode >= allTracks.Count)
        {
            Debug.LogError("Track index out of bounds.");
            return;
        }
        
        // 选择对应的轨道
        TrackAsset selectedTrack = allTracks[currentEmotionCode];
        
        // 输出调试信息
        Debug.Log("Emotion Code: " + emotionCode);
        Debug.Log($"Selected track: {selectedTrack.name}");

        // 遍历所有轨道，隐藏非选中轨道
        foreach (var track in allTracks)
        {
            if (track.name != "Blink Track") 
                // 静音轨道（除了闪烁轨道）
                track.muted = (track != selectedTrack);
        }
    }

    /// <summary>
    /// 播放情绪动画
    /// </summary>
    public void PlayEmotion()
    {
        // 重建Timeline图
        director.RebuildGraph();
        // 播放Timeline动画
        director.Play();
    }
}
