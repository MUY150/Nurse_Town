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
        var chatUI = FindObjectOfType<CurrentChatUI>();
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
        
        if (chatInputController == null)
        {
            chatInputController = gameObject.AddComponent<ChatInputController>();
            Debug.Log("[CurrentChatUIInitializer] Created ChatInputController component");
        }

        var chatUI = FindObjectOfType<CurrentChatUI>();
        Debug.Log($"[CurrentChatUIInitializer] FindObjectOfType<CurrentChatUI>: {chatUI != null}");

        // 设置 panel 引用
        if (chatInputController.panel == null && chatUI != null && chatUI.panel != null)
        {
            chatInputController.panel = chatUI.panel;
        }

        // 查找并设置 inputField
        if (chatInputController.inputField == null)
        {
            if (chatUI == null)
            {
                chatUI = FindObjectOfType<CurrentChatUI>();
            }
            
            Transform searchRoot = null;
            if (chatUI != null && chatUI.panel != null)
            {
                searchRoot = chatUI.panel.transform;
            }
            else if (chatUI != null)
            {
                searchRoot = chatUI.transform;
            }
            else
            {
                searchRoot = transform;
            }
            
            Debug.Log($"[CurrentChatUIInitializer] Searching for inputField in: {searchRoot.name}");
            
            var inputFieldContainer = searchRoot.Find("InputFieldContainer");
            if (inputFieldContainer != null)
            {
                var inputField = inputFieldContainer.GetComponentInChildren<TMP_InputField>();
                if (inputField != null)
                {
                    chatInputController.inputField = inputField;
                    Debug.Log($"[CurrentChatUIInitializer] Found inputField in InputFieldContainer under {searchRoot.name}");
                }
            }
            
            if (chatInputController.inputField == null && chatUI != null)
            {
                chatInputController.inputField = chatUI.GetComponentInChildren<TMP_InputField>();
                if (chatInputController.inputField != null)
                {
                    Debug.Log($"[CurrentChatUIInitializer] Found inputField via GetComponentInChildren on {chatUI.name}");
                }
            }
            
            if (chatInputController.inputField == null)
            {
                var inputFieldGO = searchRoot.Find("InputField");
                if (inputFieldGO != null)
                {
                    chatInputController.inputField = inputFieldGO.GetComponent<TMP_InputField>();
                    if (chatInputController.inputField != null)
                    {
                        Debug.Log($"[CurrentChatUIInitializer] Found inputField by name 'InputField' under {searchRoot.name}");
                    }
                }
            }
            
            if (chatInputController.inputField == null)
            {
                var chatInputFieldGO = searchRoot.Find("InputFieldContainer/ChatInputField");
                if (chatInputFieldGO != null)
                {
                    chatInputController.inputField = chatInputFieldGO.GetComponent<TMP_InputField>();
                    if (chatInputController.inputField != null)
                    {
                        Debug.Log($"[CurrentChatUIInitializer] Found inputField at InputFieldContainer/ChatInputField under {searchRoot.name}");
                    }
                }
            }
            
            if (chatInputController.inputField == null)
            {
                var allInputFields = FindObjectsOfType<TMP_InputField>();
                Debug.Log($"[CurrentChatUIInitializer] Found {allInputFields.Length} TMP_InputField in scene");
                foreach (var field in allInputFields)
                {
                    if (field.name == "ChatInputField" || field.name.Contains("Input"))
                    {
                        chatInputController.inputField = field;
                        Debug.Log($"[CurrentChatUIInitializer] Found inputField by global search: {field.name}");
                        break;
                    }
                }
            }
            
            if (chatInputController.inputField == null)
            {
                Debug.LogWarning($"[CurrentChatUIInitializer] Could not find TMP_InputField. Searched in: {searchRoot.name}");
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
