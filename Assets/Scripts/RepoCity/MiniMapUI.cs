using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RepoCity {
    public class MiniMapUI : MonoBehaviour {
        const float Margin = 20f;
        const float CanvasWidth = 220f;
        const float CanvasHeight = 160f;

        static readonly Color[] IslandHues = {
            new Color(0.30f, 0.55f, 0.95f, 0.65f),
            new Color(0.95f, 0.55f, 0.25f, 0.65f),
            new Color(0.40f, 0.85f, 0.45f, 0.65f),
            new Color(0.85f, 0.35f, 0.75f, 0.65f),
            new Color(0.90f, 0.85f, 0.25f, 0.65f)
        };

        Transform playerRig;
        RectTransform playerMarker;

        float worldMinX, worldMinZ;
        float scaleX, scaleZ;

        public void Initialize(List<WorldIslandResult> islands, Transform playerRig) {
            this.playerRig = playerRig;

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject canvasGO = new GameObject("MiniMapCanvas");
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            canvasGO.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
            canvasGO.transform.localPosition = Vector3.zero;
            canvasGO.transform.localRotation = Quaternion.identity;

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // One combined world-space rect across every island, plus a flat margin.
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            for (int i = 0; i < islands.Count; i++) {
                WorldIslandResult island = islands[i];
                CityBounds b = island.layout.bounds;
                float ix0 = island.worldCenter.x + b.minX;
                float ix1 = island.worldCenter.x + b.maxX;
                float iz0 = island.worldCenter.z + b.minZ;
                float iz1 = island.worldCenter.z + b.maxZ;

                if (ix0 < minX) minX = ix0;
                if (ix1 > maxX) maxX = ix1;
                if (iz0 < minZ) minZ = iz0;
                if (iz1 > maxZ) maxZ = iz1;
            }

            if (islands.Count == 0) {
                minX = -1f; maxX = 1f; minZ = -1f; maxZ = 1f;
            }

            minX -= Margin; maxX += Margin;
            minZ -= Margin; maxZ += Margin;

            worldMinX = minX;
            worldMinZ = minZ;

            float worldWidth = maxX - minX;
            float worldDepth = maxZ - minZ;

            // world X -> UI X, world Z -> UI Y (flipped so world-forward (+Z) reads as "up" on the map).
            scaleX = worldWidth > 0.01f ? CanvasWidth / worldWidth : 1f;
            scaleZ = worldDepth > 0.01f ? CanvasHeight / worldDepth : 1f;

            for (int i = 0; i < islands.Count; i++) {
                WorldIslandResult island = islands[i];
                CityBounds b = island.layout.bounds;
                float ix0 = island.worldCenter.x + b.minX;
                float ix1 = island.worldCenter.x + b.maxX;
                float iz0 = island.worldCenter.z + b.minZ;
                float iz1 = island.worldCenter.z + b.maxZ;

                Vector2 uiMin = WorldToUi(ix0, iz0);
                Vector2 uiMax = WorldToUi(ix1, iz1);

                float uiLeft = Mathf.Min(uiMin.x, uiMax.x);
                float uiRight = Mathf.Max(uiMin.x, uiMax.x);
                float uiBottom = Mathf.Min(uiMin.y, uiMax.y);
                float uiTop = Mathf.Max(uiMin.y, uiMax.y);

                GameObject islandGO = new GameObject("Island_" + island.config.label);
                islandGO.transform.SetParent(canvasGO.transform, false);
                Image islandImg = islandGO.AddComponent<Image>();
                islandImg.color = IslandHues[i % IslandHues.Length];
                RectTransform islandRect = islandGO.GetComponent<RectTransform>();
                islandRect.anchorMin = Vector2.zero;
                islandRect.anchorMax = Vector2.zero;
                islandRect.pivot = new Vector2(0f, 0f);
                islandRect.anchoredPosition = new Vector2(uiLeft, uiBottom);
                islandRect.sizeDelta = new Vector2(Mathf.Max(uiRight - uiLeft, 4f), Mathf.Max(uiTop - uiBottom, 4f));

                Text label = CreateText(islandGO.transform, "Label", font, 9, TextAnchor.MiddleCenter);
                label.text = island.config.label;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            GameObject playerGO = new GameObject("PlayerMarker");
            playerGO.transform.SetParent(canvasGO.transform, false);
            Image playerImg = playerGO.AddComponent<Image>();
            playerImg.color = new Color(1f, 0.95f, 0.2f, 1f);
            playerMarker = playerGO.GetComponent<RectTransform>();
            playerMarker.anchorMin = new Vector2(0f, 0f);
            playerMarker.anchorMax = new Vector2(0f, 0f);
            playerMarker.pivot = new Vector2(0.5f, 0.5f);
            playerMarker.sizeDelta = new Vector2(8f, 8f);

            UpdatePlayerMarker();
        }

        void Update() {
            UpdatePlayerMarker();
        }

        void UpdatePlayerMarker() {
            if (playerMarker == null || playerRig == null) return;
            Vector3 pos = playerRig.position;
            Vector2 uiPos = WorldToUi(pos.x, pos.z);
            playerMarker.anchoredPosition = uiPos;
        }

        Vector2 WorldToUi(float worldX, float worldZ) {
            return new Vector2((worldX - worldMinX) * scaleX, (worldZ - worldMinZ) * scaleZ);
        }

        Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor anchor) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
