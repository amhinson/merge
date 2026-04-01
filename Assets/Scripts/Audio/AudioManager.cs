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

        private AudioSource audioSource;
        private AudioClip proceduralMergeClip;

        public bool IsSfxEnabled { get; private set; }

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

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Generate procedural merge sound if no clip assigned
            if (mergeClip == null)
                mergeClip = GenerateMergeSound();
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

        /// <summary>
        /// Generates a short pitched pop/ding sound procedurally.
        /// Two layered sine tones with fast exponential decay.
        /// </summary>
        private static AudioClip GenerateMergeSound()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Base frequency (C5 = 523Hz) + overtone (octave above)
            float freq1 = 523f;
            float freq2 = 1047f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float normalizedT = (float)i / sampleCount;

                // Fast exponential decay
                float envelope = Mathf.Exp(-normalizedT * 12f);

                // Two sine tones blended
                float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.6f;
                float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.4f;

                samples[i] = (wave1 + wave2) * envelope;
            }

            var clip = AudioClip.Create("ProceduralMerge", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
