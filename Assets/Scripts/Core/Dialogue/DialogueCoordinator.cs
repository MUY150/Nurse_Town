using UnityEngine;
using NurseTown.Core.Interfaces;

namespace NurseTown.Core.Dialogue
{
    /// <summary>
    /// 对话协调器 - 管理当前对话目标，解决硬编码问题
    /// </summary>
    public class DialogueCoordinator : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour currentTarget;
        
        private IConversationTarget _conversationTarget;
        
        public static DialogueCoordinator Instance { get; private set; }
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            ValidateTarget();
        }
        
        void OnValidate()
        {
            ValidateTarget();
        }
        
        private void ValidateTarget()
        {
            if (currentTarget != null)
            {
                _conversationTarget = currentTarget as IConversationTarget;
                if (_conversationTarget == null)
                {
                    Debug.LogError($"[DialogueCoordinator] Current target {currentTarget.name} does not implement IConversationTarget");
                }
            }
        }
        
        /// <summary>
        /// 设置当前对话目标
        /// </summary>
        public void SetConversationTarget(MonoBehaviour target)
        {
            currentTarget = target;
            ValidateTarget();
            
            if (_conversationTarget != null)
            {
                Debug.Log($"[DialogueCoordinator] Conversation target set to: {target.name}");
            }
        }
        
        /// <summary>
        /// 接收语音转录并转发给当前目标
        /// </summary>
        public void ReceiveTranscription(string transcription)
        {
            if (_conversationTarget == null)
            {
                Debug.LogWarning("[DialogueCoordinator] No conversation target set");
                return;
            }
            
            Debug.Log($"[DialogueCoordinator] Forwarding transcription to {_conversationTarget.GetProfile()?.scenarioName}");
            _conversationTarget.ReceiveNurseTranscription(transcription);
        }
        
        /// <summary>
        /// 获取当前对话目标
        /// </summary>
        public IConversationTarget GetCurrentTarget()
        {
            return _conversationTarget;
        }
    }
}
