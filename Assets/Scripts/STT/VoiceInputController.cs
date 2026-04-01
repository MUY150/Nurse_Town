using UnityEngine;
using System;

/// <summary>
/// 语音输入控制器
/// 通过 InputManger 的 New Input System 事件触发录音
/// </summary>
public class VoiceInputController : MonoBehaviour
{
    [Header("STT配置")]
    public STTProvider sttProvider = STTProvider.Paraformer;
    
    [Header("录音设置")]
    [SerializeField] private float maxRecordingDuration = 30f;
    [SerializeField] private float minRecordingDuration = 0.5f;
    
    // 状态
    private bool isRecording = false;
    private float recordingStartTime;
    private ISTTClient _sttClient;
    
    // 事件
    public event Action OnRecordingStarted;
    public event Action<string> OnRecordingFinished;
    public event Action<string> OnRecordingError;
    
    // === 已移除的 UI 引用（供后续恢复参考） ===
    // [Header("UI引用")]
    // public TextMeshProUGUI transcriptText;      // 显示转录文本的文本框
    // public GameObject recordingIndicator;       // 录音时显示的指示器
    // public AudioSource recordingBeep;           // 录音提示音
    //
    // 恢复时需要：
    // 1. 在 StartRecording() 中：recordingIndicator?.SetActive(true); recordingBeep?.Play();
    // 2. 在 StopRecording() 中：recordingIndicator?.SetActive(false);
    // 3. 在 OnTranscriptionComplete() 中：transcriptText.text = transcription;
    
    void Start()
    {
        InitializeSTTClient();
        Debug.Log("[VoiceInputController] Initialized - Ready for voice input");
    }
    
    private void InitializeSTTClient()
    {
        _sttClient = ClientFactory.CreateSTTClient(sttProvider, this);
        if (_sttClient == null)
        {
            Debug.LogError($"[VoiceInputController] Failed to create STT client for provider: {sttProvider}");
            OnRecordingError?.Invoke("STT客户端初始化失败");
            return;
        }
        
        _sttClient.OnTranscriptionComplete += OnTranscriptionComplete;
        Debug.Log($"[VoiceInputController] STT client initialized: {sttProvider}");
    }
    
    private void OnDestroy()
    {
        if (_sttClient != null)
        {
            _sttClient.OnTranscriptionComplete -= OnTranscriptionComplete;
        }
    }
    
    void Update()
    {
        // 检查录音超时
        if (isRecording && Time.time - recordingStartTime > maxRecordingDuration)
        {
            Debug.Log("[VoiceInputController] Recording timeout, auto-stopping...");
            StopRecording();
        }
    }
    
    /// <summary>
    /// 开始录音 - 由 InputManger 通过 New Input System 事件调用
    /// </summary>
    public void StartRecording()
    {
        if (isRecording) return;
        
        if (_sttClient == null)
        {
            Debug.LogError("[VoiceInputController] STT client not initialized!");
            OnRecordingError?.Invoke("STT客户端未初始化");
            return;
        }
        
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[VoiceInputController] No microphone device found!");
            OnRecordingError?.Invoke("未找到麦克风设备");
            return;
        }
        
        // 更新状态机
        if (GameInputStateMachine.Instance != null)
        {
            GameInputStateMachine.Instance.TransitionTo(GameInputState.ChatPanel_Recording);
        }
        
        // 开始录音
        _sttClient.StartRecording((int)maxRecordingDuration, 16000);
        isRecording = true;
        recordingStartTime = Time.time;
        
        OnRecordingStarted?.Invoke();
        Debug.Log("[VoiceInputController] Recording started...");
    }
    
    /// <summary>
    /// 停止录音并转录 - 由 InputManger 通过 New Input System 事件调用
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording) return;
        
        float duration = Time.time - recordingStartTime;
        isRecording = false;
        
        // 检查录音时长
        if (duration < minRecordingDuration)
        {
            Debug.LogWarning($"[VoiceInputController] Recording too short ({duration:F2}s)");
            _sttClient?.StopRecording();
            ReturnToPreviousState();
            return;
        }
        
        // 停止录音并转录
        if (_sttClient != null)
        {
            _sttClient.StopRecordingAndTranscribe();
        }
    }
    
    /// <summary>
    /// 转录完成回调
    /// </summary>
    private void OnTranscriptionComplete(string transcription)
    {
        Debug.Log($"[VoiceInputController] Transcription received: {transcription}");
        
        // 触发事件
        OnRecordingFinished?.Invoke(transcription);
        
        // 发送给对话系统
        SendToDialogueSystem(transcription);
        
        // 返回之前的状态
        ReturnToPreviousState();
    }
    
    /// <summary>
    /// 发送转录文本到对话系统
    /// </summary>
    void SendToDialogueSystem(string transcription)
    {
        if (NurseTown.Core.Dialogue.DialogueCoordinator.Instance != null)
        {
            NurseTown.Core.Dialogue.DialogueCoordinator.Instance.ReceiveTranscription(transcription);
        }
        else
        {
            Debug.LogWarning("[VoiceInputController] No dialogue system found to receive transcription");
        }
    }
    
    /// <summary>
    /// 返回到录音前的状态
    /// </summary>
    void ReturnToPreviousState()
    {
        var stateMachine = GameInputStateMachine.Instance;
        if (stateMachine != null)
        {
            if (stateMachine.CurrentState == GameInputState.ChatPanel_Recording)
            {
                stateMachine.TransitionTo(GameInputState.ChatPanel_Open);
            }
        }
    }
    
    /// <summary>
    /// 检查是否正在录音
    /// </summary>
    public bool IsRecording => isRecording;
}
