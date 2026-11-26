using System;
using System.IO;
using System.Media;

namespace StardewDesktopPet
{
    public static class RetroAudio
    {
        public static bool Enabled { get; set; }

        private static readonly byte[] ChirpWav;
        private static readonly byte[] EatWav;
        private static readonly byte[] FortuneWav;

        static RetroAudio()
        {
            Enabled = true;
            ChirpWav = BuildSequenceWav(new[] { 660.0, 880.0 }, new[] { 0.055, 0.085 }, 0.14);
            EatWav = BuildSequenceWav(new[] { 523.25, 659.25, 783.99 }, new[] { 0.05, 0.05, 0.09 }, 0.14);
            FortuneWav = BuildSequenceWav(new[] { 523.25, 659.25, 783.99, 1046.50 }, new[] { 0.07, 0.07, 0.07, 0.14 }, 0.12);
        }

        public static void PlayChirp()
        {
            PlayBytes(ChirpWav);
        }

        public static void PlayEat()
        {
            PlayBytes(EatWav);
        }

        public static void PlayFortune()
        {
            PlayBytes(FortuneWav);
        }

        private static void PlayBytes(byte[] wavData)
        {
            if (!Enabled || wavData == null) return;
            try
            {
                using (MemoryStream ms = new MemoryStream(wavData))
                using (SoundPlayer player = new SoundPlayer(ms))
                {
                    player.Play();
                }
            }
            catch
            {
                // Ignore audio device failures silently
            }
        }

        private static byte[] BuildSequenceWav(double[] freqs, double[] durationsSec, double volume)
        {
            const int sampleRate = 22050;
            int totalSamples = 0;
            for (int i = 0; i < durationsSec.Length; i++)
            {
                totalSamples += (int)(sampleRate * durationsSec[i]);
            }

            int dataSize = totalSamples * 2;
            using (MemoryStream ms = new MemoryStream(44 + dataSize))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // RIFF header
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataSize);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(dataSize);

                for (int n = 0; n < freqs.Length; n++)
                {
                    double freq = freqs[n];
                    int count = (int)(sampleRate * durationsSec[n]);
                    for (int i = 0; i < count; i++)
                    {
                        double t = (double)i / sampleRate;
                        double progress = (double)i / count;
                        double env = Math.Sin(Math.PI * progress);
                        // Soft triangle/sine blend for cozy retro vibe
                        double raw = 0.7 * Math.Sin(2.0 * Math.PI * freq * t) + 0.3 * Math.Sin(4.0 * Math.PI * freq * t);
                        short sample = (short)(raw * env * volume * short.MaxValue);
                        bw.Write(sample);
                    }
                }
                return ms.ToArray();
            }
        }
    }
}
