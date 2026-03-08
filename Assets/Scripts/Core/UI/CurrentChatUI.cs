using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class CurrentChatUI : MonoBehaviour
{
    [Header("配置")]
    public KeyCode toggleKey = KeyCode.Return;
    public Color userColor = new Color(0.2f, 0.5f, 1f);
    public Color assistantColor = new Color(0.3f, 0.8f, 0.3f);
    public Color systemColor = new Color(0.7f, 0.7f, 0.7f);
    public bool showSystemMessages = false;
    public bool showLatestFirst = false;
    public int maxMessagesDisplayed = 100;
    public bool useChineseRoleNames = true;

    [Header("引用")]
    public GameObject panel;
    public Transform messageContainer;
    public GameObject messageItemPrefab;
    public ScrollRect scrollRect;

    [Header("输入框")]
    public TMP_InputField inputField;
    public GameObject inputFieldContainer;

    private ILlmClient _currentLlmClient;
    private List<GameObject> _messageItems = new List<GameObject>();

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Start()
    {
        if (inputField == null)
        {
            var container = transform.Find("InputFieldContainer");
            if (container != null)
            {
                inputFieldContainer = container.gameObject;
                inputField = container.GetComponentInChildren<TMP_InputField>();
                if (inputField != null)
                {
                    inputField.onSubmit.AddListener(OnInputSubmit);
                    Debug.Log("[CurrentChatUI] InputField found and bound successfully");
                }
                else
                {
                    Debug.LogWarning("[CurrentChatUI] TMP_InputField not found in InputFieldContainer");
                }
            }
            else
            {
                Debug.LogWarning("[CurrentChatUI] InputFieldContainer not found");
            }
        }
    }

    private void OnInputSubmit(string text)
    {
        OnSendButtonClicked();
    }

    private void Update()
    {
        // Enter 键：打开面板（如果关闭）或发送消息（如果打开且输入框有内容）
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (panel != null && panel.activeSelf)
            {
                // 面板已打开，尝试发送消息
                if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
                {
                    OnSendButtonClicked();
                }
                // 如果输入框为空，不执行任何操作（保持面板打开）
            }
            else
            {
                // 面板关闭，打开面板
                ShowPanel();
            }
        }
        
        // Esc 键：关闭面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panel != null && panel.activeSelf)
            {
                HidePanel();
            }
        }
    }

    public void SetCurrentLlmClient(ILlmClient client)
    {
        Debug.Log($"[CurrentChatUI] SetCurrentLlmClient called with client: {client?.GetType().Name ?? "null"}");
        
        if (_currentLlmClient != null)
        {
            _currentLlmClient.OnConversationUpdated -= RefreshChat;
        }

        _currentLlmClient = client;

        if (_currentLlmClient != null)
        {
            _currentLlmClient.OnConversationUpdated += RefreshChat;
            Debug.Log("[CurrentChatUI] Subscribed to OnConversationUpdated event");
            RefreshChat();
        }
    }

    public void TogglePanel()
    {
        if (panel == null) return;
        
        panel.SetActive(!panel.activeSelf);
        
        if (panel.activeSelf)
        {
            RefreshChat();
            ScrollToBottom();
            SetGamePaused(true);
        }
        else
        {
            SetGamePaused(false);
        }
    }

    private void SetGamePaused(bool paused)
    {
        if (paused)
        {
            // 不暂停游戏，只禁用视角控制
            DisablePlayerLook(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 恢复视角控制
            DisablePlayerLook(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void DisablePlayerLook(bool disable)
    {
        // 查找玩家对象上的 InputManger 组件并禁用/启用
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var inputManager = player.GetComponent<InputManger>();
            if (inputManager != null)
            {
                inputManager.enabled = !disable;
            }
            
            var playerLook = player.GetComponent<PlayerLook>();
            if (playerLook != null)
            {
                playerLook.enabled = !disable;
            }
        }
    }

    public void ShowPanel()
    {
        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);
            RefreshChat();
            ScrollToBottom();
            SetGamePaused(true);
            if (inputField != null)
            {
                inputField.ActivateInputField();
            }
        }
    }

    public void HidePanel()
    {
        if (panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
            SetGamePaused(false);
        }
    }

    public void RefreshChat()
    {
        Debug.Log($"[CurrentChatUI] RefreshChat called. Client: {_currentLlmClient?.GetType().Name ?? "null"}, Panel active: {panel?.activeSelf}");
        
        if (_currentLlmClient == null) 
        {
            Debug.LogWarning("[CurrentChatUI] _currentLlmClient is null, cannot refresh chat");
            return;
        }
        if (panel == null)
        {
            Debug.LogWarning("[CurrentChatUI] panel is null");
            return;
        }
        if (!panel.activeSelf) 
        {
            Debug.Log("[CurrentChatUI] Panel is not active, skipping refresh");
            return;
        }

        var messages = _currentLlmClient.GetChatHistory();
        Debug.Log($"[CurrentChatUI] Got {messages?.Count ?? 0} messages from history");
        if (messages == null) return;

        ClearMessages();

        var filteredMessages = messages.Where(m => 
            showSystemMessages || m["role"].ToLower() != "system"
        ).TakeLast(maxMessagesDisplayed).ToList();
        
        // 当最早的消息在上面时，需要反转顺序
        // 因为 Instantiate 会按顺序添加，后添加的在下方
        if (!showLatestFirst)
        {
            filteredMessages.Reverse();
        }

        foreach (var msg in filteredMessages)
        {
            AddMessage(msg["role"], msg["content"]);
        }

        if (showLatestFirst)
        {
            ScrollToTop();
        }
        else
        {
            ScrollToBottom();
        }
    }

    private void ScrollToTop()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void AddMessage(string role, string content)
    {
        Debug.Log($"[CurrentChatUI] AddMessage called - Role: {role}, Content length: {content?.Length ?? 0}");
        
        if (messageItemPrefab == null)
        {
            Debug.LogError("[CurrentChatUI] messageItemPrefab is null!");
            return;
        }
        if (messageContainer == null)
        {
            Debug.LogError("[CurrentChatUI] messageContainer is null!");
            return;
        }

        var itemObj = Instantiate(messageItemPrefab, messageContainer);
        Debug.Log($"[CurrentChatUI] Instantiated message item: {itemObj.name}");
        
        itemObj.SetActive(true);
        Debug.Log($"[CurrentChatUI] Activated message item");
        
        var messageItem = itemObj.GetComponent<MessageItem>();
        Debug.Log($"[CurrentChatUI] MessageItem component: {(messageItem != null ? "found" : "NOT FOUND")}");

        if (messageItem != null)
        {
            Color color = GetRoleColor(role);
            string roleDisplay = GetRoleDisplayName(role);
            
            messageItem.SetContent(roleDisplay, content, color);
        }

        _messageItems.Add(itemObj);
    }

    private void ClearMessages()
    {
        foreach (var item in _messageItems)
        {
            Destroy(item);
        }
        _messageItems.Clear();
    }

    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private Color GetRoleColor(string role)
    {
        return role.ToLower() switch
        {
            "user" => userColor,
            "assistant" => assistantColor,
            "system" => systemColor,
            _ => Color.gray
        };
    }

    private string GetRoleDisplayName(string role)
    {
        if (useChineseRoleNames)
        {
            return role.ToLower() switch
            {
                "user" => "你(You)",
                "assistant" => "助手(Assistant)",
                "system" => "系统(System)",
                _ => role
            };
        }
        else
        {
            return role.ToLower() switch
            {
                "user" => "You",
                "assistant" => "Assistant",
                "system" => "System",
                _ => role
            };
        }
    }

    private void OnDestroy()
    {
        if (_currentLlmClient != null)
        {
            _currentLlmClient.OnConversationUpdated -= RefreshChat;
        }
    }

    public void OnSendButtonClicked()
    {
        if (inputField == null) return;
        
        string text = inputField.text.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            SendUserMessage(text);
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    private void SendUserMessage(string message)
    {
        Debug.Log($"[CurrentChatUI] SendUserMessage: {message}");
        if (sitPatientSpeech.Instance != null)
        {
            sitPatientSpeech.Instance.ReceiveNurseTranscription(message);
        }
        else
        {
            Debug.LogWarning("[CurrentChatUI] sitPatientSpeech.Instance is null, cannot send message");
        }
    }
}
