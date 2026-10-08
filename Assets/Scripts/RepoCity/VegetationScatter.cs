using System.Collections.Generic;
using UnityEngine;

namespace RepoCity
{
    public static class VegetationScatter
    {
        const float BandInner = 14f; // must stay clear of the sibling road/sidewalk ring (ends ~11m out)
        const float BandOuter = 30f;
        const float SlotSpacingDivisor = 2.2f; // tuned so a typical ~40x30 bounds lands in the 40-80 instance range
        const int MinSlots = 40;
        const int MaxSlots = 80;

        static readonly string[] TreeModels = { "tree_default", "tree_detailed", "tree_fat", "tree_cone" };
        static readonly string[] BushModels = { "plant_bush", "plant_bushLarge" };

        public static GameObject Scatter(CityLayoutResult layout)
        {
            GameObject root = new GameObject("Vegetation");
            Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

            CityBounds b = layout.bounds;
            float width = b.maxX - b.minX;
            float depth = b.maxZ - b.minZ;
            float perimeter = 2f * (width + depth);

            int slotCount = Mathf.Clamp(Mathf.RoundToInt(perimeter / SlotSpacingDivisor), MinSlots, MaxSlots);
            float slotStep = perimeter / slotCount;

            for (int i = 0; i < slotCount; i++)
            {
                System.Func<float> rng = Mulberry32(HashString("veg" + i));

                float t = (i + (rng() - 0.5f)) * slotStep;
                if (t < 0f) t += perimeter;
                if (t >= perimeter) t -= perimeter;

                Vector3 point, normal, tangent;
                GetEdgePointAndNormal(t, b, out point, out normal, out tangent);

                float radialOffset = Mathf.Lerp(BandInner, BandOuter, rng());
                float tangentJitter = (rng() - 0.5f) * slotStep * 0.6f;
                Vector3 position = point + normal * radialOffset + tangent * tangentJitter;

                float rotY = rng() * 360f;
                float scale = Mathf.Lerp(0.8f, 1.3f, rng());

                bool isTree = rng() < (2f / 3f);
                string[] group = isTree ? TreeModels : BushModels;
                string modelName = group[Mathf.Min(group.Length - 1, Mathf.FloorToInt(rng() * group.Length))];

                GameObject prefab = GetPrefab(modelName, prefabCache);
                if (prefab == null) continue;

                GameObject instance = Object.Instantiate(prefab, position, Quaternion.Euler(0f, rotY, 0f), root.transform);
                instance.transform.localScale = Vector3.one * scale;
                instance.name = modelName;
            }

            return root;
        }

        // Walks the bounds rectangle perimeter starting at (minX,minZ), clockwise: south, east, north, west.
        static void GetEdgePointAndNormal(float t, CityBounds b, out Vector3 point, out Vector3 normal, out Vector3 tangent)
        {
            float width = b.maxX - b.minX;
            float depth = b.maxZ - b.minZ;

            if (t < width)
            {
                point = new Vector3(b.minX + t, 0f, b.minZ);
                normal = new Vector3(0f, 0f, -1f);
                tangent = new Vector3(1f, 0f, 0f);
                return;
            }
            t -= width;

            if (t < depth)
            {
                point = new Vector3(b.maxX, 0f, b.minZ + t);
                normal = new Vector3(1f, 0f, 0f);
                tangent = new Vector3(0f, 0f, 1f);
                return;
            }
            t -= depth;

            if (t < width)
            {
                point = new Vector3(b.maxX - t, 0f, b.maxZ);
                normal = new Vector3(0f, 0f, 1f);
                tangent = new Vector3(-1f, 0f, 0f);
                return;
            }
            t -= width;

            point = new Vector3(b.minX, 0f, b.maxZ - t);
            normal = new Vector3(-1f, 0f, 0f);
            tangent = new Vector3(0f, 0f, -1f);
        }

        static GameObject GetPrefab(string name, Dictionary<string, GameObject> cache)
        {
            GameObject prefab;
            if (cache.TryGetValue(name, out prefab)) return prefab;
            prefab = Resources.Load<GameObject>("RepoCity/Vegetation/" + name);
            cache[name] = prefab;
            return prefab;
        }

        static uint HashString(string s) { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return h; }
        static System.Func<float> Mulberry32(uint seed)
        {
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
