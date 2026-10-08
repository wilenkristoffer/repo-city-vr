using System.Collections.Generic;
using UnityEngine;

namespace RepoCity {

    public static class TrafficSpawner {
        static readonly string[] ModelNames = { "sedan", "suv", "taxi", "van", "police" };
        static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();

        static GameObject LoadModel(string name) {
            if (!PrefabCache.TryGetValue(name, out var prefab)) {
                prefab = Resources.Load<GameObject>("RepoCity/Cars/" + name);
                PrefabCache[name] = prefab;
            }
            return prefab;
        }

        public static void Spawn(Transform parent, List<Vector3> carLoopWaypoints, int carCount) {
            if (carLoopWaypoints == null || carLoopWaypoints.Count < 2 || carCount <= 0) return;

            for (int i = 0; i < carCount; i++) {
                string modelName = ModelNames[Random.Range(0, ModelNames.Length)];
                GameObject prefab = LoadModel(modelName);
                if (prefab == null) continue;

                int startIndex = i * carLoopWaypoints.Count / carCount;
                Vector3 startPos = carLoopWaypoints[startIndex];

                GameObject car = Object.Instantiate(prefab, startPos, Quaternion.identity, parent);
                car.name = modelName + "_" + i;

                float speed = Random.Range(4f, 7f);
                CarFollower follower = car.AddComponent<CarFollower>();
                follower.Initialize(carLoopWaypoints, startIndex, speed);
            }
        }
    }

    public class CarFollower : MonoBehaviour {
        List<Vector3> waypoints;
        int targetIndex;
        float speed;
        const float ArrivalThreshold = 0.5f;

        public void Initialize(List<Vector3> loopWaypoints, int startIndex, float moveSpeed) {
            waypoints = loopWaypoints;
            speed = moveSpeed;
            targetIndex = (startIndex + 1) % waypoints.Count;
            transform.position = waypoints[startIndex];
        }

        void Update() {
            if (waypoints == null || waypoints.Count < 2) return;

            Vector3 target = waypoints[targetIndex];
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= ArrivalThreshold) {
                targetIndex = (targetIndex + 1) % waypoints.Count;
                target = waypoints[targetIndex];
                toTarget = target - transform.position;
                toTarget.y = 0f;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * speed * Time.deltaTime;

            if (direction.sqrMagnitude > 0.0001f) {
                Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
            }
        }
    }
}
