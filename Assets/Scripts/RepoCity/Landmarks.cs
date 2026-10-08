using UnityEngine;

namespace RepoCity {

public static class Landmarks {
    const float BeaconWorldHeight = 4f;
    const float BeaconLocalXZScale = 0.4f;
    const float LabelLocalGap = 0.6f;

    static readonly Color MostActiveColor = new Color(1f, 0.85f, 0.2f);
    static readonly Color OldestColor = new Color(0.3f, 0.8f, 1f);

    public static void Highlight(GameObject islandRoot) {
        BuildingInfo[] buildings = islandRoot.GetComponentsInChildren<BuildingInfo>();
        if (buildings.Length == 0) return;

        BuildingInfo mostActive = null;
        BuildingInfo oldest = null;
        System.DateTime oldestDate = System.DateTime.MaxValue;

        foreach (BuildingInfo info in buildings) {
            if (info.file == null) continue;

            if (mostActive == null || info.file.commits > mostActive.file.commits) {
                mostActive = info;
            }

            System.DateTime parsed;
            if (!string.IsNullOrEmpty(info.file.lastModified) && System.DateTime.TryParse(info.file.lastModified, out parsed)) {
                if (oldest == null || parsed < oldestDate) {
                    oldest = info;
                    oldestDate = parsed;
                }
            }
        }

        if (mostActive == null && oldest == null) return;

        if (mostActive != null && mostActive == oldest) {
            // same building wins both categories - stack the second beacon on top of the first
            float stackTopLocalY = SpawnBeacon(mostActive.transform, MostActiveColor, "MOST ACTIVE FILE", null);
            SpawnBeacon(oldest.transform, OldestColor, "OLDEST FILE", stackTopLocalY);
            return;
        }

        if (mostActive != null) SpawnBeacon(mostActive.transform, MostActiveColor, "MOST ACTIVE FILE", null);
        if (oldest != null) SpawnBeacon(oldest.transform, OldestColor, "OLDEST FILE", null);
    }

    // Returns this beacon's own top in buildingTransform-local space, so a second beacon on the
    // same building can be stacked on top of it instead of overlapping.
    static float SpawnBeacon(Transform buildingTransform, Color color, string label, float? baseLocalYOverride) {
        float baseLocalY = baseLocalYOverride.HasValue ? baseLocalYOverride.Value : BuildingTopLocalY(buildingTransform);

        float parentScaleY = Mathf.Max(buildingTransform.lossyScale.y, 0.0001f);
        float beaconLocalHalfHeight = BeaconWorldHeight / (2f * parentScaleY); // cylinder primitive spans -1..1 at scale 1

        Vector3 beaconLocalPos = new Vector3(0f, baseLocalY + beaconLocalHalfHeight, 0f);

        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "Beacon_" + label.Replace(" ", "");
        beacon.transform.SetParent(buildingTransform, false);
        beacon.transform.localPosition = beaconLocalPos;
        beacon.transform.localRotation = Quaternion.identity;
        beacon.transform.localScale = new Vector3(BeaconLocalXZScale, beaconLocalHalfHeight, BeaconLocalXZScale);

        Collider beaconCollider = beacon.GetComponent<Collider>();
        if (beaconCollider != null) Object.Destroy(beaconCollider); // must not block the building's own inspect-raycast

        Renderer beaconRenderer = beacon.GetComponent<Renderer>();
        if (beaconRenderer != null) beaconRenderer.material.color = color;

        BuildLabel(buildingTransform, beaconLocalPos, label);

        return beaconLocalPos.y + beaconLocalHalfHeight;
    }

    static float BuildingTopLocalY(Transform buildingTransform) {
        Renderer buildingRenderer = buildingTransform.GetComponentInChildren<Renderer>();
        if (buildingRenderer == null) return 2f;

        Vector3 topWorld = new Vector3(buildingRenderer.bounds.center.x, buildingRenderer.bounds.max.y, buildingRenderer.bounds.center.z);
        return buildingTransform.InverseTransformPoint(topWorld).y;
    }

    static void BuildLabel(Transform buildingTransform, Vector3 beaconLocalPos, string label) {
        // counter-scale so the text isn't stretched/squashed by the building's own (often
        // non-uniform) scale, which this label inherits as a child of buildingTransform
        Vector3 lossyScale = buildingTransform.lossyScale;
        Vector3 counterScale = new Vector3(
            1f / Mathf.Max(lossyScale.x, 0.0001f),
            1f / Mathf.Max(lossyScale.y, 0.0001f),
            1f / Mathf.Max(lossyScale.z, 0.0001f));

        GameObject labelObj = new GameObject("Label_" + label.Replace(" ", ""));
        labelObj.transform.SetParent(buildingTransform, false);
        labelObj.transform.localPosition = beaconLocalPos + new Vector3(0f, LabelLocalGap, 0f);
        labelObj.transform.localScale = counterScale;

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = label;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.3f;
        textMesh.color = Color.white;

        labelObj.transform.LookAt(buildingTransform.TransformPoint(beaconLocalPos + Vector3.back * 10f));
        labelObj.transform.Rotate(0f, 180f, 0f); // TextMesh's face is on the opposite side of its forward axis
    }
}

}
