using System.Collections.Generic;
using UnityEngine;

namespace RepoCity {

public class BuildingInfo : MonoBehaviour {
    public FileEntry file;
}

public static class CityBuilder {
    const float ShortBuildingHeightThreshold = 6f;
    const float SkyscraperHeightThreshold = 14f;

    const float ShortMinScaleY = 0.8f;
    const float ShortMaxScaleY = 3.5f;
    const float StandardMinScaleY = 0.6f;
    const float StandardMaxScaleY = 6f;
    const float SkyscraperMinScaleY = 0.6f;
    const float SkyscraperMaxScaleY = 8f;

    const float ShortXZScaleMin = 1.8f;
    const float ShortXZScaleMax = 2.3f;
    const float StandardXZScaleMin = 3.0f;
    const float StandardXZScaleMax = 3.6f;
    const float SkyscraperXZScaleMin = 1.3f;
    const float SkyscraperXZScaleMax = 1.5f;

    const float GroundMargin = 40f;
    const float UnityPlaneDefaultSize = 10f; // CreatePrimitive(Plane) is 10x10 world units at scale 1

    // Shorter, wider "regular" buildings (natural proportions ~0.9m x 1.3m x 0.9m) - used for
    // the lowest height tier so the skyline isn't just a forest of thin towers.
    static readonly string[] ShortModels = {
        "building-a", "building-b", "building-c", "building-d", "building-e", "building-f",
        "building-g", "building-h", "building-i", "building-j", "building-k", "building-l",
        "building-m", "building-n"
    };

    static readonly string[] StandardModels = {
        "low-detail-building-a", "low-detail-building-b", "low-detail-building-c",
        "low-detail-building-d", "low-detail-building-e", "low-detail-building-f",
        "low-detail-building-g", "low-detail-building-h", "low-detail-building-i",
        "low-detail-building-j", "low-detail-building-k", "low-detail-building-l",
        "low-detail-building-m", "low-detail-building-n",
        "low-detail-building-wide-a", "low-detail-building-wide-b"
    };

    static readonly string[] SkyscraperModels = {
        "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c",
        "building-skyscraper-d", "building-skyscraper-e"
    };

    public static GameObject Build(CityLayoutResult layout) {
        GameObject root = new GameObject("City");

        Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        Dictionary<string, float> actualHeights = new Dictionary<string, float>();

        GameObject buildingsRoot = new GameObject("Buildings");
        buildingsRoot.transform.SetParent(root.transform, false);

        foreach (CityBuilding building in layout.buildings) {
            BuildOneBuilding(building, buildingsRoot.transform, prefabCache, actualHeights);
        }

        Dictionary<string, float> districtMaxHeight = new Dictionary<string, float>();
        foreach (CityBuilding building in layout.buildings) {
            float h;
            if (!actualHeights.TryGetValue(building.id, out h)) continue;
            float current;
            if (!districtMaxHeight.TryGetValue(building.district, out current) || h > current) {
                districtMaxHeight[building.district] = h;
            }
        }

        BuildGround(layout.bounds, root.transform);

        GameObject labelsRoot = new GameObject("DistrictLabels");
        labelsRoot.transform.SetParent(root.transform, false);
        foreach (CityDistrict district in layout.districts) {
            BuildDistrictLabel(district, districtMaxHeight, labelsRoot.transform);
        }

        return root;
    }

    enum HeightTier { Short, Standard, Skyscraper }

    static void BuildOneBuilding(CityBuilding building, Transform parent, Dictionary<string, GameObject> prefabCache, Dictionary<string, float> actualHeights) {
        HeightTier tier = building.height >= SkyscraperHeightThreshold ? HeightTier.Skyscraper
            : building.height >= ShortBuildingHeightThreshold ? HeightTier.Standard
            : HeightTier.Short;
        string[] pool = tier == HeightTier.Skyscraper ? SkyscraperModels
            : tier == HeightTier.Standard ? StandardModels
            : ShortModels;

        uint seed = HashString(building.id);
        System.Func<float> rand = Mulberry32(seed);

        int modelIndex = Mathf.Clamp((int)(rand() * pool.Length), 0, pool.Length - 1);
        string modelName = pool[modelIndex];

        GameObject prefab;
        if (!prefabCache.TryGetValue(modelName, out prefab)) {
            prefab = Resources.Load<GameObject>("RepoCity/Buildings/" + modelName);
            prefabCache[modelName] = prefab;
        }

        if (prefab == null) return; // shouldn't happen, but a missing model should not crash the whole city

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = building.id;

        Renderer renderer = instance.GetComponentInChildren<Renderer>();

        float naturalHeight = 1f;
        float naturalMinY = 0f;
        if (renderer != null) {
            naturalHeight = Mathf.Max(renderer.bounds.size.y, 0.0001f);
            naturalMinY = renderer.bounds.min.y;
        }

        float xzScale = tier == HeightTier.Skyscraper ? RandomRange(rand, SkyscraperXZScaleMin, SkyscraperXZScaleMax)
            : tier == HeightTier.Standard ? RandomRange(rand, StandardXZScaleMin, StandardXZScaleMax)
            : RandomRange(rand, ShortXZScaleMin, ShortXZScaleMax);

        float minScaleY = tier == HeightTier.Skyscraper ? SkyscraperMinScaleY
            : tier == HeightTier.Standard ? StandardMinScaleY : ShortMinScaleY;
        float maxScaleY = tier == HeightTier.Skyscraper ? SkyscraperMaxScaleY
            : tier == HeightTier.Standard ? StandardMaxScaleY : ShortMaxScaleY;
        float scaleY = Mathf.Clamp(building.height / naturalHeight, minScaleY, maxScaleY);

        instance.transform.localScale = new Vector3(xzScale, scaleY, xzScale);

        // the mesh's base sits at naturalMinY*scaleY relative to the pivot once scaled;
        // offset the pivot so the base lands exactly on y = 0.
        float pivotY = -naturalMinY * scaleY;
        instance.transform.localPosition = new Vector3(building.x, pivotY, building.z);

        actualHeights[building.id] = naturalHeight * scaleY;

        if (renderer != null) {
            Color jittered = JitteredColor(BuildingPalette.CategoryForExt(building.file.ext).color, rand);
            Material[] materials = renderer.materials; // per-instance copies; mutating .color here is enough, no reassignment needed
            for (int i = 0; i < materials.Length; i++) {
                materials[i].color = jittered;
            }
        }

        if (instance.GetComponentInChildren<Collider>() == null && renderer != null) {
            Bounds worldBounds = renderer.bounds;
            BoxCollider box = instance.AddComponent<BoxCollider>();
            Vector3 lossyScale = instance.transform.lossyScale;
            box.center = instance.transform.InverseTransformPoint(worldBounds.center);
            box.size = new Vector3(
                worldBounds.size.x / lossyScale.x,
                worldBounds.size.y / lossyScale.y,
                worldBounds.size.z / lossyScale.z
            );
        }

        BuildingInfo info = instance.AddComponent<BuildingInfo>();
        info.file = building.file;
    }

    static void BuildGround(CityBounds bounds, Transform parent) {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent, false);

        float sizeX = (bounds.maxX - bounds.minX) + GroundMargin * 2f;
        float sizeZ = (bounds.maxZ - bounds.minZ) + GroundMargin * 2f;
        float centerX = (bounds.minX + bounds.maxX) * 0.5f;
        float centerZ = (bounds.minZ + bounds.maxZ) * 0.5f;

        ground.transform.localPosition = new Vector3(centerX, 0f, centerZ);
        ground.transform.localScale = new Vector3(sizeX / UnityPlaneDefaultSize, 1f, sizeZ / UnityPlaneDefaultSize);

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        groundRenderer.material.color = new Color(0.12f, 0.12f, 0.13f);
    }

    static void BuildDistrictLabel(CityDistrict district, Dictionary<string, float> districtMaxHeight, Transform parent) {
        float tallest;
        districtMaxHeight.TryGetValue(district.id, out tallest); // 0f if district has no buildings, same as before

        float labelY = tallest + 4f;

        GameObject labelObj = new GameObject("DistrictLabel_" + district.id);
        labelObj.transform.SetParent(parent, false);
        labelObj.transform.localPosition = new Vector3(district.x, labelY, district.z);

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = district.label.ToUpperInvariant();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.3f;
        textMesh.color = Color.white;

        labelObj.AddComponent<FaceViewer>();
    }

    // Nudges hue/saturation/value per-building so a category isn't one flat color block repeated
    // across the skyline; same building.id always gets the same nudge (seeded rand).
    static Color JitteredColor(Color baseColor, System.Func<float> rand) {
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        h += (rand() - 0.5f) * 0.04f;
        s = Mathf.Clamp01(s * RandomRange(rand, 0.85f, 1.1f));
        v = Mathf.Clamp01(v * RandomRange(rand, 0.9f, 1.2f));
        Color result = Color.HSVToRGB(((h % 1f) + 1f) % 1f, s, v);
        result.a = baseColor.a;
        return result;
    }

    static float RandomRange(System.Func<float> rand, float min, float max) {
        return min + rand() * (max - min);
    }

    static uint HashString(string s) {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return h;
    }

    static System.Func<float> Mulberry32(uint seed) {
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
