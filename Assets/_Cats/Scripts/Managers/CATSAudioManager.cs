using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace _cats.Scripts.Core
{
    public class CATSAudioManager
    {
        private Dictionary<string, AudioClip> audioClips = new();
        private Queue<AudioSource> availableAudioSources = new();
        private List<AudioSource> activeAudioSources = new();
        
        private AudioSource musicSource;
        private GameObject audioHolder;
        private AudioMixerGroup sfxMixerGroup;
        private AudioMixerGroup musicMixerGroup;
        
        private float masterVolume = 1f;
        private float sfxVolume = 1f;
        private float musicVolume = 1f;
        private bool isMuted = false;
        
        private const int INITIAL_POOL_SIZE = 10;
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string MUTE_KEY = "AudioMuted";

        public CATSAudioManager()
        {
            InitializeAudioSystem();
            LoadVolumeSettings();
            CreateAudioSourcePool();
        }

        private void InitializeAudioSystem()
        {
            audioHolder = new GameObject("AudioManager");
            GameObject.DontDestroyOnLoad(audioHolder);
            
            // Create music source
            var musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(audioHolder.transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        private void CreateAudioSourcePool()
        {
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                CreateNewAudioSource();
            }
        }

        private AudioSource CreateNewAudioSource()
        {
            var go = new GameObject($"AudioSource_{availableAudioSources.Count + activeAudioSources.Count}");
            go.transform.SetParent(audioHolder.transform);
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            availableAudioSources.Enqueue(audioSource);
            return audioSource;
        }

        public void LoadAudioClip(string clipName, string resourcePath)
        {
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                audioClips[clipName] = clip;
            }
            else
            {
                CATSDebug.LogError($"Failed to load audio clip at path: {resourcePath}");
            }
        }

        public void LoadAudioClip(string clipName, AudioClip clip)
        {
            if (clip != null)
            {
                audioClips[clipName] = clip;
            }
        }

        public void PlaySFX(string clipName, float volume = 1f, float pitch = 1f)
        {
            if (isMuted) return;
            
            if (audioClips.TryGetValue(clipName, out var clip))
            {
                var source = GetAvailableAudioSource();
                source.clip = clip;
                source.volume = volume * sfxVolume * masterVolume;
                source.pitch = pitch;
                source.Play();
                
                activeAudioSources.Add(source);
                
                // Return to pool when finished
                CATSManager.Instance.TimeManager.SetAlarm((int)(clip.length / pitch) + 1, () =>
                {
                    ReturnAudioSource(source);
                });
            }
            else
            {
                CATSDebug.LogWarning($"Audio clip '{clipName}' not found!");
            }
        }

        public void PlaySFXOneShot(string clipName, Vector3 position, float volume = 1f)
        {
            if (isMuted) return;
            
            if (audioClips.TryGetValue(clipName, out var clip))
            {
                AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume * masterVolume);
            }
        }

        public void PlayMusic(string clipName, float fadeInTime = 0f)
        {
            if (audioClips.TryGetValue(clipName, out var clip))
            {
                if (fadeInTime > 0)
                {
                    StartMusicFade(clip, fadeInTime, true);
                }
                else
                {
                    musicSource.clip = clip;
                    musicSource.volume = isMuted ? 0 : musicVolume * masterVolume;
                    musicSource.Play();
                }
                
                CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnMusicChanged, clipName);
            }
        }

        public void StopMusic(float fadeOutTime = 0f)
        {
            if (fadeOutTime > 0)
            {
                StartMusicFade(null, fadeOutTime, false);
            }
            else
            {
                musicSource.Stop();
            }
        }

        private void StartMusicFade(AudioClip newClip, float fadeTime, bool fadeIn)
        {
            // Simple fade implementation - could be enhanced with coroutines
            if (fadeIn)
            {
                musicSource.clip = newClip;
                musicSource.volume = 0;
                musicSource.Play();
                // You'd implement the actual fade logic here
                musicSource.volume = isMuted ? 0 : musicVolume * masterVolume;
            }
            else
            {
                // Fade out logic
                musicSource.Stop();
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveVolumeSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = isMuted ? 0 : musicVolume * masterVolume;
            SaveVolumeSettings();
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        public void SetMute(bool mute)
        {
            isMuted = mute;
            UpdateAllVolumes();
            SaveVolumeSettings();
        }

        private void UpdateAllVolumes()
        {
            musicSource.volume = isMuted ? 0 : musicVolume * masterVolume;
            
            foreach (var source in activeAudioSources)
            {
                if (source.isPlaying)
                {
                    source.volume = isMuted ? 0 : sfxVolume * masterVolume;
                }
            }
        }

        private AudioSource GetAvailableAudioSource()
        {
            if (availableAudioSources.Count == 0)
            {
                return CreateNewAudioSource();
            }
            
            return availableAudioSources.Dequeue();
        }

        private void ReturnAudioSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            activeAudioSources.Remove(source);
            availableAudioSources.Enqueue(source);
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.SetInt(MUTE_KEY, isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            isMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
        }

        public float GetMasterVolume() => masterVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetMusicVolume() => musicVolume;
        public bool IsMuted() => isMuted;
    }
}