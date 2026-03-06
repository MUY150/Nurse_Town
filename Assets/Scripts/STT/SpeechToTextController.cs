using UnityEngine;
using TMPro;
using System.IO;
using System;

/// <summary>
/// 语音转文本控制器，负责录制音频并使用统一的 STT 客户端进行语音识别
/// </summary>
public class SpeechToTextController : MonoBehaviour
{
    public TextMeshProUGUI transcriptText;
    public STTProvider sttProvider = STTProvider.Paraformer;

    private bool isRecording = false;
    private AudioClip recordedClip;
    private float recordingStartTime;
    private const float MIN_RECORDING_DURATION = 0.5f;
    private ISTTClient _sttClient;

    void Start()
    {
        InitializeSTTClient();
    }

    private void InitializeSTTClient()
    {
        _sttClient = ClientFactory.CreateSTTClient(sttProvider, this);
        if (_sttClient == null)
        {
            Debug.LogError($"[SpeechToTextController] Failed to create STT client for provider: {sttProvider}");
            return;
        }

        _sttClient.OnTranscriptionComplete += OnTranscriptionComplete;
        Debug.Log($"[SpeechToTextController] STT client initialized: {sttProvider}");
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
        if (Input.GetKeyDown(KeyCode.R) && !isRecording)
        {
            StartRecording();
        }
        if (Input.GetKeyUp(KeyCode.R) && isRecording)
        {
            StopRecordingAndTranscribe();
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    private void StartRecording()
    {
        if (_sttClient == null)
        {
            Debug.LogError("[SpeechToTextController] STT client not initialized!");
            return;
        }

        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[SpeechToTextController] No microphone device found!");
            return;
        }

        Debug.Log($"[SpeechToTextController] Using microphone: {devices[0]}");
        
        _sttClient.StartRecording(10, 16000);
        
        isRecording = true;
        recordingStartTime = Time.time;
        Debug.Log("[SpeechToTextController] Recording started...");
    }

    /// <summary>
    /// 停止录音并转录
    /// </summary>
    private void StopRecordingAndTranscribe()
    {
        float recordingDuration = Time.time - recordingStartTime;

        if (recordingDuration < MIN_RECORDING_DURATION)
        {
            Debug.LogWarning($"[SpeechToTextController] Recording too short ({recordingDuration:F2}s), minimum required: {MIN_RECORDING_DURATION}s");
            _sttClient.StopRecording();
            isRecording = false;
            return;
        }

        isRecording = false;

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
        Debug.Log($"[SpeechToTextController] Transcription received: {transcription}");

        if (transcriptText != null)
        {
            transcriptText.text = transcription;
        }

        if (sitPatientSpeech.Instance != null)
        {
            sitPatientSpeech.Instance.ReceiveNurseTranscription(transcription);
        }
        else
        {
            Debug.LogError("[SpeechToTextController] sitPatientSpeech instance not found.");
        }
    }
}
