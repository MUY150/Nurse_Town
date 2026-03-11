/// <summary>
/// TTS提供者接口，定义文本转语音的基本功能
/// </summary>
public interface ITTSProvider
{
    /// <summary>
    /// 将文本转换为语音并播放
    /// </summary>
    /// <param name="text">要转换的文本</param>
    void ConvertTextToSpeech(string text);
    
    /// <summary>
    /// TTS服务是否可用
    /// </summary>
    bool IsAvailable { get; }
}
