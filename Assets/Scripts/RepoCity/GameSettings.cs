using UnityEngine;

namespace RepoCity {

    // Non-audio persisted settings (AudioManager owns the volume sliders itself). Read by
    // Locomotion (mouse sensitivity) and applied directly to QualitySettings/Screen here.
    public static class GameSettings {
        const string PrefSensitivity = "settings_mouse_sensitivity";
        const string PrefQuality = "settings_quality";
        const string PrefFullscreen = "settings_fullscreen";
        const string PrefAiDialogue = "settings_ai_dialogue";

        public static float MouseSensitivity { get; private set; } = 1f;
        public static int QualityLevel { get; private set; } = -1;
        public static bool Fullscreen { get; private set; } = true;
        // NPC chat and building narration via local Ollama; off means preset lines only.
        public static bool AiDialogue { get; private set; } = true;

        static bool loaded;

        public static void EnsureLoaded() {
            if (loaded) return;
            loaded = true;

            MouseSensitivity = PlayerPrefs.GetFloat(PrefSensitivity, 1f);
            QualityLevel = PlayerPrefs.GetInt(PrefQuality, QualitySettings.GetQualityLevel());
            Fullscreen = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            AiDialogue = PlayerPrefs.GetInt(PrefAiDialogue, 1) == 1;

            ApplyQuality();
            ApplyFullscreen();
        }

        public static void SetMouseSensitivity(float v) {
            EnsureLoaded();
            MouseSensitivity = Mathf.Clamp(v, 0.1f, 5f);
            PlayerPrefs.SetFloat(PrefSensitivity, MouseSensitivity);
            PlayerPrefs.Save();
        }

        public static void SetQualityLevel(int level) {
            EnsureLoaded();
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(PrefQuality, QualityLevel);
            PlayerPrefs.Save();
            ApplyQuality();
        }

        public static void SetFullscreen(bool fullscreen) {
            EnsureLoaded();
            Fullscreen = fullscreen;
            PlayerPrefs.SetInt(PrefFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFullscreen();
        }

        public static void SetAiDialogue(bool enabled) {
            EnsureLoaded();
            AiDialogue = enabled;
            PlayerPrefs.SetInt(PrefAiDialogue, AiDialogue ? 1 : 0);
            PlayerPrefs.Save();
        }

        static void ApplyQuality() {
            QualitySettings.SetQualityLevel(QualityLevel, true);
        }

        static void ApplyFullscreen() {
            Screen.fullScreen = Fullscreen;
        }
    }
}
