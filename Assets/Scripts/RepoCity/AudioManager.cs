using System.Collections.Generic;
using UnityEngine;

namespace RepoCity {

    // Static facade over three persistent AudioSources (2D "ui", "music" and looping "ambience" buses) plus
    // PlayClipAtPoint for one-off positional sounds (footsteps, NPC blips). Volumes persist via
    // PlayerPrefs so the settings menu and in-game audio stay in sync across scene loads.
    public static class AudioManager {
        const string PrefMaster = "audio_master";
        const string PrefMusic = "audio_music";
        const string PrefSfx = "audio_sfx";

        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 0.6f;
        public static float SfxVolume { get; private set; } = 1f;

        static GameObject root;
        static AudioSource uiSource;
        static AudioSource musicSource;
        static AudioSource ambienceSource;
        static float ambienceLevel;
        static string currentAmbienceClip;
        static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        static string currentMusicClip;

        public static void EnsureInitialized() {
            if (root != null) return;

            MasterVolume = PlayerPrefs.GetFloat(PrefMaster, 1f);
            MusicVolume = PlayerPrefs.GetFloat(PrefMusic, 0.6f);
            SfxVolume = PlayerPrefs.GetFloat(PrefSfx, 1f);

            root = new GameObject("AudioManager");
            Object.DontDestroyOnLoad(root);

            uiSource = root.AddComponent<AudioSource>();
            uiSource.spatialBlend = 0f;
            uiSource.playOnAwake = false;

            musicSource = root.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = MusicVolume * MasterVolume;

            ambienceSource = root.AddComponent<AudioSource>();
            ambienceSource.spatialBlend = 0f;
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
        }

        static AudioClip LoadClip(string clipName) {
            if (clipCache.TryGetValue(clipName, out AudioClip cached)) return cached;
            AudioClip clip = Resources.Load<AudioClip>("RepoCity/Audio/" + clipName);
            clipCache[clipName] = clip;
            if (clip == null) Debug.LogWarning("AudioManager: missing clip '" + clipName + "'");
            return clip;
        }

        // Non-positional UI/notification sound (menu clicks, test pass/fail, NPC blips heard
        // globally regardless of distance).
        public static void PlayUi(string clipName, float volume = 1f) {
            EnsureInitialized();
            AudioClip clip = LoadClip(clipName);
            if (clip == null) return;
            uiSource.PlayOneShot(clip, volume * SfxVolume * MasterVolume);
        }

        // Positional one-shot (footsteps) that still respects the sfx/master volume sliders.
        public static void PlayAt(string clipName, Vector3 position, float volume = 1f) {
            EnsureInitialized();
            AudioClip clip = LoadClip(clipName);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * SfxVolume * MasterVolume);
        }

        public static void PlayMusic(string clipName, bool loop = true) {
            EnsureInitialized();
            if (currentMusicClip == clipName && musicSource.isPlaying) return;
            AudioClip clip = LoadClip(clipName);
            if (clip == null) return;
            currentMusicClip = clipName;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        // Low background bed (city traffic) under everything else. level is relative to the SFX bus,
        // so the settings sliders still control it.
        public static void PlayAmbience(string clipName, float level) {
            EnsureInitialized();
            ambienceLevel = level;
            ApplyAmbienceVolume();
            if (currentAmbienceClip == clipName && ambienceSource.isPlaying) return;
            AudioClip clip = LoadClip(clipName);
            if (clip == null) return;
            currentAmbienceClip = clipName;
            ambienceSource.clip = clip;
            ambienceSource.Play();
        }

        public static void StopAmbience() {
            if (ambienceSource != null) ambienceSource.Stop();
            currentAmbienceClip = null;
        }

        static void ApplyAmbienceVolume() {
            if (ambienceSource != null) ambienceSource.volume = ambienceLevel * SfxVolume * MasterVolume;
        }

        public static void StopMusic() {
            if (musicSource != null) musicSource.Stop();
            currentMusicClip = null;
        }

        public static void SetMasterVolume(float v) {
            EnsureInitialized();
            MasterVolume = Mathf.Clamp01(v);
            musicSource.volume = MusicVolume * MasterVolume;
            ApplyAmbienceVolume();
            PlayerPrefs.SetFloat(PrefMaster, MasterVolume);
            PlayerPrefs.Save();
        }

        public static void SetMusicVolume(float v) {
            EnsureInitialized();
            MusicVolume = Mathf.Clamp01(v);
            musicSource.volume = MusicVolume * MasterVolume;
            PlayerPrefs.SetFloat(PrefMusic, MusicVolume);
            PlayerPrefs.Save();
        }

        public static void SetSfxVolume(float v) {
            EnsureInitialized();
            SfxVolume = Mathf.Clamp01(v);
            ApplyAmbienceVolume();
            PlayerPrefs.SetFloat(PrefSfx, SfxVolume);
            PlayerPrefs.Save();
        }
    }
}
