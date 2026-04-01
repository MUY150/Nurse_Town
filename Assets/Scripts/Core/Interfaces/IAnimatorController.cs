// IAnimatorController.cs
namespace NurseTown.Core.Interfaces
{
    public interface IAnimatorController
    {
        void Play(string stateName, float transitionDuration = 0.25f);
        void CrossFade(string stateName, float transitionDuration);
        void SetFloat(string parameter, float value);
        void SetBool(string parameter, bool value);
        void SetInteger(string parameter, int value);
        bool HasState(string stateName);
    }
}
