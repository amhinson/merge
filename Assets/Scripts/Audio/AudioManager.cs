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
        [SerializeField] private float baseMergePitch = 0.8f;
        [SerializeField] private float mergePitchIncrement = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float mergeVolume = 0.3f;

        private const string SfxEnabledKey = "sfx_enabled";

        private AudioSource audioSource;
        private AudioClip proceduralMergeClip;

        public bool IsSfxEnabled { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            IsSfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;

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

        public void PlayDrop()
        {
            PlayClip(dropClip, 1f, 0.2f);
        }

        public void PlayMerge(int tierIndex)
        {
            float pitch = baseMergePitch + (tierIndex * mergePitchIncrement);
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
            audioSource.pitch = 1f;
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
