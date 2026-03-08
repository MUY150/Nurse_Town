using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManger : MonoBehaviour
{
    private PlayerInput playerInput;
    
    public PlayerInput.OnFootActions onFoot;
    
    private PlayerMotor motor;
    private PlayerLook look;
    
    private VoiceInputController voiceController;
    
    private ChatInputController chatInputController;
    
    void Awake()
    {
        GameInputStateMachine.EnsureExists();
        
        playerInput = new PlayerInput();
        
        onFoot = playerInput.OnFoot;
        
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        voiceController = GetComponent<VoiceInputController>();
        
        if (voiceController == null)
        {
            Debug.LogError("[InputManger] VoiceInputController component not found on Player! Please add it in Inspector.");
        }
        
        onFoot.Jump.performed += OnJumpPerformed;
        
        // 订阅录音事件 - 使用started/canceled实现按住录音
        onFoot.Record.started += OnRecordStarted;
        onFoot.Record.canceled += OnRecordCanceled;
        
        // 订阅聊天相关事件
        onFoot.SendMessage.performed += OnSendMessagePerformed;
        onFoot.ToggleChat.performed += OnToggleChatPerformed;
    }
    
    void Start()
    {
        // 在Start中查找ChatInputController，因为CurrentChatUIInitializer在Start中创建它
        chatInputController = FindObjectOfType<ChatInputController>();
        Debug.Log($"[InputManger] Start - chatInputController: {chatInputController != null}");
        
        if (chatInputController == null)
        {
            Debug.LogWarning("[InputManger] ChatInputController not found in scene. Enter/F1 keys will not work.");
        }
    }
    
    void OnDestroy()
    {
        onFoot.Jump.performed -= OnJumpPerformed;
        onFoot.Record.started -= OnRecordStarted;
        onFoot.Record.canceled -= OnRecordCanceled;
        onFoot.SendMessage.performed -= OnSendMessagePerformed;
        onFoot.ToggleChat.performed -= OnToggleChatPerformed;
    }

    void FixedUpdate()
    {
        if (motor == null) return;
        
        // 检查状态机是否允许移动
        if (GameInputStateMachine.Instance?.CanMove() ?? true)
        {
            motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
        }
    }

    private void LateUpdate()
    {
        if (look == null) return;
        
        // 检查状态机是否允许视角控制
        if (GameInputStateMachine.Instance == null || 
            !GameInputStateMachine.Instance.IsActionBlocked("Look"))
        {
            look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
        }
    }
    
    void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        // 检查状态机是否允许跳跃
        if (GameInputStateMachine.Instance?.IsActionBlocked("Jump") ?? false)
            return;
            
        motor?.Jump();
    }
    
    void OnRecordStarted(InputAction.CallbackContext ctx)
    {
        // 检查状态机是否允许录音
        if (GameInputStateMachine.Instance?.CanRecord() ?? true)
        {
            voiceController?.StartRecording();
        }
    }
    
    void OnRecordCanceled(InputAction.CallbackContext ctx)
    {
        voiceController?.StopRecording();
    }
    
    void OnSendMessagePerformed(InputAction.CallbackContext ctx)
    {
        // 动态查找，确保即使初始化顺序不对也能工作
        if (chatInputController == null)
            chatInputController = FindObjectOfType<ChatInputController>();
            
        Debug.Log($"[InputManger] OnSendMessagePerformed called. chatInputController: {chatInputController != null}");
        if (chatInputController != null)
        {
            chatInputController.OnSendMessageFromInput();
        }
    }
    
    void OnToggleChatPerformed(InputAction.CallbackContext ctx)
    {
        // 动态查找，确保即使初始化顺序不对也能工作
        if (chatInputController == null)
            chatInputController = FindObjectOfType<ChatInputController>();
            
        Debug.Log($"[InputManger] OnToggleChatPerformed called. chatInputController: {chatInputController != null}");
        if (chatInputController != null)
        {
            chatInputController.TogglePanel();
        }
    }

    private void OnEnable()
    {
        onFoot.Enable();
    }
    
    private void OnDisable()
    {
        onFoot.Disable();
    }
}
