using System.Collections.Generic;
using UnityEngine;

namespace RepoCity
{
    [System.Serializable]
    public class CityDistrict
    {
        public string id;
        public string path;
        public string label;
        public float x;
        public float z;
        public float width;
        public float depth;
        public int buildingCount;
    }

    public class CityBuilding
    {
        public string id;
        public FileEntry file;
        public float x, z;
        public float width, depth;
        public float height;
        public string district;
    }

    public struct CityBounds
    {
        public float minX, maxX, minZ, maxZ;
    }

    public class CityLayoutResult
    {
        public List<CityDistrict> districts;
        public List<CityBuilding> buildings;
        public CityBounds bounds;
    }

    public static class CityLayoutBuilder
    {
        const float DistrictGap = 3f;
        const float BuildingGap = 0.6f;
        const float BuildingSize = 2.2f;

        public static CityLayoutResult Compute(RepoData data)
        {
            // 1. Group files by top-level directory segment ("" -> its own "root" district).
            List<string> districtKeys = new List<string>();
            Dictionary<string, List<FileEntry>> grouped = new Dictionary<string, List<FileEntry>>();
            bool hasRoot = false;

            foreach (FileEntry file in data.files)
            {
                string key;
                if (string.IsNullOrEmpty(file.dir))
                {
                    key = "root";
                    hasRoot = true;
                }
                else
                {
                    int slash = file.dir.IndexOf('/');
                    key = slash >= 0 ? file.dir.Substring(0, slash) : file.dir;
                }

                List<FileEntry> list;
                if (!grouped.TryGetValue(key, out list))
                {
                    list = new List<FileEntry>();
                    grouped[key] = list;
                    districtKeys.Add(key);
                }
                list.Add(file);
            }

            // deterministic ordering: root first (if present), everything else in first-seen order.
            if (hasRoot)
            {
                districtKeys.Remove("root");
                districtKeys.Insert(0, "root");
            }

            int districtCount = districtKeys.Count;
            List<CityDistrict> districts = new List<CityDistrict>(districtCount);
            List<CityBuilding> buildings = new List<CityBuilding>(data.files.Count);

            // per-district footprint + sub-grid column count, needed again when we place buildings below
            float[] districtWidths = new float[districtCount];
            float[] districtDepths = new float[districtCount];
            int[] districtCols = new int[districtCount];

            // 2a. Size each district's own footprint from its building sub-grid.
            for (int d = 0; d < districtCount; d++)
            {
                List<FileEntry> files = grouped[districtKeys[d]];
                int count = files.Count;
                int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count)));
                int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / cols));

                float width = cols * BuildingSize + (cols - 1) * BuildingGap;
                float depth = rows * BuildingSize + (rows - 1) * BuildingGap;

                districtCols[d] = cols;
                districtWidths[d] = width;
                districtDepths[d] = depth;

                districts.Add(new CityDistrict
                {
                    id = districtKeys[d],
                    path = districtKeys[d],
                    label = districtKeys[d],
                    width = width,
                    depth = depth,
                    buildingCount = count
                });
            }

            // 2b. Arrange districts on a grid, sizing each row/column by its largest member so nothing overlaps.
            int gridCols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(districtCount)));
            int gridRows = Mathf.Max(1, Mathf.CeilToInt((float)districtCount / gridCols));

            float[] colWidths = new float[gridCols];
            float[] rowDepths = new float[gridRows];

            for (int d = 0; d < districtCount; d++)
            {
                int col = d % gridCols;
                int row = d / gridCols;
                if (districtWidths[d] > colWidths[col]) colWidths[col] = districtWidths[d];
                if (districtDepths[d] > rowDepths[row]) rowDepths[row] = districtDepths[d];
            }

            float[] colLeftX = new float[gridCols];
            for (int c = 1; c < gridCols; c++)
                colLeftX[c] = colLeftX[c - 1] + colWidths[c - 1] + DistrictGap;

            float[] rowTopZ = new float[gridRows];
            for (int r = 1; r < gridRows; r++)
                rowTopZ[r] = rowTopZ[r - 1] + rowDepths[r - 1] + DistrictGap;

            for (int d = 0; d < districtCount; d++)
            {
                int col = d % gridCols;
                int row = d / gridCols;
                CityDistrict district = districts[d];
                district.x = colLeftX[col] + colWidths[col] * 0.5f;
                district.z = rowTopZ[row] + rowDepths[row] * 0.5f;
            }

            // 3. Place buildings inside their own district's sub-grid, centered on the district.
            for (int d = 0; d < districtCount; d++)
            {
                CityDistrict district = districts[d];
                List<FileEntry> files = grouped[districtKeys[d]];
                int cols = districtCols[d];
                float width = districtWidths[d];
                float depth = districtDepths[d];

                for (int i = 0; i < files.Count; i++)
                {
                    FileEntry file = files[i];
                    int col = i % cols;
                    int row = i / cols;

                    float localX = -width * 0.5f + BuildingSize * 0.5f + col * (BuildingSize + BuildingGap);
                    float localZ = -depth * 0.5f + BuildingSize * 0.5f + row * (BuildingSize + BuildingGap);

                    float height = Mathf.Clamp(2f + Mathf.Log10(file.size + 1) * 4f + Mathf.Log(file.commits + 1) * 3f, 2f, 40f);

                    buildings.Add(new CityBuilding
                    {
                        id = file.path,
                        file = file,
                        x = district.x + localX,
                        z = district.z + localZ,
                        width = BuildingSize,
                        depth = BuildingSize,
                        height = height,
                        district = district.id
                    });
                }
            }

            // 4. Center the whole arrangement so bounds end up symmetric around world (0,0) on X/Z.
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            for (int d = 0; d < districtCount; d++)
            {
                CityDistrict district = districts[d];
                float left = district.x - district.width * 0.5f;
                float right = district.x + district.width * 0.5f;
                float near = district.z - district.depth * 0.5f;
                float far = district.z + district.depth * 0.5f;
                if (left < minX) minX = left;
                if (right > maxX) maxX = right;
                if (near < minZ) minZ = near;
                if (far > maxZ) maxZ = far;
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;

            foreach (CityDistrict district in districts)
            {
                district.x -= centerX;
                district.z -= centerZ;
            }
            foreach (CityBuilding building in buildings)
            {
                building.x -= centerX;
                building.z -= centerZ;
            }

            CityBounds bounds = new CityBounds
            {
                minX = minX - centerX,
                maxX = maxX - centerX,
                minZ = minZ - centerZ,
                maxZ = maxZ - centerZ
            };

            return new CityLayoutResult
            {
                districts = districts,
                buildings = buildings,
                bounds = bounds
            };
        }
    }
}
