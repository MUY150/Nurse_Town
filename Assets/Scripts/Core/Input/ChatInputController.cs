using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 聊天输入控制器 - 集成状态机和新Input System
/// 处理聊天面板的输入逻辑，包括键盘输入和语音输入
/// </summary>
public class ChatInputController : MonoBehaviour
{
    [Header("配置")]
    public int maxHistorySize = 50;
    
    [Header("引用")]
    public GameObject panel;
    public TMP_InputField inputField;
    public CurrentChatUI chatUI;
    public VoiceInputController voiceController;
    
    [Header("输入动作 - 通过PlayerInput配置")]
    public InputActionReference toggleChatAction;
    public InputActionReference sendMessageAction;
    public InputActionReference closeChatAction;
    
    private List<string> _inputHistory = new();
    private int _historyIndex = -1;
    private string _currentDraft = "";
    
    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
            
        // 注册输入动作事件
        if (toggleChatAction != null)
            toggleChatAction.action.performed += OnToggleChatPerformed;
        if (sendMessageAction != null)
            sendMessageAction.action.performed += OnSendMessagePerformed;
        if (closeChatAction != null)
            closeChatAction.action.performed += OnCloseChatPerformed;
        
        // 监听语音事件
        if (voiceController != null)
        {
            voiceController.OnRecordingFinished += OnVoiceRecordingFinished;
            voiceController.OnRecordingError += OnVoiceRecordingError;
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅输入动作
        if (toggleChatAction != null)
            toggleChatAction.action.performed -= OnToggleChatPerformed;
        if (sendMessageAction != null)
            sendMessageAction.action.performed -= OnSendMessagePerformed;
        if (closeChatAction != null)
            closeChatAction.action.performed -= OnCloseChatPerformed;
        
        // 取消订阅语音事件
        if (voiceController != null)
        {
            voiceController.OnRecordingFinished -= OnVoiceRecordingFinished;
            voiceController.OnRecordingError -= OnVoiceRecordingError;
        }
    }
    
    void OnEnable()
    {
        toggleChatAction?.action.Enable();
        sendMessageAction?.action.Enable();
        closeChatAction?.action.Enable();
    }
    
    void OnDisable()
    {
        toggleChatAction?.action.Disable();
        sendMessageAction?.action.Disable();
        closeChatAction?.action.Disable();
    }
    
    void Update()
    {
        // 历史记录导航（使用旧Input System的KeyCode，因为TMP_InputField需要）
        HandleHistoryNavigation();
        
        // 根据UI状态更新状态机
        UpdateStateBasedOnUI();
    }
    
    /// <summary>
    /// 切换聊天面板显示/隐藏
    /// </summary>
    void OnToggleChatPerformed(InputAction.CallbackContext ctx)
    {
        TogglePanel();
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    void OnSendMessagePerformed(InputAction.CallbackContext ctx)
    {
        if (panel != null && panel.activeSelf && inputField != null && inputField.isFocused)
        {
            SendMessage();
        }
    }
    
    /// <summary>
    /// 关闭聊天面板
    /// </summary>
    void OnCloseChatPerformed(InputAction.CallbackContext ctx)
    {
        if (panel != null && panel.activeSelf)
        {
            // 如果正在录音，先停止
            if (GameInputStateMachine.Instance?.CurrentState == GameInputState.ChatPanel_Recording)
            {
                voiceController?.StopRecording();
            }
            HidePanel();
        }
    }
    
    /// <summary>
    /// 切换面板显示状态
    /// </summary>
    public void TogglePanel()
    {
        Debug.Log($"[ChatInputController] TogglePanel called. panel: {panel != null}, active: {panel?.activeSelf}");
        if (panel == null) return;
        
        if (panel.activeSelf)
            HidePanel();
        else
            ShowPanel();
    }
    
    /// <summary>
    /// 显示聊天面板
    /// </summary>
    public void ShowPanel()
    {
        if (panel == null) return;
        
        panel.SetActive(true);
        
        // 先转换到 ChatPanel_Open 状态
        GameInputStateMachine.Instance?.TransitionTo(GameInputState.ChatPanel_Open);
        
        // 然后激活输入框并转换到 Focused 状态
        inputField?.ActivateInputField();
        GameInputStateMachine.Instance?.TransitionTo(GameInputState.ChatPanel_Focused);
        
        chatUI?.RefreshChat();
    }
    
    /// <summary>
    /// 隐藏聊天面板
    /// </summary>
    public void HidePanel()
    {
        if (panel == null) return;
        
        panel.SetActive(false);
        
        GameInputStateMachine.Instance?.TransitionTo(GameInputState.Gameplay);
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    void SendMessage()
    {
        if (inputField == null) return;
        
        string message = inputField.text?.Trim();
        if (string.IsNullOrEmpty(message)) return;
        
        // 添加到历史
        AddToHistory(message);
        
        // 发送消息
        chatUI?.SendUserMessage(message);
        
        // 清空输入
        inputField.text = "";
        _currentDraft = "";
        _historyIndex = -1;
        
        // 保持聚焦
        inputField.ActivateInputField();
    }
    
    /// <summary>
    /// 从InputManger接收发送消息事件
    /// </summary>
    public void OnSendMessageFromInput()
    {
        Debug.Log($"[ChatInputController] OnSendMessageFromInput called. panel: {panel != null}, active: {panel?.activeSelf}, inputField: {inputField != null}, focused: {inputField?.isFocused}");
        if (panel != null && panel.activeSelf && inputField != null && inputField.isFocused)
        {
            SendMessage();
        }
    }
    
    /// <summary>
    /// 语音录音完成回调
    /// </summary>
    void OnVoiceRecordingFinished(string transcription)
    {
        // 将语音转录文本填入输入框
        if (inputField != null)
        {
            inputField.text = transcription;
            inputField.ActivateInputField();
        }
    }
    
    /// <summary>
    /// 语音录音错误回调
    /// </summary>
    void OnVoiceRecordingError(string error)
    {
        Debug.LogError($"[ChatInputController] Voice recording error: {error}");
        // 可在这里显示错误提示UI
    }
    
    /// <summary>
    /// 处理历史记录导航（上下箭头）
    /// </summary>
    void HandleHistoryNavigation()
    {
        if (panel == null || !panel.activeSelf || inputField == null || !inputField.isFocused) 
            return;
        
        // 检查是否正在录音，录音时不处理历史导航
        if (GameInputStateMachine.Instance?.CurrentState == GameInputState.ChatPanel_Recording)
            return;
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            NavigateHistory(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NavigateHistory(1);
        }
    }
    
    /// <summary>
    /// 导航历史记录
    /// </summary>
    void NavigateHistory(int direction)
    {
        if (_inputHistory.Count == 0 || inputField == null) return;
        
        if (direction < 0) // 上一条
        {
            if (_historyIndex == -1)
            {
                _currentDraft = inputField.text;
                _historyIndex = _inputHistory.Count - 1;
            }
            else if (_historyIndex > 0)
            {
                _historyIndex--;
            }
        }
        else // 下一条
        {
            if (_historyIndex == -1) return;
            
            _historyIndex++;
            if (_historyIndex >= _inputHistory.Count)
            {
                _historyIndex = -1;
                inputField.text = _currentDraft;
                inputField.caretPosition = inputField.text.Length;
                return;
            }
        }
        
        inputField.text = _inputHistory[_historyIndex];
        inputField.caretPosition = inputField.text.Length;
    }
    
    /// <summary>
    /// 根据UI状态更新状态机
    /// </summary>
    void UpdateStateBasedOnUI()
    {
        var stateMachine = GameInputStateMachine.Instance;
        if (stateMachine == null) return;
        
        // 如果面板关闭，回到Gameplay状态
        if (panel == null || !panel.activeSelf)
        {
            if (stateMachine.CurrentState != GameInputState.Gameplay)
                stateMachine.TransitionTo(GameInputState.Gameplay);
            return;
        }
        
        // 如果正在录音，保持Recording状态
        if (stateMachine.CurrentState == GameInputState.ChatPanel_Recording)
            return;
        
        // 根据输入框焦点状态更新
        if (inputField != null && inputField.isFocused)
        {
            if (stateMachine.CurrentState != GameInputState.ChatPanel_Focused)
                stateMachine.TransitionTo(GameInputState.ChatPanel_Focused);
        }
        else
        {
            if (stateMachine.CurrentState != GameInputState.ChatPanel_Open)
                stateMachine.TransitionTo(GameInputState.ChatPanel_Open);
        }
    }
    
    /// <summary>
    /// 添加消息到历史记录
    /// </summary>
    void AddToHistory(string message)
    {
        _inputHistory.Add(message);
        if (_inputHistory.Count > maxHistorySize)
        {
            _inputHistory.RemoveAt(0);
        }
    }
}
