using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// 聊天UI显示控制器 - 只负责UI显示，不处理输入逻辑
/// 输入逻辑已移至 ChatInputController
/// </summary>
public class CurrentChatUI : MonoBehaviour
{
    [Header("配置")]
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

    [Header("位置设置")]
    public bool positionAtBottomLeft = true;
    public Vector2 bottomLeftOffset = new Vector2(20, 20);
    public float chatUIWidth = 500;
    public float chatUIHeight = 350;

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
        if (positionAtBottomLeft)
        {
            PositionAtBottomLeft();
        }
    }

    /// <summary>
    /// 将 ChatUI 定位到屏幕左下角
    /// </summary>
    private void PositionAtBottomLeft()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 设置锚点为左下角
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);

        // 设置位置偏移
        rectTransform.anchoredPosition = bottomLeftOffset;

        // 设置尺寸
        rectTransform.sizeDelta = new Vector2(chatUIWidth, chatUIHeight);

        Debug.Log($"[CurrentChatUI] Positioned at bottom left. Offset: {bottomLeftOffset}, Size: {chatUIWidth}x{chatUIHeight}");
    }

    /// <summary>
    /// 设置当前LLM客户端
    /// </summary>
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

    /// <summary>
    /// 切换面板显示状态
    /// 注意：输入处理已移至 ChatInputController
    /// </summary>
    public void TogglePanel()
    {
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
        RefreshChat();
        ScrollToBottom();
        SetGamePaused(true);
    }

    /// <summary>
    /// 隐藏聊天面板
    /// </summary>
    public void HidePanel()
    {
        if (panel == null) return;
        
        panel.SetActive(false);
        SetGamePaused(false);
    }

    /// <summary>
    /// 设置游戏暂停状态（只影响视角控制）
    /// </summary>
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

    /// <summary>
    /// 禁用/启用玩家视角控制
    /// </summary>
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

    /// <summary>
    /// 刷新聊天显示
    /// </summary>
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

    /// <summary>
    /// 发送用户消息（由 ChatInputController 调用）
    /// </summary>
    public void SendUserMessage(string message)
    {
        Debug.Log($"[CurrentChatUI] SendUserMessage: {message}");
        
        #pragma warning disable CS0618
        if (PatientDialogueController.Instance != null)
        {
            PatientDialogueController.Instance.ReceiveNurseTranscription(message);
        }
        else if (sitPatientSpeech.Instance != null)
        {
            sitPatientSpeech.Instance.ReceiveNurseTranscription(message);
        }
        else
        {
            Debug.LogWarning("[CurrentChatUI] No patient dialogue controller found, cannot send message");
        }
        #pragma warning restore CS0618
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
}
