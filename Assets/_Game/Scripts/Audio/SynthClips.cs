using UnityEngine;

namespace CrowdSurge
{
    public enum Sfx
    {
        Click, Start, GatePositive, GateNegative, Pop, Clash, Saw, BattleCry, BossDown,
        Win, Lose, Star, Error, Whoosh
    }

    /// <summary>
    /// Procedural audio: every effect, jingle and the music loop are built from enveloped, filtered oscillators and
    /// noise (marimba plucks, bells, brass, timpani, soft pads) so nothing sounds like a raw chip-tune beep.
    /// Real recordings placed in Resources/Audio override any of them (file name = enum name, or "Music").
    /// </summary>
    public static class SynthClips
    {
        public const int Rate = 32000;
        private const float TwoPi = Mathf.PI * 2f;

        // ------------------------------------------------------------------ effects
        public static AudioClip Create(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click: return Click();
                case Sfx.Start: return Start();
                case Sfx.GatePositive: return GatePositive();
                case Sfx.GateNegative: return GateNegative();
                case Sfx.Pop: return Pop();
                case Sfx.Clash: return Clash();
                case Sfx.Saw: return SawZing();
                case Sfx.BattleCry: return BattleCry();
                case Sfx.BossDown: return BossDown();
                case Sfx.Win: return Win();
                case Sfx.Lose: return Lose();
                case Sfx.Star: return Bells("Star", new[] { 1046.5f, 1568f }, 0.08f, 0.55f, 0.3f, 0.7f);
                case Sfx.Whoosh: return Whoosh();
                default: return ErrorBuzz();
            }
        }

        // ------------------------------------------------------------------ building blocks
        private static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Soft(float x) => (float)System.Math.Tanh(x);

        private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

        private static float Edge(int i, int n, float attack = 0.004f, float release = 0.015f)
        {
            int a = Mathf.Max(1, (int)(attack * Rate)), r = Mathf.Max(1, (int)(release * Rate));
            return Mathf.Min(1f, Mathf.Min(i / (float)a, (n - i) / (float)r));
        }

        /// <summary>Marimba-like pluck: sine plus a short-lived overtone.</summary>
        private static void Pluck(float[] d, int start, float freq, float volume, float decay)
        {
            int len = Mathf.Min(d.Length - start, (int)(Mathf.Min(2f, 7f / decay) * Rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(TwoPi * freq * t) + 0.5f * Mathf.Sin(TwoPi * freq * 4f * t) * Mathf.Exp(-t * 18f) + 0.2f * Mathf.Sin(TwoPi * freq * 2f * t) * Mathf.Exp(-t * 8f);
                d[start + i] += s * Mathf.Exp(-t * decay) * Edge(i, len, 0.002f, 0.04f) * volume;
            }
        }

        /// <summary>FM bell: bright, glassy and short.</summary>
        private static void Bell(float[] d, int start, float freq, float volume, float brightness = 1f, float decay = 5.5f)
        {
            int len = Mathf.Min(d.Length - start, (int)(Mathf.Min(2.5f, 7f / decay) * Rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float index = 2.2f * brightness * Mathf.Exp(-t * 9f);
                float s = Mathf.Sin(TwoPi * freq * t + index * Mathf.Sin(TwoPi * freq * 3.5f * t)) * Mathf.Exp(-t * decay)
                          + 0.22f * brightness * Mathf.Sin(TwoPi * freq * 2f * t) * Mathf.Exp(-t * 11f);
                d[start + i] += s * Edge(i, len, 0.002f, 0.04f) * volume;
            }
        }

        private static AudioClip Bells(string name, float[] notes, float spacing, float tail, float volume, float brightness)
        {
            int step = (int)(spacing * Rate);
            var d = new float[step * (notes.Length - 1) + (int)(tail * Rate)];
            for (int k = 0; k < notes.Length; k++) Bell(d, k * step, notes[k], volume, brightness);
            return Finish(name, d, 0.08f, 0.25f, 0.4f);
        }

        /// <summary>Brass-like note: saw through a low-pass that opens during the attack, with slight vibrato.</summary>
        private static void Brass(float[] d, int start, int length, float freq, float volume, float attack = 0.06f)
        {
            float lp = 0f;
            for (int i = 0; i < length && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float vib = 1f + 0.004f * Mathf.Sin(TwoPi * 5.2f * t) * Mathf.Min(1f, t * 4f);
                float saw = 2f * ((freq * vib * t) % 1f) - 1f;
                float open = Mathf.Lerp(0.05f, 0.34f, Mathf.Clamp01(t / (attack * 2.5f)));
                lp += (saw - lp) * open;
                float env = Mathf.Min(1f, t / attack) * Mathf.Pow(Mathf.Clamp01(1f - i / (float)length), 0.7f);
                d[start + i] += lp * env * volume;
            }
        }

        /// <summary>Warm chord: detuned saws through a low-pass with slow attack and release.</summary>
        private static void Pad(float[] d, int start, int length, float[] freqs, float volume, float cutoff = 0.06f)
        {
            foreach (float f in freqs)
            {
                float a = 0f, b = 0f;
                for (int i = 0; i < length && start + i < d.Length; i++)
                {
                    float t = i / (float)Rate;
                    float sa = 2f * ((f * 1.003f * t) % 1f) - 1f;
                    float sb = 2f * ((f * 0.997f * t) % 1f) - 1f;
                    a += (sa - a) * cutoff;
                    b += (sb - b) * cutoff;
                    float env = Mathf.Min(1f, i / (0.25f * Rate)) * Mathf.Pow(Mathf.Clamp01(1f - i / (float)length), 1.4f);
                    d[start + i] += (a + b) * 0.5f * env * volume;
                }
            }
        }

        private static void Timpani(float[] d, int start, float freq, float volume)
        {
            var rng = new System.Random(start);
            float lp = 0f, phase = 0f;
            int len = Mathf.Min(d.Length - start, (int)(1.6f * Rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                phase += Mathf.Lerp(freq * 1.6f, freq, Mathf.Min(1f, t * 14f)) / Rate;
                lp += (Noise(rng) - lp) * 0.3f;
                float s = Mathf.Sin(TwoPi * phase) * Mathf.Exp(-t * 4.5f) + lp * Mathf.Exp(-t * 40f) * 0.5f;
                d[start + i] += s * Edge(i, len, 0.001f, 0.05f) * volume;
            }
        }

        private static void Echo(float[] d, int delay, float feedback)
        {
            for (int i = delay; i < d.Length; i++) d[i] += d[i - delay] * feedback;
        }

        /// <summary>Small room reverb (4 combs + 2 all-passes) so bells and brass sit in a space instead of sounding dry.</summary>
        private static void ApplyReverb(float[] d, float mix)
        {
            int[] combDelays = { 953, 1187, 1319, 1451 };
            var wet = new float[d.Length];
            foreach (int delay in combDelays)
            {
                var buf = new float[delay];
                float damp = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    int idx = i % delay;
                    float y = buf[idx];
                    damp += (y - damp) * 0.35f;
                    buf[idx] = d[i] + damp * 0.8f;
                    wet[i] += y * 0.25f;
                }
            }
            int[] apDelays = { 161, 53 };
            foreach (int delay in apDelays)
            {
                var buf = new float[delay];
                for (int i = 0; i < wet.Length; i++)
                {
                    int idx = i % delay;
                    float b = buf[idx];
                    float x = wet[i];
                    float y = -x + b;
                    buf[idx] = x + b * 0.5f;
                    wet[i] = y;
                }
            }
            for (int i = 0; i < d.Length; i++) d[i] = d[i] * (1f - mix * 0.5f) + wet[i] * mix * 2f;
        }

        /// <summary>Adds a reverb tail, normalizes to a perceived loudness (RMS of the audible part) and soft-limits the peaks.</summary>
        private static AudioClip Finish(string name, float[] dry, float rmsTarget, float reverb = 0f, float tailSeconds = 0f)
        {
            var d = new float[dry.Length + (int)(tailSeconds * Rate)];
            System.Array.Copy(dry, d, dry.Length);
            if (reverb > 0f) ApplyReverb(d, reverb);

            float peak = 0.0001f;
            foreach (float s in d) peak = Mathf.Max(peak, Mathf.Abs(s));
            double sum = 0;
            int count = 0;
            float gate = peak * 0.05f;
            foreach (float s in d)
            {
                if (Mathf.Abs(s) < gate) continue;
                sum += s * s;
                count++;
            }
            float rms = count > 0 ? Mathf.Sqrt((float)(sum / count)) : peak;
            float gain = rmsTarget / Mathf.Max(0.0001f, rms);
            for (int i = 0; i < d.Length; i++) d[i] = Soft(d[i] * gain) * 0.95f;
            return Build(name, d);
        }

        // ------------------------------------------------------------------ ui + gameplay effects
        private static AudioClip Click()
        {
            var d = new float[(int)(0.07f * Rate)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                d[i] = (Mathf.Sin(TwoPi * 560f * t) * 0.6f + Mathf.Sin(TwoPi * 1120f * t) * 0.18f) * Mathf.Exp(-t * 60f) * Edge(i, d.Length, 0.001f);
            }
            return Finish("Click", d, 0.055f);
        }

        private static AudioClip Start()
        {
            var d = new float[(int)(0.7f * Rate)];
            var rng = new System.Random(4);
            float lp = 0f, phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = i / (float)d.Length;
                lp += (Noise(rng) - lp) * Mathf.Lerp(0.04f, 0.4f, k);
                phase += Mathf.Lerp(240f, 760f, k * k) / Rate;
                d[i] = (lp * 0.8f + Mathf.Sin(TwoPi * phase) * 0.35f) * Mathf.Sin(k * Mathf.PI) * Edge(i, d.Length);
            }
            Bell(d, (int)(0.28f * Rate), 1318.5f, 0.3f);
            return Finish("Start", d, 0.085f, 0.12f, 0.35f);
        }

        /// <summary>Happy rising triad with a sparkle on top.</summary>
        private static AudioClip GatePositive()
        {
            var d = new float[(int)(0.75f * Rate)];
            Pluck(d, 0, 523.25f, 0.3f, 5f);
            Pluck(d, (int)(0.045f * Rate), 659.25f, 0.3f, 5f);
            Pluck(d, (int)(0.09f * Rate), 783.99f, 0.3f, 5f);
            Bell(d, (int)(0.12f * Rate), 1567.98f, 0.18f, 0.8f, 6.5f);
            Echo(d, (int)(0.11f * Rate), 0.22f);
            return Finish("GatePositive", d, 0.095f, 0.22f, 0.4f);
        }

        /// <summary>Soft downward "bwomp": a falling sine with a muted thud. Unpleasant but never harsh.</summary>
        private static AudioClip GateNegative()
        {
            var d = new float[(int)(0.5f * Rate)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = i / (float)d.Length;
                phase += Mathf.Lerp(420f, 110f, Mathf.Pow(k, 0.6f)) / Rate;
                float s = Mathf.Sin(TwoPi * phase) + 0.35f * Mathf.Sin(TwoPi * phase * 2f) * (1f - k);
                d[i] = s * Mathf.Exp(-t * 5f) * Edge(i, d.Length, 0.004f, 0.06f);
            }
            Timpani(d, 0, 70f, 0.35f);
            return Finish("GateNegative", d, 0.085f, 0.12f, 0.2f);
        }

        /// <summary>Cute bubble pop, played very often so it is short and quiet.</summary>
        private static AudioClip Pop()
        {
            var d = new float[(int)(0.11f * Rate)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += Mathf.Lerp(1100f, 320f, Mathf.Min(1f, t * 22f)) / Rate;
                d[i] = Mathf.Sin(TwoPi * phase) * Mathf.Exp(-t * 38f) * Edge(i, d.Length, 0.001f, 0.01f);
            }
            return Finish("Pop", d, 0.05f);
        }

        private static AudioClip Clash()
        {
            var d = new float[(int)(0.16f * Rate)];
            var rng = new System.Random(11);
            float lp = 0f, phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += Mathf.Lerp(160f, 60f, Mathf.Min(1f, t * 12f)) / Rate;
                lp += (Noise(rng) - lp) * 0.3f;
                d[i] = (Mathf.Sin(TwoPi * phase) * 0.8f + lp * 0.5f) * Mathf.Exp(-t * 22f) * Edge(i, d.Length, 0.001f, 0.02f);
            }
            return Finish("Clash", d, 0.065f);
        }

        private static AudioClip SawZing()
        {
            var d = new float[(int)(0.3f * Rate)];
            var rng = new System.Random(3);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = i / (float)d.Length;
                float f = 620f + 280f * Mathf.Sin(k * 22f);
                float ring = Mathf.Sin(TwoPi * f * t) * Mathf.Sin(TwoPi * f * 2.76f * t);
                lp += (Noise(rng) - lp) * 0.55f;
                d[i] = (ring * 0.7f + (Noise(rng) - lp) * 0.25f) * Mathf.Pow(1f - k, 1.3f) * Edge(i, d.Length, 0.002f, 0.03f);
            }
            return Finish("Saw", d, 0.055f);
        }

        private static AudioClip BattleCry()
        {
            var d = new float[(int)(0.85f * Rate)];
            Brass(d, 0, (int)(0.7f * Rate), 98f, 0.35f, 0.12f);
            Brass(d, 0, (int)(0.7f * Rate), 147f, 0.25f, 0.14f);
            Timpani(d, (int)(0.04f * Rate), 80f, 0.6f);
            Timpani(d, (int)(0.3f * Rate), 80f, 0.5f);
            return Finish("BattleCry", d, 0.1f, 0.12f, 0.3f);
        }

        private static AudioClip Whoosh()
        {
            var d = new float[(int)(0.4f * Rate)];
            var rng = new System.Random(8);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float k = i / (float)d.Length;
                lp += (Noise(rng) - lp) * Mathf.Lerp(0.05f, 0.4f, Mathf.Sin(k * Mathf.PI));
                d[i] = lp * Mathf.Pow(Mathf.Sin(k * Mathf.PI), 2f);
            }
            return Finish("Whoosh", d, 0.06f);
        }

        private static AudioClip ErrorBuzz()
        {
            var d = new float[(int)(0.25f * Rate)];
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float gate = (t < 0.1f || (t > 0.13f && t < 0.23f)) ? 1f : 0f;
                float saw = 2f * ((170f * t) % 1f) - 1f;
                lp += (saw - lp) * 0.1f;
                d[i] = lp * gate * Edge(i, d.Length, 0.004f, 0.02f);
            }
            return Finish("Error", d, 0.06f);
        }

        // ------------------------------------------------------------------ jingles
        /// <summary>Big triumphant fanfare: brass chords over timpani, bells and a rising sparkle. About 3 seconds.</summary>
        private static AudioClip Win()
        {
            var d = new float[(int)(3.4f * Rate)];
            int beat = (int)(0.22f * Rate);

            // Pickup notes
            Brass(d, 0, beat, 392f, 0.3f, 0.02f);
            Brass(d, beat, beat, 523.25f, 0.3f, 0.02f);
            Brass(d, beat * 2, beat, 659.25f, 0.3f, 0.02f);

            // Resolving chords: C - F - G - C
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392f, 523.25f },
                new[] { 349.23f, 440f, 523.25f, 698.46f },
                new[] { 392f, 493.88f, 587.33f, 783.99f },
                new[] { 523.25f, 659.25f, 783.99f, 1046.5f },
            };
            int[] starts = { beat * 3, beat * 5, beat * 7, beat * 9 };
            int[] lengths = { beat * 2, beat * 2, beat * 2, (int)(1.35f * Rate) };
            for (int c = 0; c < chords.Length; c++)
                foreach (float f in chords[c]) Brass(d, starts[c], lengths[c], f, 0.16f, 0.03f);

            Timpani(d, beat * 3, 98f, 0.6f);
            Timpani(d, beat * 5, 87f, 0.5f);
            Timpani(d, beat * 7, 98f, 0.5f);
            Timpani(d, beat * 9, 65f, 0.7f);

            // Sparkle run on the final chord
            float[] sparkle = { 1046.5f, 1318.5f, 1568f, 2093f, 2637f, 3136f };
            for (int k = 0; k < sparkle.Length; k++) Bell(d, beat * 9 + k * (int)(0.06f * Rate), sparkle[k], 0.2f, 0.9f, 4.5f);

            Echo(d, (int)(0.2f * Rate), 0.25f);
            return Finish("Win", d, 0.12f, 0.2f, 0.6f);
        }

        /// <summary>Gentle "aww": three soft falling notes with a warm minor chord. Sad but pleasant.</summary>
        private static AudioClip Lose()
        {
            var d = new float[(int)(2.4f * Rate)];
            int step = (int)(0.26f * Rate);
            float[] notes = { 392f, 349.23f, 293.66f };
            for (int k = 0; k < notes.Length; k++) Pluck(d, k * step, notes[k], 0.35f, 3f);
            Pad(d, step * 2, (int)(1.9f * Rate), new[] { 146.83f, 174.61f, 220f, 293.66f }, 0.1f);
            Pluck(d, step * 3, 146.83f, 0.3f, 1.8f);
            Echo(d, (int)(0.3f * Rate), 0.3f);
            return Finish("Lose", d, 0.095f, 0.22f, 0.7f);
        }

        /// <summary>Boss defeated: heavy boom, falling rumble, then bright fanfare notes.</summary>
        private static AudioClip BossDown()
        {
            var d = new float[(int)(2.2f * Rate)];
            var rng = new System.Random(12);
            float lp = 0f, phase = 0f;
            for (int i = 0; i < (int)(1.2f * Rate); i++)
            {
                float t = i / (float)Rate;
                phase += Mathf.Lerp(90f, 28f, Mathf.Min(1f, t * 3f)) / Rate;
                lp += (Noise(rng) - lp) * Mathf.Lerp(0.6f, 0.05f, Mathf.Min(1f, t * 2.5f));
                d[i] = (Mathf.Sin(TwoPi * phase) * Mathf.Exp(-t * 3.2f) + lp * Mathf.Exp(-t * 3.5f) * 0.9f) * Edge(i, d.Length, 0.001f, 0.1f);
            }
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int k = 0; k < notes.Length; k++) Brass(d, (int)((0.4f + k * 0.12f) * Rate), (int)(0.8f * Rate), notes[k], 0.2f, 0.02f);
            Bell(d, (int)(0.9f * Rate), 2093f, 0.2f, 0.9f, 3.5f);
            Echo(d, (int)(0.18f * Rate), 0.25f);
            return Finish("BossDown", d, 0.12f, 0.2f, 0.6f);
        }

        // ------------------------------------------------------------------ music
        /// <summary>Bouncy hyper-casual loop at 118 BPM: marimba arpeggios, round bass, soft pad and light drums. C - Am - F - G.</summary>
        public static AudioClip CreateMusic()
        {
            const float bpm = 118f;
            int beat = (int)(60f / bpm * Rate);
            const int beats = 32;
            int total = beat * beats;
            var mix = new float[total];

            float[] roots = { 130.81f, 110f, 87.31f, 98f };
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392f },
                new[] { 220f, 261.63f, 329.63f },
                new[] { 174.61f, 220f, 261.63f },
                new[] { 196f, 246.94f, 293.66f },
            };

            for (int c = 0; c < 4; c++)
                Pad(mix, c * 8 * beat, 8 * beat, chords[c], 0.07f, 0.04f);

            for (int b = 0; b < beats; b++)
            {
                int chord = (b / 8) % 4;
                int start = b * beat;

                Kick(mix, start, beat);
                if (b % 2 == 1) Clap(mix, start, b);
                Shaker(mix, start + beat / 2, beat / 2, b);

                // Bass: root on the beat, fifth on the "and"
                Pluck(mix, start, roots[chord], 0.26f, 6f);
                Pluck(mix, start + beat / 2, roots[chord] * (b % 4 == 3 ? 2f : 1.5f), 0.18f, 7f);

                // Marimba arpeggio, sixteenths with a little accent pattern
                for (int s = 0; s < 4; s++)
                {
                    float[] ch = chords[chord];
                    float f = ch[(s + b) % 3] * 2f * (s == 3 ? 2f : 1f);
                    Pluck(mix, start + s * beat / 4, f, s == 0 ? 0.1f : 0.07f, 12f);
                }
            }

            Echo(mix, (int)(beat * 0.75f), 0.24f);
            return Finish("Music", mix, 0.065f, 0.12f);
        }

        private static void Kick(float[] d, int start, int length)
        {
            int n = Mathf.Min(length, (int)(0.2f * Rate));
            float phase = 0f;
            for (int i = 0; i < n && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                phase += Mathf.Lerp(120f, 44f, Mathf.Min(1f, t * 13f)) / Rate;
                d[start + i] += Mathf.Sin(TwoPi * phase) * Mathf.Exp(-t * 17f) * 0.45f;
            }
        }

        private static void Clap(float[] d, int start, int seed)
        {
            var rng = new System.Random(seed + 50);
            int n = (int)(0.12f * Rate);
            float lp = 0f;
            for (int i = 0; i < n && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float x = Noise(rng);
                lp += (x - lp) * 0.35f;
                d[start + i] += (x - lp) * Mathf.Exp(-t * 34f) * 0.14f;
            }
        }

        private static void Shaker(float[] d, int start, int length, int seed)
        {
            var rng = new System.Random(seed * 7 + 3);
            int n = Mathf.Min(length, (int)(0.045f * Rate));
            float prev = 0f;
            for (int i = 0; i < n && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float x = Noise(rng);
                d[start + i] += (x - prev) * Mathf.Exp(-t * 95f) * 0.05f;
                prev = x;
            }
        }
    }
}
