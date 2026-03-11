/// <summary>
/// 角色类型枚举，用于区分不同角色的 TTS 配置
/// </summary>
public enum CharacterType
{
    /// <summary>站立角色（默认）- 支持11种情绪代码(0-10)</summary>
    Standing,
    
    /// <summary>坐姿角色 - 支持6种情绪代码(0-5)</summary>
    Sitting,
    
    /// <summary>自定义角色（通过配置指定）</summary>
    Custom
}
