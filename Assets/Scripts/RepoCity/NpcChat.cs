using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace RepoCity {

    public static class NpcChat {

        const int RememberedLines = 3;

        // Picked fresh each time so repeated chats with one NPC wander between subjects.
        private static readonly string[] Topics = new string[] {
            "something about yourself or what you're up to",
            "something about yourself or what you're up to",
            "your job and how today is going",
            "a juicy bit of local gossip",
            "a building or street in this city",
            "a question for the visitor",
            "something you're excited or annoyed about right now",
            "an odd opinion you hold strongly",
        };

        public static IEnumerator GetLine(NpcInfo info, Action<string> onResult) {
            string line = null;
            yield return OllamaClient.Generate(BuildPrompt(info), 60, 1.0f, 4, text => line = text);
            if (string.IsNullOrEmpty(line)) {
                onResult(PresetLine());
                yield break;
            }
            info.pastLines.Add(line);
            if (info.pastLines.Count > RememberedLines) info.pastLines.RemoveAt(0);
            onResult(line);
        }

        private static string BuildPrompt(NpcInfo info) {
            StringBuilder sb = new StringBuilder();
            sb.Append("You are ").Append(info.personaName).Append(", a ").Append(info.personaJob)
                .Append(" in Repo City, a small town where every building is a file from a software project. ");
            if (!string.IsNullOrEmpty(info.personality)) sb.Append("Your personality: ").Append(info.personality).Append(". ");
            if (!string.IsNullOrEmpty(info.personalDetail)) sb.Append("About you: you ").Append(info.personalDetail).Append(". ");
            sb.Append("A visitor walks up to you. Say ONE short line (max 25 words) in character, about ")
                .Append(Topics[UnityEngine.Random.Range(0, Topics.Length)]).Append(". ");
            sb.Append("Let your personality show, be specific and a little surprising, and avoid generic small talk. ");
            if (info.pastLines.Count > 0) {
                sb.Append("You already said to this visitor: ");
                foreach (string past in info.pastLines) sb.Append('"').Append(past).Append("\" ");
                sb.Append("Say something new; you may refer back to it. ");
            }
            sb.Append("No preamble, no quotation marks, no name prefix, just the line.");
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
