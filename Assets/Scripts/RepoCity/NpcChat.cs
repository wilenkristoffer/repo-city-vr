using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RepoCity {

    public static class NpcChat {

        private const string TagsUrl = "http://localhost:11434/api/tags";
        private const string GenerateUrl = "http://localhost:11434/api/generate";

        [System.Serializable]
        private class OllamaResponse {
            public string response;
        }

        public static IEnumerator GetLine(NpcInfo info, Action<string> onResult) {
            UnityWebRequest pingRequest = UnityWebRequest.Get(TagsUrl);
            pingRequest.timeout = 1;
            yield return pingRequest.SendWebRequest();

            if (pingRequest.result != UnityWebRequest.Result.Success) {
                onResult(PresetLine());
                yield break;
            }

            string prompt = BuildPrompt(info);
            string bodyJson = "{\"model\":\"llama3.2\",\"prompt\":\"" + EscapeJsonString(prompt) +
                "\",\"stream\":false,\"options\":{\"num_predict\":50,\"temperature\":1.0}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

            UnityWebRequest genRequest = new UnityWebRequest(GenerateUrl, "POST");
            genRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            genRequest.downloadHandler = new DownloadHandlerBuffer();
            genRequest.SetRequestHeader("Content-Type", "application/json");
            genRequest.timeout = 3;

            yield return genRequest.SendWebRequest();

            if (genRequest.result != UnityWebRequest.Result.Success) {
                onResult(PresetLine());
                yield break;
            }

            string line = ParseLine(genRequest.downloadHandler.text);
            if (string.IsNullOrEmpty(line)) {
                onResult(PresetLine());
            } else {
                onResult(line.Trim());
            }
        }

        private static string ParseLine(string responseJson) {
            try {
                OllamaResponse parsed = JsonUtility.FromJson<OllamaResponse>(responseJson);
                return parsed != null ? parsed.response : null;
            } catch {
                return null;
            }
        }

        private static string BuildPrompt(NpcInfo info) {
            StringBuilder sb = new StringBuilder();
            sb.Append("You are ").Append(info.personaName).Append(", a ").Append(info.personaJob)
                .Append(" living in a small city built out of a codebase. ");
            sb.Append("A visitor just walked up to you. Say ONE short, friendly, in-character line of small talk (max 20 words). ");
            sb.Append("No preamble, no quotation marks, just the line.");
            return sb.ToString();
        }

        private static string EscapeJsonString(string s) {
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s) {
                switch (c) {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\n"); break;
                    case '\r': sb.Append("\r"); break;
                    case '\t': sb.Append("\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private static readonly string[] PresetLines = new string[] {
            "Lovely day to wander around, isn't it?",
            "Have you seen the new building going up over there?",
            "I love what they've done with this part of town.",
            "First time visiting, or do you live around here too?",
            "Careful on the ring road, traffic picks up around here.",
            "This city keeps changing every time I look away.",
            "You picked a nice spot to stop and chat.",
            "I could use a break from work, honestly.",
            "The view from up the street is worth the walk.",
            "Nice to see a new face around the block."
        };

        public static string PresetLine() {
            int index = UnityEngine.Random.Range(0, PresetLines.Length);
            return PresetLines[index];
        }
    }
}
