using UnityEngine;
using System.Text;
using System.IO;
using System;

/// <summary>
/// Unity中的WAV文件录制和音频播放功能工具类
/// 版本：1.0 alpha 1
///
/// - 使用"ToAudioClip"方法加载wav文件/字节数据
/// 加载.wav（PCM未压缩）文件，支持8、16、24和32位，并将数据转换为Unity的AudioClip
///
/// - 使用"FromAudioClip"方法保存wav文件/字节数据
/// 将AudioClip的float数据转换为16位wav字节数组
/// </summary>
/// <remarks>
/// C#特性说明：
/// - 静态类（static class）：不能实例化，只包含静态成员
/// - 常量（const）：固定不变的值
/// - 泛型：List<T>动态数组、BitConverter.ToXxx<T>()
/// - 文件I/O：FileStream、FileMode、FileAccess、SeekOrigin、Path.Combine、Directory.CreateDirectory、File.WriteAllBytes、File.ReadAllBytes
/// - 二进制操作：BitConverter、Byte[]、Int16、UInt16、Int32、UInt32、sbyte
/// - using语句：自动资源管理（MemoryStream）
/// - 字符串操作：Encoding.ASCII.GetBytes()、string.Format()
/// - 数组操作：Array、Buffer.BlockCopy()
/// - 数学函数：BitConverter.ToInt32()、BitConverter.ToInt16()、BitConverter.ToUInt16()
/// - switch语句：多分支选择结构
/// - 异常处理：try-catch块、throw抛出异常
/// - Debug.AssertFormat()：断言调试
/// - MemoryStream：内存流，用于在内存中处理字节数据
/// - Application.persistentDataPath：Unity持久化数据路径
/// - AudioClip.Create()：创建Unity音频剪辑
/// - AudioClip.SetData()：设置音频数据
/// - AudioClip.GetData()：获取音频数据
/// </remarks>
public class WavUtility
{
    // 强制保存为16位.wav
    const int BlockSize_16Bit = 2;

    /// <summary>
    /// 加载PCM格式*.wav音频文件（使用Unity的Application数据路径）并转换为AudioClip
    /// </summary>
    /// <param name="filePath">.wav文件的本地文件路径</param>
    /// <returns>AudioClip</returns>
    public static AudioClip ToAudioClip(string filePath)
    {
        if (!filePath.StartsWith(Application.persistentDataPath) && !filePath.StartsWith(Application.dataPath))
        {
            Debug.LogWarning("This only supports files that are stored using Unity's Application data path. \nTo load bundled resources use 'Resources.Load(\"filename\") typeof(AudioClip)' method. \nhttps://docs.unity3d.com/ScriptReference/Resources.Load.html");
            return null;
        }
        // 文件I/O：读取文件的所有字节
        byte[] fileBytes = File.ReadAllBytes(filePath);
        return ToAudioClip(fileBytes, 0);
    }

    /// <summary>
    /// 将字节数组转换为AudioClip
    /// </summary>
    /// <param name="fileBytes">WAV文件字节数组</param>
    /// <param name="offsetSamples">偏移采样数</param>
    /// <param name="name">AudioClip名称</param>
    /// <returns>AudioClip</returns>
    public static AudioClip ToAudioClip(byte[] fileBytes, int offsetSamples = 0, string name = "wav")
    {
        // 二进制操作：从字节数组中读取数据
        int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
        UInt16 audioFormat = BitConverter.ToUInt16(fileBytes, 20);

        // 仅支持未压缩的PCM wav文件
        string formatCode = FormatCode(audioFormat);
        Debug.AssertFormat(audioFormat == 1 || audioFormat == 65534, "Detected format code '{0}' {1}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.", audioFormat, formatCode);

        UInt16 channels = BitConverter.ToUInt16(fileBytes, 22);
        int sampleRate = BitConverter.ToInt32(fileBytes, 24);
        UInt16 bitDepth = BitConverter.ToUInt16(fileBytes, 34);

        int headerOffset = 16 + 4 + subchunk1 + 4;
        int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);

        float[] data;
        // switch语句：根据位深度选择转换方法
        switch (bitDepth)
        {
            case 8:
                data = Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            case 16:
                data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            case 24:
                data = Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            case 32:
                data = Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            default:
                // 异常处理：抛出异常
                throw new Exception(bitDepth + " bit depth is not supported.");
        }

        // Unity API：创建AudioClip
        AudioClip audioClip = AudioClip.Create(name, data.Length, (int)channels, sampleRate, false);
        audioClip.SetData(data, 0);
        return audioClip;
    }

    #region wav文件字节数组到Unity AudioClip转换方法

    /// <summary>
    /// 将8位字节数组转换为AudioClip数据
    /// </summary>
    private static float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 8-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

        float[] data = new float[wavSize];

        sbyte maxValue = sbyte.MaxValue;

        int i = 0;
        while (i < wavSize)
        {
            data[i] = (float)source[i] / maxValue;
            ++i;
        }

        return data;
    }

    /// <summary>
    /// 将16位字节数组转换为AudioClip数据
    /// </summary>
    private static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 16-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

        int x = sizeof(Int16); // 块大小 = 2
        int convertedSize = wavSize / x;

        float[] data = new float[convertedSize];

        Int16 maxValue = Int16.MaxValue;

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

        return data;
    }

    /// <summary>
    /// 将24位字节数组转换为AudioClip数据
    /// </summary>
    private static float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 24-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

        int x = 3; // 块大小 = 3
        int convertedSize = wavSize / x;

        int maxValue = Int32.MaxValue;

        float[] data = new float[convertedSize];

        // 数组操作：使用4字节块复制3字节，然后复制带有1偏移量的字节
        byte[] block = new byte[sizeof(int)];

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            Buffer.BlockCopy(source, offset, block, 1, x);
            data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

        return data;
    }

    /// <summary>
    /// 将32位字节数组转换为AudioClip数据
    /// </summary>
    private static float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 32-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);

        int x = sizeof(float); //  块大小 = 4
        int convertedSize = wavSize / x;

        Int32 maxValue = Int32.MaxValue;

        float[] data = new float[convertedSize];

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

        return data;
    }

    #endregion

    /// <summary>
    /// 将AudioClip转换为字节数组
    /// </summary>
    /// <param name="audioClip">AudioClip</param>
    /// <returns>字节数组</returns>
    public static byte[] FromAudioClip(AudioClip audioClip)
    {
        string file;
        return FromAudioClip(audioClip, out file, false);
    }

    /// <summary>
    /// 将AudioClip转换为字节数组并可选保存为文件
    /// </summary>
    /// <param name="audioClip">AudioClip</param>
    /// <param name="filepath">输出文件路径</param>
    /// <param name="saveAsFile">是否保存为文件</param>
    /// <param name="dirname">目录名称</param>
    /// <returns>字节数组</returns>
    public static byte[] FromAudioClip(AudioClip audioClip, out string filepath, bool saveAsFile = true, string dirname = "recordings")
    {
        // using语句：自动资源管理
        MemoryStream stream = new MemoryStream();

        const int headerSize = 44;

        // 获取位深度
        UInt16 bitDepth = 16;

        // 总文件大小 = 44字节头部格式 + audioClip.samples * 因子（由于float到Int16/sbyte的转换）
        int fileSize = audioClip.samples * BlockSize_16Bit + headerSize;

        // 写入块描述符（riff）
        WriteFileHeader(ref stream, fileSize);
        // 写入文件头（fmt）
        WriteFileFormat(ref stream, audioClip.channels, audioClip.frequency, bitDepth);
        // 写入数据块（data）
        WriteFileData(ref stream, audioClip, bitDepth);

        byte[] bytes = stream.ToArray();

        // 验证总字节数
        Debug.AssertFormat(bytes.Length == fileSize, "Unexpected AudioClip to wav format byte count: {0} == {1}", bytes.Length, fileSize);

        // 保存文件到持久化存储位置
        if (saveAsFile)
        {
            // 字符串操作：使用string.Format构建路径
            filepath = string.Format("{0}/{1}/{2}.{3}", Application.persistentDataPath, dirname, DateTime.UtcNow.ToString("yyMMdd-HHmmss-fff"), "wav");
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllBytes(filepath, bytes);
        }
        else
        {
            filepath = null;
        }

        // 释放资源
        stream.Dispose();

        return bytes;
    }

    #region 写入.wav文件函数

    /// <summary>
    /// 写入WAV文件头
    /// </summary>
    private static int WriteFileHeader(ref MemoryStream stream, int fileSize)
    {
        int count = 0;
        int total = 12;

        // riff块ID
        byte[] riff = Encoding.ASCII.GetBytes("RIFF");
        count += WriteBytesToMemoryStream(ref stream, riff, "ID");

        // riff块大小
        int chunkSize = fileSize - 8; // 总大小 - 8（头部的其他两个字段）
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(chunkSize), "CHUNK_SIZE");

        byte[] wave = Encoding.ASCII.GetBytes("WAVE");
        count += WriteBytesToMemoryStream(ref stream, wave, "FORMAT");

        // 验证头部
        Debug.AssertFormat(count == total, "Unexpected wav descriptor byte count: {0} == {1}", count, total);

        return count;
    }

    /// <summary>
    /// 写入WAV文件格式
    /// </summary>
    private static int WriteFileFormat(ref MemoryStream stream, int channels, int sampleRate, UInt16 bitDepth)
    {
        int count = 0;
        int total = 24;

        byte[] id = Encoding.ASCII.GetBytes("fmt ");
        count += WriteBytesToMemoryStream(ref stream, id, "FMT_ID");

        int subchunk1Size = 16; // 24 - 8
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(subchunk1Size), "SUBCHUNK_SIZE");

        UInt16 audioFormat = 1;
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(audioFormat), "AUDIO_FORMAT");

        UInt16 numChannels = Convert.ToUInt16(channels);
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(numChannels), "CHANNELS");

        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(sampleRate), "SAMPLE_RATE");

        int byteRate = sampleRate * channels * BytesPerSample(bitDepth);
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(byteRate), "BYTE_RATE");

        UInt16 blockAlign = Convert.ToUInt16(channels * BytesPerSample(bitDepth));
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(blockAlign), "BLOCK_ALIGN");

        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(bitDepth), "BITS_PER_SAMPLE");

        // 验证格式
        Debug.AssertFormat(count == total, "Unexpected wav fmt byte count: {0} == {1}", count, total);

        return count;
    }

    /// <summary>
    /// 写入WAV文件数据
    /// </summary>
    private static int WriteFileData(ref MemoryStream stream, AudioClip audioClip, UInt16 bitDepth)
    {
        int count = 0;
        int total = 8;

        // 从AudioClip复制float[]数据
        float[] data = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(data, 0);

        byte[] bytes = ConvertAudioClipDataToInt16ByteArray(data);

        byte[] id = Encoding.ASCII.GetBytes("data");
        count += WriteBytesToMemoryStream(ref stream, id, "DATA_ID");

        int subchunk2Size = Convert.ToInt32(audioClip.samples * BlockSize_16Bit);
        count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(subchunk2Size), "SAMPLES");

        // 验证头部
        Debug.AssertFormat(count == total, "Unexpected wav data id byte count: {0} == {1}", count, total);

        // 将字节写入流
        count += WriteBytesToMemoryStream(ref stream, bytes, "DATA");

        // 验证音频数据
        Debug.AssertFormat(bytes.Length == subchunk2Size, "Unexpected AudioClip to wav subchunk2 size: {0} == {1}", bytes.Length, subchunk2Size);

        return count;
    }

    /// <summary>
    /// 将AudioClip数据转换为Int16字节数组
    /// </summary>
    private static byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
    {
        // using语句：自动资源管理
        MemoryStream dataStream = new MemoryStream();

        int x = sizeof(Int16);

        Int16 maxValue = Int16.MaxValue;

        int i = 0;
        while (i < data.Length)
        {
            dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
            ++i;
        }
        byte[] bytes = dataStream.ToArray();

        // 验证转换后的字节
        Debug.AssertFormat(data.Length * x == bytes.Length, "Unexpected float[] to Int16 to byte[] size: {0} == {1}", data.Length * x, bytes.Length);

        dataStream.Dispose();

        return bytes;
    }

    /// <summary>
    /// 将字节数组写入内存流
    /// </summary>
    private static int WriteBytesToMemoryStream(ref MemoryStream stream, byte[] bytes, string tag = "")
    {
        int count = bytes.Length;
        stream.Write(bytes, 0, count);
        return count;
    }

    #endregion

    /// <summary>
    /// 计算AudioClip的位深度
    /// </summary>
    /// <param name="audioClip">AudioClip</param>
    /// <returns>位深度，应该是8或16或32位</returns>
    public static UInt16 BitDepth(AudioClip audioClip)
    {
        UInt16 bitDepth = Convert.ToUInt16(audioClip.samples * audioClip.channels * audioClip.length / audioClip.frequency);
        Debug.AssertFormat(bitDepth == 8 || bitDepth == 16 || bitDepth == 32, "Unexpected AudioClip bit depth: {0}. Expected 8 or 16 or 32 bit.", bitDepth);
        return bitDepth;
    }

    /// <summary>
    /// 计算每个样本的字节数
    /// </summary>
    private static int BytesPerSample(UInt16 bitDepth)
    {
        return bitDepth / 8;
    }

    /// <summary>
    /// 计算块大小
    /// </summary>
    private static int BlockSize(UInt16 bitDepth)
    {
        switch (bitDepth)
        {
            case 32:
                return sizeof(Int32); // 32位 -> 4字节（Int32）
            case 16:
                return sizeof(Int16); // 16位 -> 2字节（Int16）
            case 8:
                return sizeof(sbyte); // 8位 -> 1字节（sbyte）
            default:
                throw new Exception(bitDepth + " bit depth is not supported.");
        }
    }

    /// <summary>
    /// 获取格式代码
    /// </summary>
    private static string FormatCode(UInt16 code)
    {
        switch (code)
        {
            case 1:
                return "PCM";
            case 2:
                return "ADPCM";
            case 3:
                return "IEEE";
            case 7:
                return "μ-law";
            case 65534:
                return "WaveFormatExtensable";
            default:
                Debug.LogWarning("Unknown wav code format:" + code);
                return "";
        }
    }

}
