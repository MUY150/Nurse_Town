public interface ITTSProvider
{
    void ConvertTextToSpeech(string text);
    bool IsAvailable { get; }
}
