using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 玩家UI控制器，管理玩家界面元素
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - [SerializeField]序列化特性：让私有字段在Inspector中可编辑
/// - TextMeshProUGUI：TextMeshPro的UI文本组件
/// - Unity生命周期方法：Start()
/// </remarks>
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;

    /// <summary>
    /// 更新UI文本
    /// </summary>
    /// <param name="promptMessage">要显示的提示消息</param>
    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }
    
    void Start()
    {
        
    }
}
