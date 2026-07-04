using System.IO;
using System.Media;
using System.Text;

namespace LincoFarmTool.Services;

/// <summary>
/// 程序合成的小老鼠「吱」声（高频上扬啁啾音），不依赖外部音频文件。
/// 每调一次 <see cref="PlayOnce"/> 播放一声。
/// </summary>
public static class SqueakSound
{
    private const int SampleRate = 44100;
    private static readonly byte[] Wav = BuildWav();

    public static void PlayOnce()
    {
        // 每次新建 player，让多声可叠加播放
        var player = new SoundPlayer(new MemoryStream(Wav));
        player.Play();
    }

    private static byte[] BuildWav()
    {
        const double dur = 0.16;                 // 每声 160ms
        int n = (int)(SampleRate * dur);
        var samples = new short[n];

        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SampleRate;
            double p = (double)i / (n - 1);      // 0..1 进度

            // 频率：先上扬后回落（“吱”的啁啾），叠加快速颤音
            double freq = 2000 + 1500 * Math.Sin(Math.PI * p)
                               + 140 * Math.Sin(2 * Math.PI * 55 * t);
            phase += 2 * Math.PI * freq / SampleRate;

            // 音量包络：平滑起落，略偏前
            double env = Math.Pow(Math.Sin(Math.PI * p), 0.5);

            double s = Math.Sin(phase) * 0.55            // 基频
                     + 0.18 * Math.Sin(phase * 2);       // 二次谐波添亮
            s *= env;

            samples[i] = (short)(Math.Clamp(s, -1.0, 1.0) * short.MaxValue);
        }

        return ToWav(samples);
    }

    private static byte[] ToWav(short[] samples)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int dataSize = samples.Length * 2;       // 16-bit 单声道
        int byteRate = SampleRate * 2;

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);                 // fmt chunk size
        bw.Write((short)1);           // PCM
        bw.Write((short)1);           // 单声道
        bw.Write(SampleRate);
        bw.Write(byteRate);
        bw.Write((short)2);           // block align
        bw.Write((short)16);          // 位深
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);
        foreach (short s in samples) bw.Write(s);

        bw.Flush();
        return ms.ToArray();
    }
}
