using UnityEngine;

namespace MergeGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips (optional — falls back to procedural)")]
        public AudioClip dropClip;
        public AudioClip mergeClip;
        public AudioClip gameOverClip;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float mergeVolume = 0.3f;

        private const string SfxEnabledKey = "sfx_enabled";
        private const string ScaleKey = "merge_scale";
        private const string KeyKey = "merge_key";
        private const string InstrumentKey = "merge_instrument";

        private AudioSource audioSource;
        private AudioClip[] instrumentClips;

        public bool IsSfxEnabled { get; private set; }

        // Available instruments
        public static readonly string[] InstrumentNames = { "Ding", "Marimba", "Pluck", "Bleep", "Bell", "Bubble", "Kalimba" };
        public int CurrentInstrumentIndex { get; private set; }

        // Available merge scales
        public static readonly string[] ScaleNames = { "Major", "Minor", "Pentatonic", "Blues", "Chromatic", "Whole Tone" };
        private static readonly int[][] Scales =
        {
            new[] { 0, 2, 4, 5, 7, 9, 11, 12 },           // Major
            new[] { 0, 2, 3, 5, 7, 8, 10, 12 },            // Minor
            new[] { 0, 2, 4, 7, 9, 12 },                    // Pentatonic
            new[] { 0, 3, 5, 6, 7, 10, 12 },                // Blues
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, // Chromatic
            new[] { 0, 2, 4, 6, 8, 10, 12 },                // Whole Tone
        };

        public int CurrentScaleIndex { get; private set; }

        // Keys: semitone offset from C, centered so no key is more than 6 semitones away
        public static readonly string[] KeyNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static readonly int[] KeyOffsets =  {  0,   1,    2,   3,   4,   5,   6,   -5,  -4,   -3,  -2,   -1 };
        public int CurrentKeyIndex { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            IsSfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
            CurrentScaleIndex = PlayerPrefs.GetInt(ScaleKey, 0);
            CurrentKeyIndex = PlayerPrefs.GetInt(KeyKey, 0);
            CurrentInstrumentIndex = PlayerPrefs.GetInt(InstrumentKey, 0);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Generate all instrument clips
            instrumentClips = new AudioClip[InstrumentNames.Length];
            instrumentClips[0] = GenerateDing();
            instrumentClips[1] = GenerateMarimba();
            instrumentClips[2] = GeneratePluck();
            instrumentClips[3] = GenerateBleep();
            instrumentClips[4] = GenerateBell();
            instrumentClips[5] = GenerateBubble();
            instrumentClips[6] = GenerateKalimba();

            if (mergeClip == null)
                mergeClip = instrumentClips[CurrentInstrumentIndex];
        }

        public void SetSfxEnabled(bool enabled)
        {
            IsSfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void CycleScale()
        {
            CurrentScaleIndex = (CurrentScaleIndex + 1) % Scales.Length;
            PlayerPrefs.SetInt(ScaleKey, CurrentScaleIndex);
            PlayerPrefs.Save();
        }

        public void CycleKey()
        {
            CurrentKeyIndex = (CurrentKeyIndex + 1) % KeyNames.Length;
            PlayerPrefs.SetInt(KeyKey, CurrentKeyIndex);
            PlayerPrefs.Save();
        }

        public void CycleInstrument()
        {
            CurrentInstrumentIndex = (CurrentInstrumentIndex + 1) % InstrumentNames.Length;
            mergeClip = instrumentClips[CurrentInstrumentIndex];
            PlayerPrefs.SetInt(InstrumentKey, CurrentInstrumentIndex);
            PlayerPrefs.Save();
        }

        public void PlayDrop()
        {
            PlayClip(dropClip, 1f, 0.2f);
        }

        public void PlayMerge(int tierIndex, int chainLength = 1)
        {
            int[] scale = Scales[CurrentScaleIndex];
            int index = chainLength - 1;
            int octaves = index / scale.Length;
            int step = index % scale.Length;
            float semitones = KeyOffsets[CurrentKeyIndex] + scale[step] + octaves * 12;
            float pitch = Mathf.Pow(2f, semitones / 12f);
            PlayClip(mergeClip, pitch, mergeVolume);
        }

        public void PlayGameOver()
        {
            PlayClip(gameOverClip, 1f, 0.4f);
        }

        private void PlayClip(AudioClip clip, float pitch, float volume = 0.3f)
        {
            if (!IsSfxEnabled) return;
            if (clip == null || audioSource == null) return;

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

        // ───── Procedural Instrument Generators ─────

        private const int SampleRate = 44100;

        private static AudioClip MakeClip(string name, float duration, System.Func<float, float, float> generator)
        {
            int count = Mathf.RoundToInt(SampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float n = (float)i / count;
                samples[i] = generator(t, n);
            }
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        /// <summary>Clean sine pop with octave overtone. Bright, neutral.</summary>
        private static AudioClip GenerateDing()
        {
            return MakeClip("Ding", 0.15f, (t, n) =>
            {
                float env = Mathf.Exp(-n * 12f);
                return (Sin(523f, t) * 0.6f + Sin(1047f, t) * 0.4f) * env;
            });
        }

        /// <summary>Sine with sharp attack, fast decay, quiet sub-octave. Woody, warm.</summary>
        private static AudioClip GenerateMarimba()
        {
            return MakeClip("Marimba", 0.2f, (t, n) =>
            {
                float env = Mathf.Exp(-n * 18f);
                float attack = Mathf.Clamp01(t * SampleRate / 8f); // ~8 sample attack
                return (Sin(523f, t) * 0.7f + Sin(261f, t) * 0.2f + Sin(1047f, t) * 0.1f) * env * attack;
            });
        }

        /// <summary>Sawtooth with fast decay. Gritty string pluck.</summary>
        private static AudioClip GeneratePluck()
        {
            return MakeClip("Pluck", 0.18f, (t, n) =>
            {
                float env = Mathf.Exp(-n * 16f);
                // Sawtooth from first 6 harmonics
                float wave = 0f;
                for (int h = 1; h <= 6; h++)
                    wave += Sin(523f * h, t) / h;
                wave *= 0.4f;
                return wave * env;
            });
        }

        /// <summary>Square wave with very fast decay. 8-bit arcade.</summary>
        private static AudioClip GenerateBleep()
        {
            return MakeClip("Bleep", 0.1f, (t, n) =>
            {
                float env = Mathf.Exp(-n * 20f);
                // Square wave: sign of sine
                float wave = Mathf.Sign(Sin(523f, t)) * 0.35f;
                return wave * env;
            });
        }

        /// <summary>Sine with detuned inharmonic overtone. Metallic shimmer.</summary>
        private static AudioClip GenerateBell()
        {
            return MakeClip("Bell", 0.35f, (t, n) =>
            {
                float env1 = Mathf.Exp(-n * 8f);
                float env2 = Mathf.Exp(-n * 12f);
                // Inharmonic overtone at ~2.76x (not a perfect interval)
                return Sin(523f, t) * 0.5f * env1 + Sin(1443f, t) * 0.3f * env2 + Sin(784f, t) * 0.2f * env1;
            });
        }

        /// <summary>Sine with pitch bend up and medium decay. Playful pop.</summary>
        private static AudioClip GenerateBubble()
        {
            return MakeClip("Bubble", 0.15f, (t, n) =>
            {
                float env = Mathf.Exp(-n * 14f);
                // Pitch bends up from ~60% to 100% over the first 30ms
                float bend = 0.6f + 0.4f * Mathf.Clamp01(t / 0.03f);
                return Sin(523f * bend, t) * 0.7f * env;
            });
        }

        /// <summary>Sine with slight pitch bend down, longer gentle decay. Thumb piano.</summary>
        private static AudioClip GenerateKalimba()
        {
            return MakeClip("Kalimba", 0.3f, (t, n) =>
            {
                float env = Mathf.Exp(-n * 7f);
                // Slight downward pitch bend at start
                float bend = 1f + 0.08f * Mathf.Exp(-t * 60f);
                return (Sin(523f * bend, t) * 0.6f + Sin(1047f * bend, t) * 0.25f + Sin(1570f, t) * 0.15f * Mathf.Exp(-n * 15f)) * env;
            });
        }
    }
}
