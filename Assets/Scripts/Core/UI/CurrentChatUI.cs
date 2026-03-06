using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class CurrentChatUI : MonoBehaviour
{
    [Header("配置")]
    public KeyCode toggleKey = KeyCode.F1;
    public Color userColor = new Color(0.2f, 0.5f, 1f);
    public Color assistantColor = new Color(0.3f, 0.8f, 0.3f);
    public Color systemColor = new Color(0.7f, 0.7f, 0.7f);
    public bool showSystemMessages = false;
    public bool showLatestFirst = true;
    public int maxMessagesDisplayed = 100;

    [Header("引用")]
    public GameObject panel;
    public Transform messageContainer;
    public GameObject messageItemPrefab;
    public ScrollRect scrollRect;

    private ILlmClient _currentLlmClient;
    private List<GameObject> _messageItems = new List<GameObject>();

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
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
        }
    }

    public void ShowPanel()
    {
        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);
            RefreshChat();
            ScrollToBottom();
        }
    }

    public void HidePanel()
    {
        if (panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
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
        
        if (showLatestFirst)
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
        return role.ToLower() switch
        {
            "user" => "你",
            "assistant" => "助手",
            "system" => "系统",
            _ => role
        };
    }

    private void OnDestroy()
    {
        if (_currentLlmClient != null)
        {
            _currentLlmClient.OnConversationUpdated -= RefreshChat;
        }
    }
}
