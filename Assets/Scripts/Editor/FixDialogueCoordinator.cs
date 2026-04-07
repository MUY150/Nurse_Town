#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using NurseTown.Core.Dialogue;

public class FixDialogueCoordinator : MonoBehaviour
{
    [MenuItem("Tools/Fix Dialogue Coordinator Reference")]
    public static void FixReference()
    {
        var coordinator = FindObjectOfType<DialogueCoordinator>();
        if (coordinator == null)
        {
            Debug.LogError("[FixDialogueCoordinator] DialogueCoordinator not found in scene!");
            return;
        }

        var sittingObj = GameObject.Find("Sitting");
        if (sittingObj == null)
        {
            Debug.LogError("[FixDialogueCoordinator] Sitting GameObject not found!");
            return;
        }

        var patientController = sittingObj.GetComponent<PatientDialogueController>();
        if (patientController == null)
        {
            Debug.LogError("[FixDialogueCoordinator] PatientDialogueController not found on Sitting!");
            return;
        }

        coordinator.SetConversationTarget(patientController);
        
        EditorUtility.SetDirty(coordinator);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log($"[FixDialogueCoordinator] Success! currentTarget set to: {patientController.gameObject.name}");
    }
}
#endif
