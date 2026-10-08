using System;
using System.Collections.Generic;

namespace RepoCity {

    // Simple in-memory (non-persisted) milestone tracker. Fires OnToast the first time each
    // milestone is crossed; AchievementToastUI (built separately) is expected to subscribe and
    // display the message - this class has no UI dependency itself.
    public static class AchievementTracker {
        public static event Action<string> OnToast;

        static readonly HashSet<string> filesInspected = new HashSet<string>();
        static readonly HashSet<string> npcsTalkedTo = new HashSet<string>();
        static readonly HashSet<string> challengesCompleted = new HashSet<string>();
        static readonly HashSet<string> islandsEntered = new HashSet<string>();

        static bool firedFirstFile, fired5Files, fired15Files;
        static bool firedFirstNpc, fired5Npcs;
        static bool fired3Challenges;
        static bool firstIslandSeen;

        public static void NotifyFileInspected(string filePath) {
            if (string.IsNullOrEmpty(filePath)) return;
            filesInspected.Add(filePath);

            if (!firedFirstFile && filesInspected.Count >= 1) { firedFirstFile = true; Toast("First file inspected!"); }
            else if (!fired5Files && filesInspected.Count >= 5) { fired5Files = true; Toast("5 files explored"); }
            else if (!fired15Files && filesInspected.Count >= 15) { fired15Files = true; Toast("City historian: 15 files explored"); }
        }

        public static void NotifyNpcTalkedTo(string personaName) {
            if (string.IsNullOrEmpty(personaName)) return;
            npcsTalkedTo.Add(personaName);

            if (!firedFirstNpc && npcsTalkedTo.Count >= 1) { firedFirstNpc = true; Toast("Made a new friend"); }
            else if (!fired5Npcs && npcsTalkedTo.Count >= 5) { fired5Npcs = true; Toast("Local celebrity: 5 NPCs met"); }
        }

        public static void NotifyChallengeCompleted(string challengeId) {
            if (string.IsNullOrEmpty(challengeId)) return;
            if (!challengesCompleted.Add(challengeId)) return; // already completed, don't re-toast

            Toast("Challenge solved: " + challengeId);

            if (!fired3Challenges && challengesCompleted.Count >= 3) {
                fired3Challenges = true;
                Toast("Halfway there: 3 challenges solved");
            }
        }

        public static void NotifyIslandEntered(string islandLabel) {
            if (string.IsNullOrEmpty(islandLabel)) return;

            if (!firstIslandSeen) {
                firstIslandSeen = true;
                islandsEntered.Add(islandLabel);
                return; // spawn island - not a toast-worthy "entry"
            }

            if (islandsEntered.Add(islandLabel)) {
                Toast("Welcome to " + islandLabel);
            }
        }

        static void Toast(string message) {
            OnToast?.Invoke(message);
        }
    }
}
