using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RepoCity {

    public static class OllamaNarrator {

        private const string TagsUrl = "http://localhost:11434/api/tags";
        private const string GenerateUrl = "http://localhost:11434/api/generate";

        [System.Serializable]
        private class OllamaResponse {
            public string response;
        }

        public static IEnumerator GetNarration(FileEntry file, Action<string> onResult) {
            UnityWebRequest pingRequest = UnityWebRequest.Get(TagsUrl);
            pingRequest.timeout = 1;
            yield return pingRequest.SendWebRequest();

            if (pingRequest.result != UnityWebRequest.Result.Success) {
                onResult(TemplateNarration(file));
                yield break;
            }

            string prompt = BuildPrompt(file);
            string bodyJson = "{\"model\":\"llama3.2\",\"prompt\":\"" + EscapeJsonString(prompt) +
                "\",\"stream\":false,\"options\":{\"num_predict\":60,\"temperature\":0.9}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

            UnityWebRequest genRequest = new UnityWebRequest(GenerateUrl, "POST");
            genRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            genRequest.downloadHandler = new DownloadHandlerBuffer();
            genRequest.SetRequestHeader("Content-Type", "application/json");
            genRequest.timeout = 3;

            yield return genRequest.SendWebRequest();

            if (genRequest.result != UnityWebRequest.Result.Success) {
                onResult(TemplateNarration(file));
                yield break;
            }

            string narration = ParseNarration(genRequest.downloadHandler.text);
            if (string.IsNullOrEmpty(narration)) {
                onResult(TemplateNarration(file));
            } else {
                onResult(narration.Trim());
            }
        }

        private static string ParseNarration(string responseJson) {
            try {
                OllamaResponse parsed = JsonUtility.FromJson<OllamaResponse>(responseJson);
                return parsed != null ? parsed.response : null;
            } catch {
                return null;
            }
        }

        private static string BuildPrompt(FileEntry file) {
            string lang = string.IsNullOrEmpty(file.ext) ? "none" : file.ext;
            string lastTouched;
            if (string.IsNullOrEmpty(file.lastModified)) {
                lastTouched = "unknown";
            } else {
                int len = Math.Min(10, file.lastModified.Length);
                lastTouched = file.lastModified.Substring(0, len);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("You are a tour guide in a VR city built out of a codebase, where every building is one file. ");
            sb.Append("Describe this building in ONE vivid, punchy sentence (max 25 words) in character as the tour guide. ");
            sb.Append("No preamble, no quotation marks, just the sentence.\n");
            sb.Append("File: ").Append(file.path).Append("\n");
            sb.Append("Language: ").Append(lang).Append("\n");
            sb.Append("Size: ").Append(file.size).Append(" bytes\n");
            sb.Append("Commits: ").Append(file.commits).Append("\n");
            sb.Append("Last touched: ").Append(lastTouched);
            return sb.ToString();
        }

        private static string EscapeJsonString(string s) {
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s) {
                switch (c) {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        public static string TemplateNarration(FileEntry file) {
            uint hash = HashString(file.path);
            Func<float> rng = Mulberry32(hash);

            int sizeTier = file.size > 20000 ? 2 : (file.size > 3000 ? 1 : 0);
            int activityTier = file.commits > 15 ? 2 : (file.commits > 3 ? 1 : 0);

            string[][] sizePhrases = new string[][] {
                new string[] { "a modest little outpost", "a tiny one-room affair", "barely more than a shed" },
                new string[] { "a solid mid-rise", "a comfortably-sized block", "a reasonable piece of the skyline" },
                new string[] { "a sprawling tower", "one of the biggest buildings in town", "an imposing landmark" }
            };

            string[][] activityPhrases = new string[][] {
                new string[] { "quietly undisturbed for ages", "gathering dust in peace", "left well alone" },
                new string[] { "visited every so often", "getting the occasional renovation", "ticking along steadily" },
                new string[] { "in a constant state of renovation", "rebuilt more times than anyone can count", "the busiest construction site in the city" }
            };

            string sizePhrase = PickPhrase(sizePhrases[sizeTier], rng);
            string activityPhrase = PickPhrase(activityPhrases[activityTier], rng);

            return file.name + " is " + sizePhrase + ", " + activityPhrase + ".";
        }

        private static string PickPhrase(string[] options, Func<float> rng) {
            int index = (int)(rng() * options.Length);
            if (index >= options.Length) { index = options.Length - 1; }
            if (index < 0) { index = 0; }
            return options[index];
        }

        private static uint HashString(string s) {
            uint h = 2166136261;
            foreach (char c in s) { h ^= c; h *= 16777619; }
            return h;
        }

        private static Func<float> Mulberry32(uint seed) {
            uint a = seed;
            return () => {
                a += 0x6D2B79F5;
                uint t = a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return ((t ^ (t >> 14)) >> 0) / 4294967296f;
            };
        }
    }
}
