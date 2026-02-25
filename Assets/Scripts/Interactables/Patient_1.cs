// ============================================================================
// 文件名: Patient_1.cs
// 功能描述: 病人交互脚本，处理与病人的交互
// 作者: AI Assistant
// 创建日期: 2026-01-11
// 修改记录: 添加详细中文注释，标注C#特性和Unity API
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 病人交互类
/// 实现Interactable接口，处理玩家与病人的交互
/// </summary>
/// <remarks>
/// C#特性说明:
/// - Interactable: 自定义接口，定义交互行为
/// - [SerializeField]: Unity序列化特性，让私有字段在Inspector中可编辑
/// - GameObject: Unity游戏对象的基础类
/// </remarks>
public class patient_1 : Interactable
{
    // ============================================================================
    // Unity特性说明: [SerializeField]
    // ============================================================================
    // [SerializeField]是Unity的序列化特性
    // 让私有字段可以在Inspector面板中编辑
    // 类似C++中需要手动实现编辑器UI，C#通过特性自动处理
    // ============================================================================
    
    [SerializeField] private GameObject patient;
    private bool _patient_bool;
    
    // ============================================================================
    // Unity生命周期方法: Start() 和 Update()
    // ============================================================================
    // Start在Awake之后、第一帧更新之前调用
    // Update每帧调用一次，用于处理持续的游戏逻辑
    // 适合用于初始化组件和设置初始状态
    // ============================================================================
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
    
    // ============================================================================
    // Unity特性说明: override关键字
    // ============================================================================
    // override用于重写基类的方法
    // Interactable是基类的方法，这里重写实现病人的交互逻辑
    // ============================================================================
    
    /// <summary>
    /// 玩家与病人交互的方法
    /// 切换病人的交互状态
    /// </summary>
    /// <remarks>
    /// Unity特性: Debug.Log
    /// 用于在控制台输出调试信息
    /// </remarks>
    protected override void Interact()
    {
        _patient_bool = !_patient_bool;
        Debug.Log("Interacting with Patient");
    }
}
