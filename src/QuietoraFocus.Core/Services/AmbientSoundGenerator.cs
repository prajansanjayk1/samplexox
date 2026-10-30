using System.Text;
using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class AmbientSoundGenerator
{
    private const int SampleRate = 44100;
    private const short Channels = 2;
    private const short BitsPerSample = 16;

    /// <summary>
    /// Procedurally synthesizes seamless WAV audio bytes in memory for the specified ambient sound type.
    /// Runs on any CPU with zero external dependencies.
    /// </summary>
    public static byte[] GenerateWavBytes(AmbientSoundType type, double durationSeconds = 6.0)
    {
        var totalSamples = (int)(SampleRate * durationSeconds);
        var bytesPerSample = BitsPerSample / 8;
        var dataLength = totalSamples * Channels * bytesPerSample;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 1. RIFF chunk descriptor
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        // 2. "fmt " sub-chunk
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // SubChunk1Size for PCM
        writer.Write((short)1); // AudioFormat 1 = PCM
        writer.Write(Channels);
        writer.Write(SampleRate);
        writer.Write(SampleRate * Channels * bytesPerSample); // ByteRate
        writer.Write((short)(Channels * bytesPerSample));     // BlockAlign
        writer.Write(BitsPerSample);

        // 3. "data" sub-chunk
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        // 4. Synthesize PCM 16-bit stereo samples
        var rand = new Random(42);
        double brownL = 0;
        double brownR = 0;

        for (int i = 0; i < totalSamples; i++)
        {
            double left = 0;
            double right = 0;
            double t = (double)i / SampleRate;

            switch (type)
            {
                case AmbientSoundType.BrownNoise:
                    // Warm, deep Brownian noise
                    double whiteL = (rand.NextDouble() * 2.0 - 1.0) * 0.2;
                    double whiteR = (rand.NextDouble() * 2.0 - 1.0) * 0.2;
                    brownL = (brownL * 0.95) + whiteL;
                    brownR = (brownR * 0.95) + whiteR;
                    left = brownL * 0.8;
                    right = brownR * 0.8;
                    break;

                case AmbientSoundType.WhiteNoise:
                    left = (rand.NextDouble() * 2.0 - 1.0) * 0.15;
                    right = (rand.NextDouble() * 2.0 - 1.0) * 0.15;
                    break;

                case AmbientSoundType.GentleRain:
                    // Modulated noise with gentle low-frequency wave swells
                    double lfo = 0.5 + 0.5 * Math.Sin(2.0 * Math.PI * 0.18 * t);
                    double w1 = (rand.NextDouble() * 2.0 - 1.0) * 0.12 * (0.6 + 0.4 * lfo);
                    double w2 = (rand.NextDouble() * 2.0 - 1.0) * 0.12 * (0.6 + 0.4 * lfo);
                    brownL = (brownL * 0.92) + w1;
                    brownR = (brownR * 0.92) + w2;
                    left = brownL * 0.7;
                    right = brownR * 0.7;
                    break;

                case AmbientSoundType.BinauralGamma40Hz:
                    // 200 Hz Left, 240 Hz Right (40Hz Gamma Beat for intense concentration)
                    left = 0.22 * Math.Sin(2.0 * Math.PI * 200.0 * t);
                    right = 0.22 * Math.Sin(2.0 * Math.PI * 240.0 * t);
                    // Add subtle low brown floor to make the tone pleasant
                    brownL = (brownL * 0.97) + (rand.NextDouble() * 2.0 - 1.0) * 0.02;
                    brownR = (brownR * 0.97) + (rand.NextDouble() * 2.0 - 1.0) * 0.02;
                    left += brownL * 0.15;
                    right += brownR * 0.15;
                    break;

                case AmbientSoundType.BinauralAlpha10Hz:
                    // 200 Hz Left, 210 Hz Right (10Hz Alpha Beat for calm relaxed study)
                    left = 0.22 * Math.Sin(2.0 * Math.PI * 200.0 * t);
                    right = 0.22 * Math.Sin(2.0 * Math.PI * 210.0 * t);
                    brownL = (brownL * 0.97) + (rand.NextDouble() * 2.0 - 1.0) * 0.02;
                    brownR = (brownR * 0.97) + (rand.NextDouble() * 2.0 - 1.0) * 0.02;
                    left += brownL * 0.15;
                    right += brownR * 0.15;
                    break;

                default:
                    left = 0;
                    right = 0;
                    break;
            }

            // Clamp and write 16-bit signed PCM
            short sLeft = (short)(Math.Clamp(left, -1.0, 1.0) * 32767);
            short sRight = (short)(Math.Clamp(right, -1.0, 1.0) * 32767);

            writer.Write(sLeft);
            writer.Write(sRight);
        }

        writer.Flush();
        return ms.ToArray();
    }
}
