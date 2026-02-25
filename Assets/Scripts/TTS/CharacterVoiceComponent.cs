using System.Collections;
using UnityEngine;

/// <summary>
/// 将此组件附加到任何需要特定语音配置文件的角色上
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 协程（Coroutine）：使用IEnumerator和yield return实现异步操作
/// - Unity生命周期方法：Start()
/// - 泛型：GetComponent<T>()、FindObjectOfType<T>()
/// - 字符串插值：$""语法构建字符串
/// - 属性（Property）：自动属性
/// - 条件运算符：三元运算符 ?:
/// - switch语句：多分支选择结构
/// </remarks>
public class CharacterVoiceComponent : MonoBehaviour
{
    [Tooltip("The voice profile asset for this character")]
    public CharacterVoiceProfile voiceProfile;

    [Tooltip("Reference to the TTSManager (optional, will find it if not set)")]
    public TTSManager ttsManager;

    [Tooltip("Reference to this character's facial animation controller (optional)")]
    public CSVFacialAnimationController facialAnimationController;

    // 跟踪此角色是否正在说话
    private bool isSpeaking = false;

    void Start()
    {
        // 如果未分配，查找TTSManager
        if (ttsManager == null)
        {
            // 泛型方法：FindObjectOfType<T>()查找指定类型的组件
            ttsManager = FindObjectOfType<TTSManager>();
            if (ttsManager == null)
            {
                Debug.LogError("No TTSManager found in the scene!");
                return;
            }
        }

        // 如果未分配，查找面部动画控制器
        if (facialAnimationController == null)
        {
            // 泛型方法：GetComponentInChildren<T>()在子对象中查找组件
            facialAnimationController = GetComponentInChildren<CSVFacialAnimationController>();
        }

        // 验证语音配置文件
        if (voiceProfile == null)
        {
            // 字符串插值：$""语法
            Debug.LogError($"No voice profile assigned to character: {gameObject.name}");
        }
    }

    /// <summary>
    /// 让此角色说出提供的文本
    /// </summary>
    /// <param name="text">要说的文本</param>
    /// <param name="emotionCode">可选的情绪代码（0-10）</param>
    public void Speak(string text, int emotionCode = 0)
    {
        if (voiceProfile == null || ttsManager == null) return;

        // 如果提供了情绪代码，格式化文本
        string formattedText = text;
        if (emotionCode >= 0 && emotionCode <= 10)
        {
            // 字符串插值：$""语法
            formattedText += $" [{emotionCode}]";
        }

        // 应用此角色的语音设置
        ApplyVoiceProfile();

        // 触发语音
        ttsManager.ConvertTextToSpeech(formattedText);

        // 设置说话标志
        isSpeaking = true;

        // 启动协程检测语音何时结束
        StartCoroutine(WaitForSpeechToEnd());
    }

    /// <summary>
    /// 将此角色的语音配置文件应用到TTS系统
    /// </summary>
    private void ApplyVoiceProfile()
    {
        if (voiceProfile == null || ttsManager == null) return;

        // 将语音设置应用到TTSManager（Qwen特定）
        ttsManager.voice = voiceProfile.voiceId;  // 将voiceId映射到Qwen的语音名称
        ttsManager.speed = voiceProfile.playbackSpeed;  // 使用playbackSpeed作为Qwen的速度

        // 如果我们有面部动画控制器，应用动画设置
        if (facialAnimationController != null)
        {
            facialAnimationController.animationScale = voiceProfile.animationScale;
            // 您还可以在此设置其他动画参数
        }
    }

    /// <summary>
    /// 监控音频源以检测语音何时结束
    /// </summary>
    private IEnumerator WaitForSpeechToEnd()
    {
        // 等待音频开始播放
        yield return new WaitForSeconds(0.5f);

        AudioSource audioSource = ttsManager.audioSource;
        if (audioSource == null)
        {
            isSpeaking = false;
            yield break;
        }

        // 等待音频停止播放
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        // 语音已结束
        isSpeaking = false;

        // 通知监听器语音已结束
        OnSpeechEnded();
    }

    /// <summary>
    /// 检查此角色是否正在说话
    /// </summary>
    public bool IsSpeaking()
    {
        return isSpeaking;
    }

    /// <summary>
    /// 语音结束时调用的事件
    /// </summary>
    protected virtual void OnSpeechEnded()
    {
        // 可以在子类中重写以在语音结束时触发操作
    }
}
