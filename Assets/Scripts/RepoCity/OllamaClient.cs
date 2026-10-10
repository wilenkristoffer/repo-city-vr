using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RepoCity {

    // Shared access to a local Ollama server for NPC chat and building narration. Picks the first
    // installed model from PreferredModels (small and fast first: the GPU is also rendering VR),
    // so it works with whatever the player already has instead of one hard-coded model.
    public static class OllamaClient {

        const string TagsUrl = "http://localhost:11434/api/tags";
        const string GenerateUrl = "http://localhost:11434/api/generate";
        const float RecheckSeconds = 30f;

        static readonly string[] PreferredModels = {
            "gemma3:4b", "gemma4:e4b", "qwen2.5:7b", "qwen2.5:3b", "gemma4:e2b",
            "llama3.2", "llama3.2:latest", "llama3.1:8b", "llama3:latest",
        };

        // Null when no server/model is available; callers then use their preset text.
        public static string SelectedModel { get; private set; }

        static float lastCheckTime = -999f;
        static bool checking;

        [Serializable] class TagsResponse { public ModelInfo[] models; }
        [Serializable] class ModelInfo { public string name; }
        [Serializable] class GenerateResponse { public string response; }

        // Finds a usable model (cached; re-checks at most every 30 s while none is found, so starting
        // Ollama mid-game is picked up).
        public static IEnumerator EnsureModel() {
            if (SelectedModel != null) yield break;
            while (checking) yield return null;
            if (SelectedModel != null || Time.realtimeSinceStartup - lastCheckTime < RecheckSeconds) yield break;

            checking = true;
            lastCheckTime = Time.realtimeSinceStartup;
            UnityWebRequest request = UnityWebRequest.Get(TagsUrl);
            request.timeout = 1;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) {
                SelectedModel = PickModel(request.downloadHandler.text);
                if (SelectedModel != null) Debug.Log("[Ollama] Using model " + SelectedModel);
                else Debug.Log("[Ollama] Server running but no models installed; using preset lines.");
            }
            request.Dispose();
            checking = false;
        }

        static string PickModel(string tagsJson) {
            TagsResponse tags;
            try { tags = JsonUtility.FromJson<TagsResponse>(tagsJson); } catch { return null; }
            if (tags == null || tags.models == null || tags.models.Length == 0) return null;
            foreach (string preferred in PreferredModels) {
                foreach (ModelInfo m in tags.models) {
                    if (m != null && m.name == preferred) return m.name;
                }
            }
            // Nothing preferred installed: take any non-embedding model.
            foreach (ModelInfo m in tags.models) {
                if (m != null && !string.IsNullOrEmpty(m.name) && !m.name.Contains("embed")) return m.name;
            }
            return null;
        }

        // Loads the model into memory ahead of time (an empty prompt only loads it) so the first NPC
        // line doesn't hit the multi-second cold start and time out to a preset.
        public static IEnumerator Warmup() {
            GameSettings.EnsureLoaded();
            if (!GameSettings.AiDialogue) yield break;
            yield return EnsureModel();
            if (SelectedModel == null) yield break;
            string body = "{\"model\":\"" + SelectedModel + "\",\"prompt\":\"\",\"keep_alive\":\"30m\"}";
            UnityWebRequest request = Post(body, 60);
            yield return request.SendWebRequest();
            request.Dispose();
        }

        // Frees the model's GPU memory right away (keep_alive 0) when AI dialogue is switched off.
        public static IEnumerator Unload() {
            if (SelectedModel == null) yield break;
            string body = "{\"model\":\"" + SelectedModel + "\",\"prompt\":\"\",\"keep_alive\":0}";
            UnityWebRequest request = Post(body, 10);
            yield return request.SendWebRequest();
            request.Dispose();
        }

        // Calls onResult with the generated text, or null on any failure or when AI dialogue is
        // switched off in Settings (callers fall back to presets).
        public static IEnumerator Generate(string prompt, int maxTokens, float temperature, int timeoutSeconds, Action<string> onResult) {
            GameSettings.EnsureLoaded();
            if (!GameSettings.AiDialogue) { onResult(null); yield break; }
            yield return EnsureModel();
            if (SelectedModel == null) { onResult(null); yield break; }

            string body = "{\"model\":\"" + SelectedModel + "\",\"prompt\":\"" + EscapeJson(prompt) +
                "\",\"stream\":false,\"keep_alive\":\"30m\",\"options\":{\"num_predict\":" + maxTokens +
                ",\"temperature\":" + temperature.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}}";
            UnityWebRequest request = Post(body, timeoutSeconds);
            yield return request.SendWebRequest();

            string text = null;
            if (request.result == UnityWebRequest.Result.Success) {
                try {
                    GenerateResponse parsed = JsonUtility.FromJson<GenerateResponse>(request.downloadHandler.text);
                    text = parsed != null ? parsed.response : null;
                } catch { text = null; }
            }
            request.Dispose();
            onResult(string.IsNullOrEmpty(text) ? null : text.Trim().Trim('"', '“', '”'));
        }

        static UnityWebRequest Post(string body, int timeoutSeconds) {
            UnityWebRequest request = new UnityWebRequest(GenerateUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;
            return request;
        }

        static string EscapeJson(string s) {
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s) {
                switch (c) {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
