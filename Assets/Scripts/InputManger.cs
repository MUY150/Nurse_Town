using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 输入管理器，处理玩家输入并传递给相应的组件
/// 注意：原文件名拼写错误（InputManger应为InputManager），保持原有代码不变
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - Unity新输入系统：PlayerInput、InputAction
/// - Unity生命周期方法：Awake()、FixedUpdate()、LateUpdate()、OnEnable()、OnDisable()
/// - 私有字段：private成员变量
/// - 公共字段：public成员变量
/// - GetComponent方法：获取组件
/// - 委托事件：+= 操作符订阅事件
/// - Lambda表达式：ctx => motor.Jump()
/// - 泛型：GetComponent<T>()
/// - Vector2：Unity二维向量结构
/// - InputAction.ReadValue：读取输入动作值
/// - InputAction.Enable/Disable：启用/禁用输入动作
/// </remarks>
public class InputManger : MonoBehaviour
{
    // 私有字段：PlayerInput组件引用
    private PlayerInput playerInput;
    
    // 公共字段：InputAction集合，包含玩家脚步相关动作
    public PlayerInput.OnFootActions onFoot;
    
    // 组件引用：玩家移动控制器
    private PlayerMotor motor;

    // 组件引用：玩家视角控制器
    private PlayerLook look;
    
    // Unity生命周期方法：在第一帧更新之前调用
    // Start is called before the first frame update
    void Awake()
    {
        // 创建PlayerInput实例
        playerInput = new PlayerInput();
        
        // 获取OnFoot动作集合
        onFoot = playerInput.OnFoot;
        
        // 获取相关组件引用
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        
        // 订阅跳跃事件：使用Lambda表达式简化事件处理
        onFoot.Jump.performed += ctx => motor.Jump();
        
        // 订阅视角事件（当前被注释）
        // onFoot.Look.performed += ctx => look.ProcessLook(ctx.ReadValue<Vector2>());
    }

    /// <summary>
    /// Unity生命周期方法：固定时间间隔调用（用于物理计算）
    /// </summary>
    void FixedUpdate()
    {
        if (motor == null) return;
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    /// <summary>
    /// Unity生命周期方法：在Update之后调用
    /// </summary>
    private void LateUpdate()
    {
        if (look == null) return;
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    /// <summary>
    /// Unity生命周期方法：脚本启用时调用
    /// </summary>
    private void OnEnable()
    {
        // 启用输入动作
        onFoot.Enable();
    }
    
    /// <summary>
    /// Unity生命周期方法：脚本禁用时调用
    /// </summary>
    private void OnDisable()
    {
        // 禁用输入动作
        onFoot.Disable();
    }
}
