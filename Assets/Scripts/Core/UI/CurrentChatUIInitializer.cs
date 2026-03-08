using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// CurrentChatUI 初始化器
/// 负责自动查找和配置聊天UI相关引用
/// </summary>
public class CurrentChatUIInitializer : MonoBehaviour
{
    void Start()
    {
        InitializeChatUI();
        InitializeChatInputController();
    }

    /// <summary>
    /// 初始化 CurrentChatUI 的引用
    /// </summary>
    void InitializeChatUI()
    {
        var chatUI = GetComponent<CurrentChatUI>();
        if (chatUI == null)
        {
            chatUI = GetComponentInChildren<CurrentChatUI>();
        }
        if (chatUI == null) return;

        // 如果 panel 未设置，尝试使用当前 GameObject
        if (chatUI.panel == null)
        {
            chatUI.panel = gameObject;
        }

        // 查找 MessageContainer
        if (chatUI.messageContainer == null)
        {
            var messageContainerTransform = transform.Find("MessageContainer");
            if (messageContainerTransform != null)
            {
                chatUI.messageContainer = messageContainerTransform;
            }
        }

        // 查找 ScrollRect
        if (chatUI.scrollRect == null)
        {
            chatUI.scrollRect = GetComponentInChildren<ScrollRect>();
        }
    }

    /// <summary>
    /// 初始化 ChatInputController 的引用
    /// </summary>
    void InitializeChatInputController()
    {
        var chatInputController = GetComponent<ChatInputController>();
        if (chatInputController == null)
        {
            chatInputController = GetComponentInChildren<ChatInputController>();
        }
        
        // 如果场景中没有 ChatInputController，自动创建一个
        if (chatInputController == null)
        {
            chatInputController = gameObject.AddComponent<ChatInputController>();
            Debug.Log("[CurrentChatUIInitializer] Created ChatInputController component");
        }

        var chatUI = GetComponent<CurrentChatUI>();
        if (chatUI == null)
        {
            chatUI = GetComponentInChildren<CurrentChatUI>();
        }

        // 设置 panel 引用
        if (chatInputController.panel == null && chatUI != null && chatUI.panel != null)
        {
            chatInputController.panel = chatUI.panel;
        }

        // 查找并设置 inputField
        if (chatInputController.inputField == null)
        {
            var inputFieldContainer = transform.Find("InputFieldContainer");
            if (inputFieldContainer != null)
            {
                var inputField = inputFieldContainer.GetComponentInChildren<TMP_InputField>();
                if (inputField != null)
                {
                    chatInputController.inputField = inputField;
                }
            }
        }

        // 设置 chatUI 引用
        if (chatInputController.chatUI == null)
        {
            chatInputController.chatUI = chatUI;
        }

        // 查找并设置 voiceController
        if (chatInputController.voiceController == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                chatInputController.voiceController = player.GetComponent<VoiceInputController>();
            }
        }
    }
}
