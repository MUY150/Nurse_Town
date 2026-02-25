using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 环境变量加载器，从.env文件加载环境变量
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - Unity生命周期方法：Awake()
/// - 泛型：Dictionary<string, string>键值对集合
/// - 静态字典：EnvVars存储环境变量
/// - 文件I/O：Path.Combine、File.ReadAllLines、File.Exists
/// - 字符串操作：Split()、Trim()、StartsWith()、IsNullOrWhiteSpace()
/// - 字符串数组：string[]
/// - foreach循环：遍历字符串数组
/// - 三元运算符：?: 条件运算符
/// - 静态方法：GetEnvVariable()静态方法
/// - Debug输出：Debug.Log()、Debug.LogWarning()
/// - Path.Combine：安全的路径拼接方法
/// - Application.dataPath：Unity项目数据路径
/// - Unity API：Debug输出
/// </remarks>
public class EnvironmentLoader : MonoBehaviour
{
    // 静态字典：存储从.env文件加载的环境变量
    private static readonly Dictionary<string, string> EnvVars = new Dictionary<string, string>();

    /// <summary>
    /// Unity生命周期方法：对象创建时调用
    /// </summary>
    void Awake()
    {
        // 加载.env文件
        LoadEnvFile();
        Debug.Log("env loader awake");
    }

    /// <summary>
    /// 加载.env文件并解析环境变量
    /// </summary>
    private void LoadEnvFile()
    {
        // 构建.env文件路径（项目根目录）
        string filePath = Path.Combine(Application.dataPath, "../.env");
        Debug.Log("env loader file path: " + filePath);

        // 检查.env文件是否存在
        if (File.Exists(filePath))
        {
            Debug.Log("env loader file exists");
            
            // 读取所有行
            string[] lines = File.ReadAllLines(filePath);
            
            // 遍历每一行
            foreach (string line in lines)
            {
                // 跳过空行和注释行
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // 使用等号分割键值对
                string[] parts = line.Split('=');
                
                // 确保分割后有2个部分（键和值）
                if (parts.Length == 2)
                {
                    // 去除前后空格并存储到字典中
                    EnvVars[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
        else
        {
            Debug.LogWarning(".env file not found");
        }
    }

    /// <summary>
    /// 获取环境变量值
    /// </summary>
    /// <param name="key">环境变量名称</param>
    /// <returns>环境变量值，如果不存在则返回null</returns>
    public static string GetEnvVariable(string key)
    {
        // 三元运算符：使用TryGetValue方法安全获取值
        return EnvVars.TryGetValue(key, out string value) ? value : null;
    }
}
