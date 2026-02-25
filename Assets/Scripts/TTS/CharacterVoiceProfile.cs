using System;
using UnityEngine;

/// <summary>
/// 使用ElevenLabs TTS定义角色的语音配置文件
/// </summary>
/// <remarks>
/// C#特性说明：
/// - ScriptableObject：Unity可脚本化对象，用于存储数据资产
/// - [CreateAssetMenu]特性：在Unity编辑器中创建菜单项
/// - [Header]特性：在Inspector中显示分组标题
/// - [Tooltip]特性：在Inspector中显示提示文本
/// - [Range]特性：限制数值范围，显示为滑块
/// - [TextArea]特性：在Inspector中显示多行文本框
/// - 属性（Property）：自动属性
/// - 字段：公共字段，可在Inspector中编辑
/// </remarks>
[CreateAssetMenu(fileName = "New Character Voice Profile", menuName = "Audio/Character Voice Profile")]
public class CharacterVoiceProfile : ScriptableObject
{
    [Header("Character Information")]
    [Tooltip("Name of the character (for reference only)")]
    public string characterName = "Character";

    [Tooltip("Description of this voice (for reference only)")]
    [TextArea(2, 5)]
    public string voiceDescription = "";

    [Header("ElevenLabs Voice Settings")]
    [Tooltip("Voice ID from ElevenLabs")]
    public string voiceId = "Bz0vsNJm8uY1hbd4c4AE"; // 默认语音ID

    [Tooltip("Model ID from ElevenLabs")]
    public string modelId = "eleven_multilingual_v2"; // 默认模型

    [Header("Voice Characteristics")]
    [Range(0f, 1f)]
    [Tooltip("Stability value (0-1). Lower values make voice more spontaneous, higher values more stable.")]
    public float stability = 0.4f;

    [Range(0f, 1f)]
    [Tooltip("Similarity boost value (0-1). Higher values make voice sound more like the original voice.")]
    public float similarityBoost = 0.75f;

    [Range(0f, 1f)]
    [Tooltip("Style exaggeration value (0-1). Higher values amplify the speaking style.")]
    public float styleExaggeration = 0.3f;

    [Header("Advanced Settings")]
    [Range(0.5f, 2.0f)]
    [Tooltip("Playback speed multiplier. 1.0 is normal speed.")]
    public float playbackSpeed = 1.0f;

    [Range(0.5f, 2.0f)]
    [Tooltip("Animation scaling for facial movements. Higher values make expressions more pronounced.")]
    public float animationScale = 1f;
}
