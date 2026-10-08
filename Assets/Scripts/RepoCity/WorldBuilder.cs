using System.Collections.Generic;
using UnityEngine;

namespace RepoCity {

    public class WorldIslandResult {
        public WorldIslandConfig config;
        public GameObject root;
        public CityLayoutResult layout;
        public Vector3 worldCenter;
        public List<Vector3> carLoopWaypoints;      // WORLD space
        public List<Vector3> sidewalkLoopWaypoints; // WORLD space
    }

    public static class WorldBuilder {
        const int CarCount = 6;
        const int NpcCount = 10;
        const float IslandGap = 30f;

        public static List<WorldIslandResult> BuildAll(Transform parent) {
            List<WorldIslandResult> results = new List<WorldIslandResult>();
            float cursorX = 0f;
            bool isFirst = true;

            foreach (WorldIslandConfig config in WorldConfig.Islands) {
                try {
                    RepoData data = RepoDataLoader.Load(config.resourcePath);
                    CityLayoutResult layout = CityLayoutBuilder.Compute(data);

                    float halfWidth = (layout.bounds.maxX - layout.bounds.minX) * 0.5f + 50f;

                    Vector3 worldCenter;
                    if (isFirst) {
                        worldCenter = Vector3.zero;
                        cursorX = halfWidth;
                        isFirst = false;
                    } else {
                        cursorX += IslandGap;
                        worldCenter = new Vector3(cursorX + halfWidth, 0f, 0f);
                        cursorX += halfWidth;
                    }

                    GameObject islandRoot = new GameObject("Island_" + config.label);
                    islandRoot.transform.SetParent(parent, false);

                    GameObject cityRoot = CityBuilder.Build(layout);
                    cityRoot.transform.SetParent(islandRoot.transform, false);

                    RoadNetworkResult roads = RoadNetwork.Build(layout);
                    roads.root.transform.SetParent(islandRoot.transform, false);

                    GameObject vegetationRoot = VegetationScatter.Scatter(layout);
                    vegetationRoot.transform.SetParent(islandRoot.transform, false);

                    // City/roads/vegetation above were built/parented while islandRoot sat at local
                    // (0,0,0), so shifting the parent now carries them correctly to worldCenter.
                    islandRoot.transform.localPosition = worldCenter;

                    List<Vector3> carWaypointsWorld = roads.carLoopWaypoints.ConvertAll(p => p + worldCenter);
                    List<Vector3> sidewalkWaypointsWorld = roads.sidewalkLoopWaypoints.ConvertAll(p => p + worldCenter);

                    // Traffic/NPCs are spawned AFTER the shift, and given the already-WORLD-space
                    // waypoint lists, because CarFollower/NpcWalker cache the list by reference and
                    // chase it every frame against transform.position - if they were spawned (and
                    // handed the pre-shift local list) before the parent moved, the GameObjects would
                    // still end up in the right place (parenting carries them along), but their cached
                    // waypoints would forever be offset by -worldCenter from where transform.position
                    // actually is, so they'd drive/walk straight off toward the origin instead of
                    // looping their own ring road.
                    TrafficSpawner.Spawn(islandRoot.transform, carWaypointsWorld, CarCount);
                    NpcSpawner.Spawn(islandRoot.transform, sidewalkWaypointsWorld, NpcCount);

                    WorldIslandResult result = new WorldIslandResult {
                        config = config,
                        root = islandRoot,
                        layout = layout,
                        worldCenter = worldCenter,
                        carLoopWaypoints = carWaypointsWorld,
                        sidewalkLoopWaypoints = sidewalkWaypointsWorld
                    };
                    results.Add(result);

                    Debug.Log("WorldBuilder: built island '" + config.label + "' with " +
                        layout.buildings.Count + " buildings at worldCenter " + worldCenter);
                } catch (System.Exception e) {
                    Debug.LogWarning("WorldBuilder: failed to build island '" + config.label + "' (" +
                        config.resourcePath + "), skipping. " + e);
                }
            }

            return results;
        }
    }
}
