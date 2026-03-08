using UnityEngine;
using TMPro;

public class ChatInputInitializer : MonoBehaviour
{
    [Header("字体设置")]
    public TMP_FontAsset chineseFont;
    
    void Start()
    {
        var inputField = GetComponent<TMP_InputField>();
        if (inputField == null) return;

        var textArea = transform.Find("Text Area");
        var placeholder = transform.Find("Placeholder");

        if (textArea != null)
        {
            var textComponent = textArea.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                inputField.textViewport = textArea.GetComponent<RectTransform>();
                inputField.textComponent = textComponent;
            }
        }

        if (placeholder != null)
        {
            var placeholderText = placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
            {
                inputField.placeholder = placeholderText;
            }
        }

        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.restoreOriginalTextOnEscape = false;
        
        // 禁用颜色过渡，使用 Image 的背景颜色
        var colors = inputField.colors;
        colors.normalColor = new Color(1, 1, 1, 0);
        colors.highlightedColor = new Color(1, 1, 1, 0);
        colors.pressedColor = new Color(1, 1, 1, 0);
        colors.selectedColor = new Color(1, 1, 1, 0);
        inputField.colors = colors;
        
        // 设置中文字体
        if (chineseFont == null)
        {
            chineseFont = Resources.Load<TMP_FontAsset>("Fonts/msyh SDF");
        }
        
        if (chineseFont != null)
        {
            if (inputField.textComponent != null)
            {
                inputField.textComponent.font = chineseFont;
                inputField.textComponent.color = Color.white;
            }
            
            var placeholderText = inputField.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.font = chineseFont;
                placeholderText.text = "输入消息...";
            }
        }
        else
        {
            // 如果找不到中文字体，使用默认白色
            if (inputField.textComponent != null)
            {
                inputField.textComponent.color = Color.white;
            }
        }
    }
}
