using UnityEngine;
using TMPro;
using System.Collections;

public class MessageItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private RectTransform rectTransform;
    
    [SerializeField] private float minHeight = 50f;
    [SerializeField] private float verticalPadding = 16f;
    [SerializeField] private float roleHeight = 30f;

    private void Awake()
    {
        Debug.Log($"[MessageItem] Awake - roleText: {(roleText != null ? "OK" : "NULL")}, messageText: {(messageText != null ? "OK" : "NULL")}");
        
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
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
            messageText.color = Color.white;
            Debug.Log($"[MessageItem] Set messageText to: {messageText.text?.Substring(0, Mathf.Min(20, messageText.text?.Length ?? 0))}...");
        }
        else
        {
            Debug.LogError("[MessageItem] messageText is NULL!");
        }
        
        AdjustHeight();
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
        
        AdjustHeight();
    }
    
    private void AdjustHeight()
    {
        if (messageText == null || rectTransform == null) return;
        
        StartCoroutine(AdjustHeightCoroutine());
    }
    
    private System.Collections.IEnumerator AdjustHeightCoroutine()
    {
        yield return null;
        
        messageText.ForceMeshUpdate(true);
        
        float textHeight = messageText.preferredHeight;
        float actualRoleHeight = (roleText != null && roleText.gameObject.activeSelf) ? roleHeight : 0f;
        
        float requiredHeight = actualRoleHeight + textHeight + verticalPadding;
        requiredHeight = Mathf.Max(requiredHeight, minHeight);
        
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
        
        Debug.Log($"[MessageItem] Adjusted height: {requiredHeight}, textHeight: {textHeight}, roleHeight: {actualRoleHeight}");
    }
}
