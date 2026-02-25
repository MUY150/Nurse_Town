using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理角色语音配置文件集合，方便在不同语音设置之间切换
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 单例模式（Singleton）：使用静态Instance字段确保全局唯一
/// - 泛型：Dictionary<K,V>键值对集合、数组、GetComponent<T>()、FindObjectOfType<T>()
/// - Unity生命周期方法：Awake()、Start()
/// - 属性（Property）：自动属性
/// - 字符串插值：$""语法构建字符串
/// - 数组：CharacterVoiceProfile[]数组
/// - foreach循环：遍历集合
/// - 条件运算符：三元运算符 ?:
/// - TryGetValue方法：字典的安全访问方法
/// </remarks>
public class VoiceProfileManager : MonoBehaviour
{
    // 单例模式：静态实例，确保全局唯一
    public static VoiceProfileManager Instance { get; private set; }
    
    [Header("Voice Profiles")]
    [Tooltip("Array of character voice profiles to choose from")]
    public CharacterVoiceProfile[] availableProfiles;
    
    [Tooltip("Default profile to use if none is specified")]
    public CharacterVoiceProfile defaultProfile;
    
    [Header("Component References")]
    [Tooltip("Reference to TTSManager")]
    public TTSManager ttsManager;
    
    [Tooltip("Reference to CSVFacialAnimationController")]
    public CSVFacialAnimationController facialAnimationController;
    
    // 字典：用于按名称快速查找配置文件
    private Dictionary<string, CharacterVoiceProfile> profilesByName;
    
    // 当前活动的配置文件
    private CharacterVoiceProfile currentProfile;
    
    void Awake()
    {
        // 单例模式：确保只有一个VoiceProfileManager实例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 如果已存在实例，销毁当前对象
            Destroy(gameObject);
            return;
        }
        
        // 初始化配置文件字典
        profilesByName = new Dictionary<string, CharacterVoiceProfile>();
        // foreach循环：遍历所有可用的配置文件
        foreach (var profile in availableProfiles)
        {
            if (profile != null)
            {
                // 字典操作：将配置文件添加到字典中
                profilesByName[profile.characterName] = profile;
            }
        }
        
        // 设置默认配置文件
        if (defaultProfile != null)
        {
            currentProfile = defaultProfile;
        }
        else if (availableProfiles != null && availableProfiles.Length > 0)
        {
            // 如果没有默认配置文件，使用第一个可用配置文件
            currentProfile = availableProfiles[0];
        }
        
        // 应用初始配置文件设置
        if (currentProfile != null)
        {
            ApplyProfileSettings(currentProfile);
        }
    }
    
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
            }
        }
        
        // 如果未分配，查找面部动画控制器
        if (facialAnimationController == null)
        {
            facialAnimationController = FindObjectOfType<CSVFacialAnimationController>();
        }
    }
    
    /// <summary>
    /// 按名称设置活动语音配置文件
    /// </summary>
    /// <param name="profileName">配置文件名称</param>
    /// <returns>是否成功设置</returns>
    public bool SetActiveProfile(string profileName)
    {
        // TryGetValue方法：字典的安全访问方法，避免KeyNotFoundException
        if (profilesByName.TryGetValue(profileName, out CharacterVoiceProfile profile))
        {
            currentProfile = profile;
            ApplyProfileSettings(profile);
            // 字符串插值：$""语法
            Debug.Log($"Switched to voice profile: {profileName}");
            return true;
        }
        
        Debug.LogWarning($"Voice profile '{profileName}' not found!");
        return false;
    }
    
    /// <summary>
    /// 按索引设置活动语音配置文件
    /// </summary>
    /// <param name="profileIndex">配置文件索引</param>
    /// <returns>是否成功设置</returns>
    public bool SetActiveProfile(int profileIndex)
    {
        // 数组边界检查
        if (availableProfiles != null && profileIndex >= 0 && profileIndex < availableProfiles.Length)
        {
            currentProfile = availableProfiles[profileIndex];
            ApplyProfileSettings(currentProfile);
            Debug.Log($"Switched to voice profile: {currentProfile.characterName}");
            return true;
        }
        
        Debug.LogWarning($"Voice profile index {profileIndex} is out of range!");
        return false;
    }
    
    /// <summary>
    /// 直接设置活动语音配置文件
    /// </summary>
    /// <param name="profile">语音配置文件</param>
    public void SetActiveProfile(CharacterVoiceProfile profile)
    {
        if (profile != null)
        {
            currentProfile = profile;
            ApplyProfileSettings(profile);
            Debug.Log($"Switched to voice profile: {profile.characterName}");
        }
        else
        {
            Debug.LogWarning("Attempted to set null voice profile!");
        }
    }
    
    /// <summary>
    /// 获取当前活动的配置文件
    /// </summary>
    public CharacterVoiceProfile GetCurrentProfile()
    {
        return currentProfile;
    }
    
    /// <summary>
    /// 将语音配置文件的设置应用到相关组件
    /// </summary>
    /// <param name="profile">语音配置文件</param>
    private void ApplyProfileSettings(CharacterVoiceProfile profile)
    {
        if (profile == null) return;
        
        // 将设置应用到TTSManager
        if (ttsManager != null)
        {
            // 对于Qwen TTS，我们只设置语音名称和速度
            // 移除了ElevenLabs特定的参数
            ttsManager.voice = profile.voiceId; // 将voiceId映射到voice用于Qwen
        }
        
        // 将设置应用到面部动画控制器
        if (facialAnimationController != null)
        {
            facialAnimationController.animationScale = profile.animationScale;
        }
    }
    
    /// <summary>
    /// 获取所有可用配置文件名称的列表
    /// </summary>
    /// <returns>配置文件名称数组</returns>
    public string[] GetProfileNames()
    {
        // 数组边界检查
        if (availableProfiles == null || availableProfiles.Length == 0)
        {
            return new string[0];
        }
        
        // 创建字符串数组
        string[] names = new string[availableProfiles.Length];
        // for循环：遍历数组
        for (int i = 0; i < availableProfiles.Length; i++)
        {
            // 条件运算符：三元运算符 ?:
            names[i] = availableProfiles[i] != null ? availableProfiles[i].characterName : "Unknown";
        }
        
        return names;
    }
}
