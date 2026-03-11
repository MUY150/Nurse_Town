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

        // 初始化输入框字体
        InitializeInputFieldFont(chatInputController.inputField);
    }

    void InitializeInputFieldFont(TMP_InputField inputField)
    {
        if (inputField == null) return;

        TMP_FontAsset chineseFont = null;
        
#if UNITY_EDITOR
        chineseFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/msyhl SDF.asset");
        if (chineseFont == null)
        {
            chineseFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/msyh SDF.asset");
        }
        
        if (chineseFont == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("msyh t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                chineseFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }
#endif
        
        if (chineseFont == null)
        {
            var chatUI = FindObjectOfType<CurrentChatUI>();
            if (chatUI != null && chatUI.messageItemPrefab != null)
            {
                var textComponents = chatUI.messageItemPrefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (textComponents.Length > 0 && textComponents[0].font != null)
                {
                    chineseFont = textComponents[0].font;
                    Debug.Log($"[CurrentChatUIInitializer] Got font from MessageItemPrefab: {chineseFont.name}");
                }
            }
        }
        
        if (chineseFont == null)
        {
            Debug.LogWarning("[CurrentChatUIInitializer] Could not load Chinese font for input field");
            return;
        }

        var textArea = inputField.transform.Find("Text Area");
        var placeholder = inputField.transform.Find("Placeholder");
        TextMeshProUGUI textComponent = null;

        if (textArea != null)
        {
            textComponent = textArea.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                inputField.textViewport = textArea.GetComponent<RectTransform>();
                inputField.textComponent = textComponent;
                textComponent.font = chineseFont;
                textComponent.color = new Color(1f, 1f, 1f, 1f);
                textComponent.fontSize = 18;
                textComponent.enableWordWrapping = false;
                textComponent.alignment = TextAlignmentOptions.Left;
                Debug.Log($"[CurrentChatUIInitializer] Set font on inputField.textComponent, color: white, fontSize: 18");
            }
        }

        if (placeholder != null)
        {
            var placeholderText = placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
            {
                inputField.placeholder = placeholderText;
                placeholderText.font = chineseFont;
                placeholderText.text = "输入消息...";
                placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                placeholderText.fontSize = 18;
                Debug.Log($"[CurrentChatUIInitializer] Set font on inputField.placeholder with color gray");
            }
        }

        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.restoreOriginalTextOnEscape = false;
        
        // 设置输入框背景 - 使用 InputFieldContainer 的背景
        var inputFieldContainer = inputField.transform.parent;
        if (inputFieldContainer != null)
        {
            var containerImage = inputFieldContainer.GetComponent<Image>();
            if (containerImage != null)
            {
                containerImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
                containerImage.raycastTarget = true;
                Debug.Log($"[CurrentChatUIInitializer] Set InputFieldContainer background color");
            }
        }
        
        // 刷新文本组件以应用更改
        if (textComponent != null)
        {
            textComponent.SetVerticesDirty();
            textComponent.SetLayoutDirty();
        }
        
        var placeholderRef = inputField.placeholder as TextMeshProUGUI;
        if (placeholderRef != null)
        {
            placeholderRef.SetVerticesDirty();
            placeholderRef.SetLayoutDirty();
        }
        
        // 强制更新输入框布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(inputField.GetComponent<RectTransform>());
        
        Debug.Log($"[CurrentChatUIInitializer] Input field initialized with: {chineseFont.name}, textViewport: {inputField.textViewport != null}, textComponent: {inputField.textComponent != null}, placeholder: {inputField.placeholder != null}");
    }
}
