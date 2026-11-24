using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrowdSurge.Editor
{
    /// <summary>
    /// Renders every generated sound to a WAV file (project root / AudioPreview) and logs peak / RMS / NaN checks,
    /// so the audio can be reviewed outside the game and sanity-checked automatically.
    /// </summary>
    public static class AudioReport
    {
        [MenuItem("CrowdSurge/Export Audio Preview")]
        public static void Run()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "AudioPreview");
            Directory.CreateDirectory(dir);

            foreach (Sfx sfx in Enum.GetValues(typeof(Sfx))) Export(dir, sfx.ToString(), SynthClips.Create(sfx));
            Export(dir, "Music", SynthClips.CreateMusic());
            Debug.Log("[AudioReport] exported to " + dir);
        }

        private static void Export(string dir, string name, AudioClip clip)
        {
            var data = new float[clip.samples];
            clip.GetData(data, 0);

            float peak = 0f;
            double sum = 0, sumSq = 0;
            int bad = 0;
            foreach (float s in data)
            {
                if (float.IsNaN(s) || float.IsInfinity(s)) { bad++; continue; }
                peak = Mathf.Max(peak, Mathf.Abs(s));
                sum += s;
                sumSq += s * s;
            }
            double rms = Math.Sqrt(sumSq / data.Length);
            Debug.Log($"[AudioReport] {name}: {clip.length:0.00}s peak={peak:0.00} rms={rms:0.000} dc={sum / data.Length:0.0000} nan={bad} edges=({data[0]:0.000},{data[data.Length - 1]:0.000})");
            WriteWav(Path.Combine(dir, name + ".wav"), data, clip.frequency);
        }

        private static void WriteWav(string path, float[] samples, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(rate);
                w.Write(rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (float s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }
        }
    }
}
