// IEffect.cs
namespace NurseTown.Core.Interfaces
{
    public interface IEffect
    {
        string EffectId { get; }
        void Trigger();
        void Stop();
    }
}
