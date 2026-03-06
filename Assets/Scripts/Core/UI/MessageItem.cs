using UnityEngine;
using TMPro;

public class MessageItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Awake()
    {
        Debug.Log($"[MessageItem] Awake - roleText: {(roleText != null ? "OK" : "NULL")}, messageText: {(messageText != null ? "OK" : "NULL")}");
    }

    public void SetContent(string role, string content, Color roleColor)
    {
        Debug.Log($"[MessageItem] SetContent called - Role: {role}, Content: {content?.Substring(0, Mathf.Min(20, content?.Length ?? 0))}..., roleText: {(roleText != null ? "OK" : "NULL")}, messageText: {(messageText != null ? "OK" : "NULL")}");
        
        if (roleText != null)
        {
            roleText.text = $"{role}:";
            roleText.color = roleColor;
            Debug.Log($"[MessageItem] Set roleText to: {roleText.text}");
        }
        else
        {
            Debug.LogError("[MessageItem] roleText is NULL!");
        }

        if (messageText != null)
        {
            messageText.text = content;
            Debug.Log($"[MessageItem] Set messageText to: {messageText.text?.Substring(0, Mathf.Min(20, messageText.text?.Length ?? 0))}...");
        }
        else
        {
            Debug.LogError("[MessageItem] messageText is NULL!");
        }
    }

    public void SetContent(string content, Color color)
    {
        if (roleText != null)
        {
            roleText.gameObject.SetActive(false);
        }

        if (messageText != null)
        {
            messageText.text = content;
            messageText.color = color;
        }
    }
}
