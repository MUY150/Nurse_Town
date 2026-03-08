using UnityEngine;
using TMPro;

public class CurrentChatUIInitializer : MonoBehaviour
{
    void Start()
    {
        var chatUI = GetComponent<CurrentChatUI>();
        if (chatUI == null)
        {
            chatUI = GetComponentInChildren<CurrentChatUI>();
        }
        if (chatUI == null) return;

        var chatUIPanel = chatUI.transform;
        var inputFieldContainer = chatUIPanel.Find("InputFieldContainer");
        if (inputFieldContainer != null)
        {
            chatUI.inputFieldContainer = inputFieldContainer.gameObject;

            var inputField = inputFieldContainer.GetComponentInChildren<TMP_InputField>();
            if (inputField != null)
            {
                chatUI.inputField = inputField;

                inputField.onSubmit.AddListener(OnInputSubmit);
            }
        }
    }

    private void OnInputSubmit(string text)
    {
        var chatUI = GetComponent<CurrentChatUI>();
        if (chatUI == null)
        {
            chatUI = GetComponentInChildren<CurrentChatUI>();
        }
        if (chatUI != null)
        {
            chatUI.OnSendButtonClicked();
        }
    }
}
