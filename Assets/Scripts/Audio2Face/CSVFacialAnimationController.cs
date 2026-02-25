// ============================================================================
// 文件名: CSVFacialAnimationController.cs
// 功能描述: CSV面部动画控制器，解析CSV数据并应用到角色面部
// 作者: AI Assistant
// 创建日期: 2026-01-11
// 修改记录: 添加详细中文注释，标注C#特性和Unity API
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================================
// Unity特性说明: [Header] 和 [Tooltip]
// ============================================================================
// [Header]: 在Inspector面板中创建分组标题
// [Tooltip]: 鼠标悬停在字段上时显示的提示文本
// [Range]: 限制数值范围，在Inspector中显示为滑块
// ============================================================================

/// <summary>
/// CSV面部动画控制器
/// 负责解析CSV格式的面部动画数据并应用到角色的混合形状
/// </summary>
/// <remarks>
/// C#特性说明:
/// - Dictionary: 键值对集合，类似C++的std::map
/// - KeyValuePair: 键值对结构
/// - LINQ: 用于数据查询和排序
/// - 协程(Coroutine): Unity的异步执行机制
/// </remarks>
public class CSVFacialAnimationController : MonoBehaviour
{
    [Header("Animation Setup")]
    [Tooltip("Your model with blendshapes")]
    public SkinnedMeshRenderer characterFace;
    
    [Tooltip("CSV file with animation data")]
    public TextAsset animationCSV;
    
    [Tooltip("Audio file to sync with")]
    public AudioClip audioClip;
    
    [Range(0.5f, 2.0f)]
    [Tooltip("Multiply animation values by this amount")]
    public float animationScale = 1.5f;
    
    [Header("Sync Settings")]
    [Tooltip("Name of column containing time information")]
    public string timeColumnName = "timeCode";
    
    [Range(1f, 100f)]
    [Tooltip("Playback speed multiplier (use 30 for typical 30fps animation)")]
    public float playbackSpeed = 30f;
    
    [Tooltip("Offset to apply to all time values (in seconds)")]
    public float timeOffset = 0.0f;
    
    [Header("Debug Settings")]
    [Tooltip("Show debug logs for first few frames")]
    public bool showDebugLogs = true;
    
    // ============================================================================
    // C#特性说明: 属性(Property)和Lambda表达式
    // ============================================================================
    // IsPlaying是只读属性，使用Lambda表达式: => isPlaying
    // 类似C++的getter函数，但语法更简洁
    // Lambda表达式: x => x > 5，类似C++的lambda或函数指针
    // ============================================================================
    
    // Add a public property to check if animation is playing
    public bool IsPlaying => isPlaying;
    
    // Private variables
    // Dictionary是C#的泛型字典，类似C++的std::map
    private Dictionary<string, int> blendShapeMapping;
    
    // List是C#的动态数组，类似C++的std::vector
    // KeyValuePair是键值对结构
    private List<KeyValuePair<float, Dictionary<string, float>>> timeOrderedFrames;
    private AudioSource audioSource;
    private bool isPlaying = false;
    private float animationDuration = 0f;
    private bool isInitialized = false;

    // ============================================================================
    // Unity生命周期方法: Start()
    // ============================================================================
    // Start在Awake之后、第一帧更新之前调用
    // 适合用于查找组件引用和启动协程
    // ============================================================================
    
    void Start()
    {
        // Create audio source if needed
        // AddComponent是Unity的方法，动态添加组件到游戏对象
        if (audioClip != null && GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.playOnAwake = false;
        }
        else
        {
            // GetComponent获取对象上已存在的组件
            audioSource = GetComponent<AudioSource>();
        }
        
        // Initialize the animation system if we have the required components
        if (characterFace != null && animationCSV != null)
        {
            InitializeAnimation();
            StartCoroutine(PlayAnimation());
        }
        else
        {
            Debug.LogWarning("CSVFacialAnimationController: Missing required components (Character Face or Animation CSV). Animation will not play.");
        }
    }
    
    // ============================================================================
    // 初始化动画系统
    // ============================================================================
    // 负责创建混合形状映射、加载CSV数据并排序
    // 使用LINQ的Sort方法对帧进行时间排序
    // ============================================================================
    
    private void InitializeAnimation()
    {
        if (characterFace == null)
        {
            Debug.LogError("No character face mesh assigned!");
            return;
        }
        
        if (animationCSV == null)
        {
            Debug.LogError("No animation CSV file assigned!");
            return;
        }
        
        // Create blendshape mapping
        CreateBlendShapeMapping();
        
        // Initialize frames list
        timeOrderedFrames = new List<KeyValuePair<float, Dictionary<string, float>>>();
        
        // Load animation data from CSV
        LoadAnimationFromCSV();
        
        // Sort frames by time
        if (timeOrderedFrames.Count > 0)
        {
            // 使用Lambda表达式进行排序: (a, b) => a.Key.CompareTo(b.Key)
            timeOrderedFrames.Sort((a, b) => a.Key.CompareTo(b.Key));
            
            // Calculate animation duration from the last time point
            animationDuration = timeOrderedFrames[timeOrderedFrames.Count - 1].Key;
            Debug.Log($"Animation duration based on time points: {animationDuration} seconds");
            Debug.Log($"With playback speed of {playbackSpeed}x, animation will play in {animationDuration / playbackSpeed} seconds");
            
            Debug.Log($"Loaded {timeOrderedFrames.Count} frames of animation data");
            Debug.Log($"Found {blendShapeMapping.Count} blendshape mappings");
            
            if (audioClip != null)
            {
                Debug.Log($"Audio duration: {audioClip.length} seconds");
                float expectedAnimationPlaytime = animationDuration / playbackSpeed;
                if (Math.Abs(audioClip.length - expectedAnimationPlaytime) > 1.0f)
                {
                    Debug.LogWarning($"Audio length ({audioClip.length}s) and expected animation playback time ({expectedAnimationPlaytime}s) differ significantly!");
                    Debug.LogWarning($"You may need to adjust the playbackSpeed parameter (currently {playbackSpeed}).");
                    
                    // Suggest a value
                    float suggestedSpeed = animationDuration / audioClip.length;
                    Debug.LogWarning($"Suggested playbackSpeed value: {suggestedSpeed}");
                }
            }
            
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("No animation frames loaded from CSV!");
        }
    }
    
    // ============================================================================
    // 创建混合形状映射
    // ============================================================================
    // 将网格中的混合形状名称映射到CSV中的列名
    // 支持多种命名约定（首字母大写、精确匹配、小写）
    // C#字符串操作: Substring、ToUpper、ToLower
    // ============================================================================
    
    private void CreateBlendShapeMapping()
    {
        blendShapeMapping = new Dictionary<string, int>();
        
        if (characterFace == null || characterFace.sharedMesh == null)
        {
            Debug.LogError("Cannot create blendshape mapping: Character face or shared mesh is missing!");
            return;
        }
        
        // Get all available blendshapes in the mesh
        int blendShapeCount = characterFace.sharedMesh.blendShapeCount;
        
        Debug.Log($"Character has {blendShapeCount} blend shapes");
        
        // Create mapping between CSV column names and mesh blendshape indices
        for (int i = 0; i < blendShapeCount; i++)
        {
            string blendShapeName = characterFace.sharedMesh.GetBlendShapeName(i);
            
            // Try different naming conventions for mapping
            // C#字符串操作: Substring、ToUpper、ToLower
            string csvNameWithPrefix = "blendShapes." + char.ToUpper(blendShapeName[0]) + blendShapeName.Substring(1);
            string csvNameExact = "blendShapes." + blendShapeName;
            string csvNameLower = "blendShapes." + blendShapeName.ToLower();
            
            blendShapeMapping[csvNameWithPrefix] = i;
            blendShapeMapping[csvNameExact] = i;
            blendShapeMapping[csvNameLower] = i;
            
            if (showDebugLogs && i < 5)
            {
                Debug.Log($"Mapped blendshape {i}: {blendShapeName} to CSV names including {csvNameWithPrefix}");
            }
        }
    }
    
    // ============================================================================
    // 从CSV文件加载动画数据
    // ============================================================================
    // 解析CSV文件，提取时间列和混合形状数据
    // 跳过非混合形状列和空值
    // ============================================================================
    
    private void LoadAnimationFromCSV()
    {
        if (animationCSV == null)
        {
            Debug.LogError("Cannot load animation: Animation CSV is missing!");
            return;
        }
        
        // Parse CSV data
        // Split是C#字符串方法，按分隔符分割字符串
        string[] lines = animationCSV.text.Split('\n');
        
        if (lines.Length < 2)
        {
            Debug.LogError("CSV file has insufficient data!");
            return;
        }
        
        // Get header line and parse column names
        string[] headers = lines[0].Split(',');
        
        // Find the index of the time column
        int timeColumnIndex = -1;
        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim();
            if (headerName == timeColumnName || i == 0) // Try to use first column as fallback
            {
                timeColumnIndex = i;
                if (headerName == timeColumnName)
                {
                    Debug.Log($"Found time column: {headerName} at index {i}");
                }
                else
                {
                    Debug.Log($"Using first column as time column: {headerName}");
                    timeColumnName = headerName;
                }
                break;
            }
        }
        
        if (timeColumnIndex == -1)
        {
            Debug.LogError($"Could not find time column named '{timeColumnName}' or use first column as fallback!");
            return;
        }
        
        // Print some header names for debugging
        if (showDebugLogs)
        {
            Debug.Log("CSV Header Analysis:");
            // Math.Min是C#的数学函数，返回较小值
            for (int i = 0; i < Math.Min(10, headers.Length); i++)
            {
                Debug.Log($"Column {i}: '{headers[i].Trim()}'");
            }
        }
        
        // Skip first row (headers) and load all frames
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length <= timeColumnIndex)
            {
                Debug.LogWarning($"Skipping line {lineIndex}: Not enough values to read time column");
                continue;
            }
            
            // Try to parse the time value
            float timeValue;
            if (!float.TryParse(values[timeColumnIndex], out timeValue))
            {
                Debug.LogWarning($"Skipping line {lineIndex}: Could not parse time value '{values[timeColumnIndex]}'");
                continue;
            }
            
            // Apply time offset
            timeValue = timeValue + timeOffset;
            
            Dictionary<string, float> frameData = new Dictionary<string, float>();
            
            // Parse each value and add to frame data if it's a blendshape column
            for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
            {
                // Skip the time column
                if (i == timeColumnIndex) continue;
                
                string header = headers[i].Trim();
                
                // Skip non-blendshape columns and empty values
                // StartsWith是C#字符串方法，检查字符串是否以指定前缀开头
                if (!header.StartsWith("blendShapes.") || string.IsNullOrEmpty(values[i]))
                {
                    continue;
                }
                
                // Try to parse the value
                float value;
                if (float.TryParse(values[i], out value) && value > 0)
                {
                    frameData[header] = value;
                }
            }
            
            // Store the frame data with its time value
            timeOrderedFrames.Add(new KeyValuePair<float, Dictionary<string, float>>(timeValue, frameData));
            
            // Log first few frames for debugging
            if (showDebugLogs && lineIndex <= 5)
            {
                Debug.Log($"Frame at time {timeValue}: {frameData.Count} blendshape values");
            }
        }
        
        // Debug log for first and last frames
        if (showDebugLogs && timeOrderedFrames.Count > 0)
        {
            var firstFrame = timeOrderedFrames[0];
            var lastFrame = timeOrderedFrames[timeOrderedFrames.Count - 1];
            
            Debug.Log($"First frame at time {firstFrame.Key}, last frame at time {lastFrame.Key}");
            Debug.Log($"Total animation duration: {lastFrame.Key - firstFrame.Key} seconds");
            
            // Print time differences between first few frames to verify frame rate
            if (timeOrderedFrames.Count >= 5)
            {
                Debug.Log("Frame time analysis (first 5 frames):");
                for (int i = 1; i < 5; i++)
                {
                    float timeDiff = timeOrderedFrames[i].Key - timeOrderedFrames[i-1].Key;
                    Debug.Log($"Time between frame {i-1} and {i}: {timeDiff} seconds (approx {1.0f/timeDiff} fps)");
                }
            }
        }
    }
    
    IEnumerator PlayAnimation()
    {
        if (!isInitialized || timeOrderedFrames == null || timeOrderedFrames.Count == 0)
        {
            Debug.LogWarning("Cannot play animation: Animation data not initialized or empty!");
            yield break;
        }
        
        // Reset all blendshapes first
        ResetBlendShapes();
        
        // Start audio playback
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        isPlaying = true;
        float startTime = Time.time;
        float animationPlaybackDuration = animationDuration / playbackSpeed;
        
        Debug.Log($"Starting animation playback. Expected duration: {animationPlaybackDuration} seconds");
        
        // Main animation loop
        while (isPlaying)
        {
            // Calculate elapsed time since animation started
            float elapsedTime = Time.time - startTime;
            
            // Stop if we've reached the end of the animation
            if (elapsedTime > animationPlaybackDuration)
            {
                break;
            }
            
            // Convert actual time to animation time (scaled by playback speed)
            float animationTime = elapsedTime * playbackSpeed;
            
            // Find the appropriate frame to display based on the current time
            ApplyFrameAtTime(animationTime);
            
            // Wait until next frame
            yield return null;
        }
        
        // Make sure we apply the last frame
        if (timeOrderedFrames.Count > 0)
        {
            ApplyFrameData(timeOrderedFrames[timeOrderedFrames.Count - 1].Value);
        }
        
        Debug.Log("Animation playback completed");
        isPlaying = false;
    }
    
    // ============================================================================
    // 根据时间应用帧数据
    // ============================================================================
    // 使用LINQ的FindIndex方法查找最接近的帧
    // ============================================================================
    
    private void ApplyFrameAtTime(float time)
    {
        if (timeOrderedFrames == null || timeOrderedFrames.Count == 0)
        {
            return;
        }
        
        // Find closest frame that's less than or equal to the current time
        // FindIndex是LINQ方法，使用Lambda表达式查找元素
        int index = timeOrderedFrames.FindIndex(frame => frame.Key > time) - 1;
        
        // If time is before first frame, use first frame
        if (index < 0) index = 0;
        
        // If time is after last frame, use last frame
        if (index >= timeOrderedFrames.Count) index = timeOrderedFrames.Count - 1;
        
        // Apply the frame
        if (index >= 0 && index < timeOrderedFrames.Count)
        {
            ApplyFrameData(timeOrderedFrames[index].Value);
        }
    }
    
    // ============================================================================
    // 应用帧数据到混合形状
    // ============================================================================
    // 遍历帧数据字典，应用每个混合形状的值
    // Unity的混合形状使用0-100范围
    // ============================================================================
    
    private void ApplyFrameData(Dictionary<string, float> frameData)
    {
        if (characterFace == null || frameData == null || blendShapeMapping == null)
        {
            return;
        }
        
        int shapesApplied = 0;
        
        // Apply each blendshape value from the frame data
        // foreach是C#的循环语法，类似C++的range-based for循环
        // var是类型推断关键字，编译器自动推断类型
        foreach (var entry in frameData)
        {
            string csvName = entry.Key;
            float value = entry.Value * animationScale; // Apply scaling
            
            // Find corresponding blendshape index
            // TryGetValue是Dictionary的方法，安全地获取值
            if (blendShapeMapping.TryGetValue(csvName, out int blendShapeIndex))
            {
                // Apply the value to the blendshape
                // SetBlendShapeWeight是Unity的方法，设置混合形状权重
                // Unity使用0-100范围，所以乘以100
                characterFace.SetBlendShapeWeight(blendShapeIndex, value * 100f); // Unity uses 0-100 range
                shapesApplied++;
            }
        }
    }
    
    // ============================================================================
    // 重置所有混合形状
    // ============================================================================
    // 将所有混合形状权重设置为0，重置面部表情
    // ============================================================================
    
    private void ResetBlendShapes()
    {
        if (characterFace == null || characterFace.sharedMesh == null)
        {
            return;
        }
        
        // Reset all blendshapes to zero
        for (int i = 0; i < characterFace.sharedMesh.blendShapeCount; i++)
        {
            characterFace.SetBlendShapeWeight(i, 0);
        }
    }
    
    // ============================================================================
    // 重启动动画（用于UI按钮）
    // ============================================================================
    // 停止所有协程并重新开始动画播放
    // 适合用于重新播放或重置动画
    // ============================================================================
    
    // Restart the animation (useful for UI button)
    public void RestartAnimation()
    {
        StopAllCoroutines();
        
        if (characterFace != null && animationCSV != null)
        {
            // Re-initialize if needed
            if (!isInitialized)
            {
                InitializeAnimation();
            }
            
            if (isInitialized)
            {
                StartCoroutine(PlayAnimation());
            }
            else
            {
                Debug.LogWarning("Cannot restart animation: Animation data not initialized!");
            }
        }
        else
        {
            Debug.LogWarning("Cannot restart animation: Missing required components!");
        }
    }
    
    // ============================================================================
    // Unity生命周期方法: OnDisable()
    // ============================================================================
    // OnDisable在对象被禁用时调用
    // 适合用于停止协程和清理资源
    // ============================================================================
    
    // Cleanup when script is disabled
    private void OnDisable()
    {
        StopAllCoroutines();
        isPlaying = false;
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    // ============================================================================
    // Unity调试方法: OnGUI()
    // ============================================================================
    // OnGUI用于在场景中绘制调试信息
    // 仅在编辑器中可用，用于调试目的
    // ============================================================================
    
    // For debug visualization
    void OnGUI()
    {
        // Add proper null checks to prevent NullReferenceExceptions
        if (showDebugLogs && isPlaying && isInitialized && timeOrderedFrames != null && timeOrderedFrames.Count > 0)
        {
            float elapsedTime = Time.time - Time.timeSinceLevelLoad + Time.deltaTime;
            GUI.Label(new Rect(10, 10, 300, 20), $"Animation Time: {elapsedTime:F2}s");
            
            float animTime = elapsedTime * playbackSpeed;
            GUI.Label(new Rect(10, 30, 300, 20), $"Actual Animation Position: {animTime:F2}s / {animationDuration:F2}s");
            
            int frameIndex = timeOrderedFrames.FindIndex(frame => frame.Key > animTime) - 1;
            if (frameIndex < 0) frameIndex = 0;
            if (frameIndex >= timeOrderedFrames.Count) frameIndex = timeOrderedFrames.Count - 1;
            
            GUI.Label(new Rect(10, 50, 300, 20), $"Current Frame: {frameIndex} / {timeOrderedFrames.Count}");
        }
    }
    
    // ============================================================================
    // 公共方法：检查动画是否正在播放
    // ============================================================================
    // 提供公共接口让其他脚本检查动画状态
    // ============================================================================
    
    // Add a public method to check if animation is still playing
    public bool IsAnimationPlaying()
    {
        return isPlaying;
    }
}