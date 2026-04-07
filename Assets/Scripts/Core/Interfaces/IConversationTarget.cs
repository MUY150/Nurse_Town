// IConversationTarget.cs
namespace NurseTown.Core.Interfaces
{
    /// <summary>
    /// 对话目标接口 - 统一病人、护士等可对话角色
    /// </summary>
    public interface IConversationTarget
    {
        /// <summary>
        /// 接收护士语音转录文本
        /// </summary>
        void ReceiveNurseTranscription(string transcription);
        
        /// <summary>
        /// 获取角色档案
        /// </summary>
        PatientProfile GetProfile();
        
        /// <summary>
        /// 获取LLM客户端
        /// </summary>
        ILlmClient GetLlmClient();
        
        /// <summary>
        /// 获取动画控制器
        /// </summary>
        ICharacterAnimation GetAnimationController();
    }
}
