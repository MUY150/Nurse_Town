// IConversationTarget.cs
namespace NurseTown.Core.Interfaces
{
    public interface IConversationTarget
    {
        void ReceiveNurseTranscription(string transcription);
        PatientProfile GetProfile();
        ILlmClient GetLlmClient();
        ICharacterAnimation GetAnimationController();
    }
}
