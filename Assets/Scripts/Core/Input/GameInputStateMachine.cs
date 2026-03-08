using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏输入状态枚举
/// </summary>
public enum GameInputState
{
    Gameplay,           // 正常游戏状态（面板全关）
    ChatPanel_Open,     // 聊天面板打开，输入框未聚焦
    ChatPanel_Focused,  // 聊天面板打开，输入框聚焦
    ChatPanel_Recording,// 聊天面板打开，正在录音
    Menu_Open,          // 菜单/暂停界面打开
    Dialogue_Active,    // 与NPC对话中
}

/// <summary>
/// 游戏输入状态机 - 统一管理所有输入状态
/// 单例模式，跨场景持久化
/// </summary>
public class GameInputStateMachine : MonoBehaviour
{
    public static GameInputStateMachine Instance { get; private set; }
    
    /// <summary>
    /// 确保实例存在（自动创建）
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("[GameInputStateMachine]");
            Instance = go.AddComponent<GameInputStateMachine>();
            Debug.Log("[GameInputStateMachine] Auto-created instance");
        }
    }
    
    [SerializeField] private GameInputState _currentState = GameInputState.Gameplay;
    
    /// <summary>
    /// 当前输入状态
    /// </summary>
    public GameInputState CurrentState 
    { 
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                var oldState = _currentState;
                _currentState = value;
                OnStateChanged?.Invoke(oldState, value);
                Debug.Log($"[GameInputStateMachine] State changed: {oldState} -> {value}");
            }
        }
    }
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action<GameInputState, GameInputState> OnStateChanged;
    
    // 状态转换规则定义
    private readonly Dictionary<GameInputState, HashSet<GameInputState>> _validTransitions = new()
    {
        { GameInputState.Gameplay, new() { 
            GameInputState.ChatPanel_Open, 
            GameInputState.Menu_Open, 
            GameInputState.Dialogue_Active 
        }},
        { GameInputState.ChatPanel_Open, new() { 
            GameInputState.Gameplay, 
            GameInputState.ChatPanel_Focused, 
            GameInputState.ChatPanel_Recording,
            GameInputState.Menu_Open 
        }},
        { GameInputState.ChatPanel_Focused, new() { 
            GameInputState.Gameplay, 
            GameInputState.ChatPanel_Open, 
            GameInputState.ChatPanel_Recording,
            GameInputState.Menu_Open 
        }},
        { GameInputState.ChatPanel_Recording, new() { 
            GameInputState.ChatPanel_Open, 
            GameInputState.ChatPanel_Focused 
        }},
        { GameInputState.Menu_Open, new() { 
            GameInputState.Gameplay, 
            GameInputState.ChatPanel_Open 
        }},
        { GameInputState.Dialogue_Active, new() { 
            GameInputState.Gameplay 
        }}
    };
    
    // 各状态下禁用的游戏输入动作
    private readonly Dictionary<GameInputState, HashSet<string>> _blockedActions = new()
    {
        { GameInputState.Gameplay, new() },
        { GameInputState.ChatPanel_Open, new() { "Movement", "Jump", "Look" } },
        { GameInputState.ChatPanel_Focused, new() { "Movement", "Jump", "Look" } },
        { GameInputState.ChatPanel_Recording, new() { "Movement", "Jump", "Look", "Interact" } },
        { GameInputState.Menu_Open, new() { "Movement", "Jump", "Look", "Interact", "Record" } },
        { GameInputState.Dialogue_Active, new() { "Movement", "Jump" } }
    };
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 检查是否可以转换到目标状态
    /// </summary>
    public bool CanTransitionTo(GameInputState newState)
    {
        if (_validTransitions.TryGetValue(CurrentState, out var validStates))
        {
            return validStates.Contains(newState);
        }
        return false;
    }
    
    /// <summary>
    /// 转换到目标状态
    /// </summary>
    /// <returns>是否成功转换</returns>
    public bool TransitionTo(GameInputState newState)
    {
        if (!CanTransitionTo(newState))
        {
            Debug.LogWarning($"[GameInputStateMachine] Invalid transition: {CurrentState} -> {newState}");
            return false;
        }
        
        CurrentState = newState;
        return true;
    }
    
    /// <summary>
    /// 检查指定动作在当前状态下是否被禁用
    /// </summary>
    public bool IsActionBlocked(string actionName)
    {
        if (_blockedActions.TryGetValue(CurrentState, out var blocked))
        {
            return blocked.Contains(actionName);
        }
        return false;
    }
    
    /// <summary>
    /// 检查当前是否可以录音
    /// </summary>
    public bool CanRecord()
    {
        return CurrentState is GameInputState.Gameplay 
               or GameInputState.ChatPanel_Open;
    }
    
    /// <summary>
    /// 检查当前是否可以移动
    /// </summary>
    public bool CanMove()
    {
        return CurrentState == GameInputState.Gameplay 
               || CurrentState == GameInputState.Dialogue_Active;
    }
    
    /// <summary>
    /// 检查当前是否处于UI交互状态（面板打开）
    /// </summary>
    public bool IsUIActive()
    {
        return CurrentState is GameInputState.ChatPanel_Open 
               or GameInputState.ChatPanel_Focused 
               or GameInputState.ChatPanel_Recording
               or GameInputState.Menu_Open;
    }
}
