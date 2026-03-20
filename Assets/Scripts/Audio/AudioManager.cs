using UnityEngine;

namespace MergeGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        public AudioClip dropClip;
        public AudioClip mergeClip;
        public AudioClip gameOverClip;

        [Header("Settings")]
        [SerializeField] private float baseMergePitch = 0.8f;
        [SerializeField] private float mergePitchIncrement = 0.1f;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void PlayDrop()
        {
            PlayClip(dropClip, 1f);
        }

        public void PlayMerge(int tierIndex)
        {
            float pitch = baseMergePitch + (tierIndex * mergePitchIncrement);
            PlayClip(mergeClip, pitch);
        }

        public void PlayGameOver()
        {
            PlayClip(gameOverClip, 1f);
        }

        private void PlayClip(AudioClip clip, float pitch)
        {
            if (clip == null || audioSource == null) return;

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
            audioSource.pitch = 1f; // Reset for next sound
        }
    }
}
