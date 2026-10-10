using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace RepoCity {

    public static class OllamaNarrator {

        public static IEnumerator GetNarration(FileEntry file, Action<string> onResult) {
            string narration = null;
            yield return OllamaClient.Generate(BuildPrompt(file), 60, 0.9f, 6, text => narration = text);
            onResult(string.IsNullOrEmpty(narration) ? TemplateNarration(file) : narration);
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
