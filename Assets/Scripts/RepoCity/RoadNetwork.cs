using System.Collections.Generic;
using UnityEngine;

namespace RepoCity
{
    public class RoadNetworkResult
    {
        public GameObject root;
        public List<Vector3> carLoopWaypoints;
        public List<Vector3> sidewalkLoopWaypoints;
    }

    public static class RoadNetwork
    {
        const float RoadMargin = 4f;       // outward from layout.bounds to the road centerline rectangle
        const float SidewalkMargin = 1.5f; // additional outward offset, beyond the road rectangle
        const float SidewalkSpacing = 2.5f;

        public static RoadNetworkResult Build(CityLayoutResult layout)
        {
            CityBounds b = layout.bounds;
            float roadMinX = b.minX - RoadMargin;
            float roadMaxX = b.maxX + RoadMargin;
            float roadMinZ = b.minZ - RoadMargin;
            float roadMaxZ = b.maxZ + RoadMargin;

            GameObject root = new GameObject("RoadNetwork");

            GameObject straightPrefab = Resources.Load<GameObject>("RepoCity/Roads/road-straight");
            GameObject bendPrefab = Resources.Load<GameObject>("RepoCity/Roads/road-bend");

            // Corners first. Rotated in 90-degree steps going the same way the car loop below travels,
            // so the ring reads as continuous even though exact bend facing isn't pixel-matched.
            PlaceBend(bendPrefab, new Vector3(roadMinX, 0f, roadMinZ), 0f, root.transform);
            PlaceBend(bendPrefab, new Vector3(roadMaxX, 0f, roadMinZ), 90f, root.transform);
            PlaceBend(bendPrefab, new Vector3(roadMaxX, 0f, roadMaxZ), 180f, root.transform);
            PlaceBend(bendPrefab, new Vector3(roadMinX, 0f, roadMaxZ), 270f, root.transform);

            List<Vector3> carWaypoints = new List<Vector3>();

            // Straight runs between corners, in loop order. alongX edges get yRotation 90 so the
            // (assumed Z-aligned) straight mesh turns to run along X instead.
            PlaceStraightRun(straightPrefab, root.transform, carWaypoints, roadMinX + 1f, roadMaxX - 1f, true, roadMinZ, 90f);
            PlaceStraightRun(straightPrefab, root.transform, carWaypoints, roadMinZ + 1f, roadMaxZ - 1f, false, roadMaxX, 0f);
            PlaceStraightRun(straightPrefab, root.transform, carWaypoints, roadMaxX - 1f, roadMinX + 1f, true, roadMaxZ, 90f);
            PlaceStraightRun(straightPrefab, root.transform, carWaypoints, roadMaxZ - 1f, roadMinZ + 1f, false, roadMinX, 0f);

            List<Vector3> sidewalkWaypoints = BuildSidewalkLoop(
                roadMinX - SidewalkMargin, roadMaxX + SidewalkMargin,
                roadMinZ - SidewalkMargin, roadMaxZ + SidewalkMargin);

            return new RoadNetworkResult
            {
                root = root,
                carLoopWaypoints = carWaypoints,
                sidewalkLoopWaypoints = sidewalkWaypoints
            };
        }

        // Places 1m-spaced straight tiles walking from `from` to `to` along X (alongX) or Z, at the
        // fixed cross-axis coordinate `fixedCoord`. `from` may be greater than `to` (walking backward).
        static void PlaceStraightRun(GameObject prefab, Transform parent, List<Vector3> waypoints,
            float from, float to, bool alongX, float fixedCoord, float yRotation)
        {
            float dir = to >= from ? 1f : -1f;
            float length = Mathf.Abs(to - from);
            int count = Mathf.Max(0, Mathf.FloorToInt(length + 0.001f) + 1);
            Quaternion rot = Quaternion.Euler(0f, yRotation, 0f);

            for (int i = 0; i < count; i++)
            {
                float t = from + dir * i;
                Vector3 pos = alongX ? new Vector3(t, 0f, fixedCoord) : new Vector3(fixedCoord, 0f, t);
                Spawn(prefab, pos, rot, parent);
                waypoints.Add(pos);
            }
        }

        // Lays a straight line of 1m road tiles between two axis-aligned points (same X or same Z),
        // for connecting separate city islands in a multi-repo world. Returns the waypoint list in
        // order from `from` to `to`, in case a caller wants cars/NPCs to travel the connector too.
        public static List<Vector3> BuildConnector(Vector3 from, Vector3 to, Transform parent)
        {
            GameObject straightPrefab = Resources.Load<GameObject>("RepoCity/Roads/road-straight");
            List<Vector3> waypoints = new List<Vector3>();

            bool alongX = Mathf.Abs(to.x - from.x) >= Mathf.Abs(to.z - from.z);
            float yRotation = alongX ? 90f : 0f;

            if (alongX)
            {
                PlaceStraightRun(straightPrefab, parent, waypoints, from.x, to.x, true, from.z, yRotation);
            }
            else
            {
                PlaceStraightRun(straightPrefab, parent, waypoints, from.z, to.z, false, from.x, yRotation);
            }

            return waypoints;
        }

        static void PlaceBend(GameObject prefab, Vector3 pos, float yRotation, Transform parent)
        {
            Spawn(prefab, pos, Quaternion.Euler(0f, yRotation, 0f), parent);
        }

        static void Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            GameObject go = Object.Instantiate(prefab, pos, rot, parent);
            MeshRenderer mr = go.GetComponentInChildren<MeshRenderer>();
            if (mr != null) mr.receiveShadows = true;
        }

        static List<Vector3> BuildSidewalkLoop(float minX, float maxX, float minZ, float maxZ)
        {
            List<Vector3> points = new List<Vector3>();
            AppendEdge(points, new Vector3(minX, 0f, minZ), new Vector3(maxX, 0f, minZ));
            AppendEdge(points, new Vector3(maxX, 0f, minZ), new Vector3(maxX, 0f, maxZ));
            AppendEdge(points, new Vector3(maxX, 0f, maxZ), new Vector3(minX, 0f, maxZ));
            AppendEdge(points, new Vector3(minX, 0f, maxZ), new Vector3(minX, 0f, minZ));
            return points;
        }

        // Adds evenly spaced points covering [start, end) - the end corner is left for the next edge's
        // call to supply, so the closed loop has no duplicate corner points.
        static void AppendEdge(List<Vector3> points, Vector3 start, Vector3 end)
        {
            float length = Vector3.Distance(start, end);
            int segments = Mathf.Max(1, Mathf.RoundToInt(length / SidewalkSpacing));
            for (int i = 0; i < segments; i++)
            {
                points.Add(Vector3.Lerp(start, end, (float)i / segments));
            }
        }
    }
}
