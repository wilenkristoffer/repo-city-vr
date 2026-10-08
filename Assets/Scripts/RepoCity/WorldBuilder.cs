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

                    TrafficSpawner.Spawn(islandRoot.transform, roads.carLoopWaypoints, CarCount);
                    NpcSpawner.Spawn(islandRoot.transform, roads.sidewalkLoopWaypoints, NpcCount);

                    // Everything above was built/spawned while islandRoot sat at local (0,0,0), since
                    // TrafficSpawner/NpcSpawner position their spawns with the world-space Transform.position
                    // setter - only shifting the parent now carries them correctly to worldCenter.
                    islandRoot.transform.localPosition = worldCenter;

                    List<Vector3> carWaypointsWorld = roads.carLoopWaypoints.ConvertAll(p => p + worldCenter);
                    List<Vector3> sidewalkWaypointsWorld = roads.sidewalkLoopWaypoints.ConvertAll(p => p + worldCenter);

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
