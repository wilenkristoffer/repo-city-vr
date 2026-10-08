using System.Collections.Generic;
using UnityEngine;

namespace RepoCity
{
    // Marker + persona data, read by Locomotion.cs's interact-raycast (written separately) and by
    // NpcChat.cs (written separately) to build an AI prompt. This is a FROZEN CONTRACT other files
    // depend on exactly as named here - do not rename these fields.
    public class NpcInfo : MonoBehaviour
    {
        public string personaName;
        public string personaJob;
    }

    public static class NpcSpawner
    {
        static readonly string[] CharacterModels =
        {
            "character-female-a", "character-female-b", "character-female-c",
            "character-female-d", "character-female-e", "character-female-f",
            "character-male-a", "character-male-b", "character-male-c",
            "character-male-d", "character-male-e", "character-male-f",
        };

        static readonly string[] PersonaNames =
        {
            "Alex", "Sam", "Jordan", "Taylor", "Morgan", "Casey",
            "Riley", "Jamie", "Drew", "Quinn", "Avery", "Reese",
        };

        static readonly string[] PersonaJobs =
        {
            "barista", "courier", "street musician", "shopkeeper", "dog walker",
            "office worker", "tourist", "student", "mechanic", "artist", "jogger", "retiree",
        };

        public static void Spawn(Transform parent, List<Vector3> sidewalkLoopWaypoints, int npcCount)
        {
            if (sidewalkLoopWaypoints == null || sidewalkLoopWaypoints.Count == 0)
            {
                return;
            }

            Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

            for (int i = 0; i < npcCount; i++)
            {
                string modelName = CharacterModels[Random.Range(0, CharacterModels.Length)];

                GameObject prefab;
                if (!prefabCache.TryGetValue(modelName, out prefab))
                {
                    prefab = Resources.Load<GameObject>("RepoCity/Characters/" + modelName);
                    prefabCache[modelName] = prefab;
                }

                if (prefab == null)
                {
                    continue;
                }

                // Spread starting indices evenly around the loop, like traffic spacing.
                int startIndex = (int)((long)i * sidewalkLoopWaypoints.Count / npcCount);

                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = "NPC_" + modelName + "_" + i;
                instance.transform.position = sidewalkLoopWaypoints[startIndex];

                NpcInfo info = instance.AddComponent<NpcInfo>();
                info.personaName = PersonaNames[Random.Range(0, PersonaNames.Length)];
                info.personaJob = PersonaJobs[Random.Range(0, PersonaJobs.Length)];

                CapsuleCollider capsule = instance.AddComponent<CapsuleCollider>();
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0f, 0.9f, 0f);

                NpcWalker walker = instance.AddComponent<NpcWalker>();
                walker.Init(sidewalkLoopWaypoints, startIndex, modelName);
            }
        }
    }

    public class NpcWalker : MonoBehaviour
    {
        const float MinSpeed = 1.1f;
        const float MaxSpeed = 1.4f;
        const float WaypointArriveDistance = 0.3f;
        const float TurnDegreesPerSecond = 180f;

        List<Vector3> waypoints;
        int targetIndex;
        float speed;

        float pauseTimer;
        bool paused;
        float nextPauseCheckTime;

        Animation legacyAnimation;
        bool hasWalkClip;
        bool hasIdleClip;

        public void Init(List<Vector3> loopWaypoints, int startIndex, string modelName)
        {
            waypoints = loopWaypoints;
            targetIndex = (startIndex + 1) % waypoints.Count;
            speed = Random.Range(MinSpeed, MaxSpeed);
            nextPauseCheckTime = Time.time + Random.Range(8f, 20f);

            SetupAnimation(modelName);
            PlayWalk();
        }

        void SetupAnimation(string modelName)
        {
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>("RepoCity/Characters/" + modelName);

            AnimationClip walkClip = null;
            AnimationClip idleClip = null;
            foreach (AnimationClip clip in clips)
            {
                if (clip.name == "walk") walkClip = clip;
                else if (clip.name == "idle") idleClip = clip;
            }

            legacyAnimation = gameObject.AddComponent<Animation>();

            if (walkClip != null)
            {
                walkClip.legacy = true;
                legacyAnimation.AddClip(walkClip, walkClip.name);
                hasWalkClip = true;
            }

            if (idleClip != null)
            {
                idleClip.legacy = true;
                legacyAnimation.AddClip(idleClip, idleClip.name);
                hasIdleClip = true;
            }

            legacyAnimation.wrapMode = WrapMode.Loop;
        }

        void PlayWalk()
        {
            if (hasWalkClip) legacyAnimation.Play("walk");
        }

        void PlayIdle()
        {
            if (hasIdleClip) legacyAnimation.Play("idle");
        }

        void Update()
        {
            if (paused)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f)
                {
                    paused = false;
                    PlayWalk();
                }
                return;
            }

            if (Time.time >= nextPauseCheckTime)
            {
                paused = true;
                pauseTimer = Random.Range(2f, 4f);
                nextPauseCheckTime = Time.time + Random.Range(8f, 20f);
                PlayIdle();
                return;
            }

            Vector3 target = waypoints[targetIndex];
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= WaypointArriveDistance)
            {
                targetIndex = (targetIndex + 1) % waypoints.Count;
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * speed * Time.deltaTime;

            Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, TurnDegreesPerSecond * Time.deltaTime);
        }
    }
}
