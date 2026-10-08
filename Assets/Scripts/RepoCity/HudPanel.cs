using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RepoCity {
    public class HudPanel : MonoBehaviour {
        Text titleText;
        Text statsText;
        Text legendText;
        Text narrationHeadingText;
        Text narrationSubheadingText;
        Text narrationBodyText;

        GameObject defaultGroup;
        GameObject narrationGroup;

        public void Initialize(CityLayoutResult layout) {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject canvasGO = new GameObject("HudCanvas");
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(220f, 140f);
            canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasGO.transform.localPosition = Vector3.zero;
            canvasGO.transform.localRotation = Quaternion.identity;

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.10f, 0.22f, 0.82f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            defaultGroup = new GameObject("DefaultGroup");
            defaultGroup.transform.SetParent(canvasGO.transform, false);
            RectTransform defaultGroupRect = defaultGroup.AddComponent<RectTransform>();
            defaultGroupRect.anchorMin = Vector2.zero;
            defaultGroupRect.anchorMax = Vector2.one;
            defaultGroupRect.offsetMin = Vector2.zero;
            defaultGroupRect.offsetMax = Vector2.zero;

            titleText = CreateText(defaultGroup.transform, "Title", font, 16, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -28f), new Vector2(-8f, -4f));
            titleText.text = "REPO CITY";

            statsText = CreateText(defaultGroup.transform, "Stats", font, 11, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -46f), new Vector2(-8f, -30f));
            statsText.text = layout.buildings.Count + " files\n" + layout.districts.Count + " districts";

            legendText = CreateText(defaultGroup.transform, "Legend", font, 10, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(legendText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 6f), new Vector2(-8f, -48f));
            legendText.verticalOverflow = VerticalWrapMode.Overflow;
            legendText.rectTransform.gameObject.AddComponent<RectMask2D>();

            List<BuildingCategory> categories =
                BuildingPalette.CategoriesInUse(layout.buildings.Select(b => b.file)).ToList();

            // Default step is 12 units/category. With more categories than the legend's
            // fixed height can hold at that step, shrink the step (and swatch/font size
            // with it) to fit; RectMask2D above clips anything that still overflows.
            float legendHeight = legendText.rectTransform.rect.height;
            float step = categories.Count > 1 ? Mathf.Min(12f, legendHeight / categories.Count) : 12f;
            float scale = Mathf.Clamp01(step / 12f);
            float swatchSize = 8f * scale;
            legendText.fontSize = Mathf.Max(6, Mathf.RoundToInt(10f * scale));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < categories.Count; i++) {
                BuildingCategory cat = categories[i];

                GameObject swatchGO = new GameObject("Swatch_" + i);
                swatchGO.transform.SetParent(legendText.transform, false);
                Image swatch = swatchGO.AddComponent<Image>();
                swatch.color = cat.color;
                RectTransform swatchRect = swatchGO.GetComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(0f, 1f);
                swatchRect.anchorMax = new Vector2(0f, 1f);
                swatchRect.pivot = new Vector2(0f, 1f);
                swatchRect.anchoredPosition = new Vector2(0f, -i * step);
                swatchRect.sizeDelta = new Vector2(swatchSize, swatchSize);

                sb.Append("   ").Append(cat.label);
                if (i < categories.Count - 1) sb.Append("\n");
            }
            legendText.text = sb.ToString();

            narrationGroup = new GameObject("NarrationGroup");
            narrationGroup.transform.SetParent(canvasGO.transform, false);
            RectTransform narrationGroupRect = narrationGroup.AddComponent<RectTransform>();
            narrationGroupRect.anchorMin = Vector2.zero;
            narrationGroupRect.anchorMax = Vector2.one;
            narrationGroupRect.offsetMin = Vector2.zero;
            narrationGroupRect.offsetMax = Vector2.zero;

            narrationHeadingText = CreateText(narrationGroup.transform, "NarrationHeading", font, 15, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(narrationHeadingText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -26f), new Vector2(-8f, -4f));

            narrationSubheadingText = CreateText(narrationGroup.transform, "NarrationSubheading", font, 9, FontStyle.Italic, TextAnchor.UpperLeft);
            SetRect(narrationSubheadingText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -40f), new Vector2(-8f, -26f));
            narrationSubheadingText.color = new Color(0.8f, 0.8f, 0.9f, 1f);

            narrationBodyText = CreateText(narrationGroup.transform, "NarrationBody", font, 10, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(narrationBodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 6f), new Vector2(-8f, -42f));

            narrationGroup.SetActive(false);
        }

        public void ShowNarration(FileEntry file, string text) {
            defaultGroup.SetActive(false);
            narrationGroup.SetActive(true);
            narrationHeadingText.text = file.name;
            narrationSubheadingText.text = file.path;
            narrationBodyText.text = text;
        }

        public void ShowNpcLine(string personaName, string personaJob, string line) {
            defaultGroup.SetActive(false);
            narrationGroup.SetActive(true);
            narrationHeadingText.text = personaName;
            narrationSubheadingText.text = personaJob;
            narrationBodyText.text = line;
        }

        public void ClearNarration() {
            narrationGroup.SetActive(false);
            defaultGroup.SetActive(true);
        }

        Text CreateText(Transform parent, string name, Font font, int fontSize, FontStyle style, TextAnchor anchor) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
