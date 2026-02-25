//	Copyright (c) 2012 Calvin Rien
//	http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// WAV文件保存工具类，用于将Unity的AudioClip保存为WAV格式文件
/// </summary>
/// <remarks>
/// C#特性说明：
/// - 静态类（static class）：不能实例化，只包含静态成员
/// - 常量（const）：固定不变的值
/// - 泛型：List<T>动态数组
/// - 文件I/O：FileStream、FileMode、FileAccess、SeekOrigin
/// - 二进制操作：BitConverter、Byte[]、Int16、UInt16
/// - using语句：自动资源管理
/// - 字符串操作：string.ToLower()、Path.Combine、Path.GetDirectoryName
/// - 数组操作：Array、List<T>的方法
/// - 数学函数：Mathf.Abs()
/// </remarks>
public static class SavWav
{
    const int HEADER_SIZE = 44;

    /// <summary>
    /// 保存AudioClip为WAV文件
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <param name="clip">音频剪辑</param>
    public static bool Save(string filename, AudioClip clip)
    {
        if (!filename.ToLower().EndsWith(".wav"))
        {
            filename += ".wav";
        }

        var filepath = Path.Combine(Application.persistentDataPath, filename);

        Debug.Log(filepath);

        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        using (var fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, clip);
            WriteHeader(fileStream, clip);
        }

        return true;
    }

    /// <summary>
    /// 去除静音（AudioClip版本）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="min">最小音量阈值</param>
    public static AudioClip TrimSilence(AudioClip clip, float min)
    {
        var samples = new float[clip.samples];

        clip.GetData(samples, 0);

        return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
    }

    /// <summary>
    /// 去除静音（List版本）
    /// </summary>
    /// <param name="samples">采样数据列表</param>
    /// <param name="min">最小音量阈值</param>
    /// <param name="channels">声道数</param>
    /// <param name="hz">采样率</param>
    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
    {
        return TrimSilence(samples, min, channels, hz, false, false);
    }

    /// <summary>
    /// 去除静音（完整版本）
    /// </summary>
    /// <param name="samples">采样数据列表</param>
    /// <param name="min">最小音量阈值</param>
    /// <param name="channels">声道数</param>
    /// <param name="hz">采样率</param>
    /// <param name="_3D">是否为3D音频</param>
    /// <param name="stream">是否为流式音频</param>
    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream)
    {
        int i;

        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }
        samples.RemoveRange(0, i);

        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }
        samples.RemoveRange(i, samples.Count - i);

        var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

        clip.SetData(samples.ToArray(), 0);

        return clip;
    }

    /// <summary>
    /// 创建空的WAV文件流
    /// </summary>
    /// <param name="filepath">文件路径</param>
    /// <returns>文件流</returns>
    static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    /// <summary>
    /// 转换并写入音频数据
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="clip">音频剪辑</param>
    static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples];

        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];

        Byte[] bytesData = new Byte[samples.Length * 2];

        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    /// <summary>
    /// 写入WAV文件头
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="clip">音频剪辑</param>
    static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
