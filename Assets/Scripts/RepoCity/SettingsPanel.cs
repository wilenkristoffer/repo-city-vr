using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RepoCity {
    public class SettingsPanel : MonoBehaviour {
        const float RowHeight = 64f;

        public void Initialize(System.Action onClose) {
            AudioManager.EnsureInitialized();
            GameSettings.EnsureLoaded();

            if (Object.FindObjectOfType<EventSystem>() == null) {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject canvasGO = new GameObject("SettingsCanvas");
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(480f, 620f);
            canvasGO.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            canvasGO.transform.localPosition = Vector3.zero;
            canvasGO.transform.localRotation = Quaternion.identity;

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            SetRect(bgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text title = CreateText(canvasGO.transform, "Title", font, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -60f), new Vector2(-10f, -10f));
            title.text = "SETTINGS";

            float y = -74f;

            Slider masterSlider = AddSliderRow(canvasGO.transform, font, "Master Volume", ref y, 0f, 1f, AudioManager.MasterVolume);
            masterSlider.onValueChanged.AddListener(v => { AudioManager.SetMasterVolume(v); SliderTick(); });

            Slider musicSlider = AddSliderRow(canvasGO.transform, font, "Music Volume", ref y, 0f, 1f, AudioManager.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => { AudioManager.SetMusicVolume(v); SliderTick(); });

            Slider sfxSlider = AddSliderRow(canvasGO.transform, font, "SFX Volume", ref y, 0f, 1f, AudioManager.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => { AudioManager.SetSfxVolume(v); SliderTick(); });

            Slider sensitivitySlider = AddSliderRow(canvasGO.transform, font, "Mouse Sensitivity", ref y, 0.1f, 5f, GameSettings.MouseSensitivity);
            sensitivitySlider.onValueChanged.AddListener(v => { GameSettings.SetMouseSensitivity(v); SliderTick(); });

            RectTransform qualityRow = AddRow(canvasGO.transform, "QualityRow", ref y);
            CreateRowLabel(qualityRow, font, "Quality");
            RectTransform qualityControl = CreateControlArea(qualityRow);
            Dropdown qualityDropdown = CreateDropdown(qualityControl, font, QualitySettings.names, QualitySettings.GetQualityLevel());
            qualityDropdown.onValueChanged.AddListener(v => { GameSettings.SetQualityLevel(v); AudioManager.PlayUi("ui_click"); });

            RectTransform fullscreenRow = AddRow(canvasGO.transform, "FullscreenRow", ref y);
            CreateRowLabel(fullscreenRow, font, "Fullscreen");
            RectTransform fullscreenControl = CreateControlArea(fullscreenRow);
            Toggle fullscreenToggle = CreateToggle(fullscreenControl, GameSettings.Fullscreen);
            fullscreenToggle.onValueChanged.AddListener(v => { GameSettings.SetFullscreen(v); AudioManager.PlayUi("ui_click"); });

            Button backButton = CreateButton(canvasGO.transform, font, "Back");
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 24f);
            backButton.onClick.AddListener(() => {
                AudioManager.PlayUi("ui_back");
                canvasGO.SetActive(false);
                onClose?.Invoke();
            });
        }

        static float lastTickTime;

        // Slider values change every frame while dragged with the VR laser; one click per change spammed audio.
        static void SliderTick() {
            if (Time.unscaledTime - lastTickTime < 0.08f) return;
            lastTickTime = Time.unscaledTime;
            AudioManager.PlayUi("ui_click", 0.5f);
        }

        Slider AddSliderRow(Transform parent, Font font, string label, ref float y, float min, float max, float initial) {
            RectTransform row = AddRow(parent, label + "Row", ref y);
            CreateRowLabel(row, font, label);
            RectTransform control = CreateControlArea(row);
            return CreateSlider(control, min, max, initial);
        }

        RectTransform AddRow(Transform parent, string name, ref float y) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(0f, RowHeight);
            y -= RowHeight;
            return rect;
        }

        Text CreateRowLabel(RectTransform row, Font font, string label) {
            Text text = CreateText(row, "Label", font, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, Vector2.zero, new Vector2(0.42f, 1f), new Vector2(16f, 0f), new Vector2(-4f, 0f));
            text.text = label;
            return text;
        }

        RectTransform CreateControlArea(RectTransform row) {
            GameObject go = new GameObject("Control");
            go.transform.SetParent(row, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.44f, 0.2f);
            rect.anchorMax = new Vector2(0.98f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        Slider CreateSlider(RectTransform parent, float min, float max, float initial) {
            GameObject go = new GameObject("Slider");
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Slider slider = go.AddComponent<Slider>();

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.2f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.3f);
            bgRect.anchorMax = new Vector2(1f, 0.7f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(go.transform, false);
            RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.3f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.7f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-15f, 0f);

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.65f, 1f, 1f);
            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            Image handleImage = handleGO.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);

            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initial;
            return slider;
        }

        Toggle CreateToggle(RectTransform parent, bool initial) {
            GameObject go = new GameObject("Toggle");
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(40f, 40f);
            rect.anchoredPosition = new Vector2(20f, 0f);
            Toggle toggle = go.AddComponent<Toggle>();

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.25f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            SetRect(bgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(bgGO.transform, false);
            Image checkImage = checkGO.AddComponent<Image>();
            checkImage.color = new Color(0.35f, 0.65f, 1f, 1f);
            RectTransform checkRect = checkGO.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = initial;
            return toggle;
        }

        Dropdown CreateDropdown(RectTransform parent, Font font, string[] options, int currentIndex) {
            GameObject ddGO = new GameObject("Dropdown");
            ddGO.transform.SetParent(parent, false);
            RectTransform ddRect = ddGO.AddComponent<RectTransform>();
            SetRect(ddRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image ddImage = ddGO.AddComponent<Image>();
            ddImage.color = new Color(1f, 1f, 1f, 0.15f);
            Dropdown dropdown = ddGO.AddComponent<Dropdown>();

            Text labelText = CreateText(ddGO.transform, "Label", font, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));

            GameObject templateGO = new GameObject("Template");
            templateGO.transform.SetParent(ddGO.transform, false);
            RectTransform templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -2f);
            templateRect.sizeDelta = new Vector2(0f, 150f);
            Image templateImage = templateGO.AddComponent<Image>();
            templateImage.color = new Color(0.1f, 0.1f, 0.14f, 0.97f);
            ScrollRect scrollRect = templateGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);
            RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
            SetRect(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Mask mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 28f);

            GameObject itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);
            RectTransform itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 28f);
            Toggle itemToggle = itemGO.AddComponent<Toggle>();

            GameObject itemBgGO = new GameObject("Item Background");
            itemBgGO.transform.SetParent(itemGO.transform, false);
            Image itemBgImage = itemBgGO.AddComponent<Image>();
            itemBgImage.color = new Color(1f, 1f, 1f, 0.08f);
            RectTransform itemBgRect = itemBgGO.GetComponent<RectTransform>();
            SetRect(itemBgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject itemCheckGO = new GameObject("Item Checkmark");
            itemCheckGO.transform.SetParent(itemGO.transform, false);
            Image itemCheckImage = itemCheckGO.AddComponent<Image>();
            itemCheckImage.color = new Color(0.35f, 0.65f, 1f, 1f);
            RectTransform itemCheckRect = itemCheckGO.GetComponent<RectTransform>();
            itemCheckRect.anchorMin = new Vector2(0f, 0.5f);
            itemCheckRect.anchorMax = new Vector2(0f, 0.5f);
            itemCheckRect.sizeDelta = new Vector2(16f, 16f);
            itemCheckRect.anchoredPosition = new Vector2(14f, 0f);

            Text itemLabelText = CreateText(itemGO.transform, "Item Label", font, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
            itemLabelText.color = Color.white;
            SetRect(itemLabelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 1f), new Vector2(-6f, -1f));

            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = itemCheckImage;
            itemToggle.isOn = true;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;

            templateGO.SetActive(false);

            dropdown.options.Clear();
            for (int i = 0; i < options.Length; i++) dropdown.options.Add(new Dropdown.OptionData(options[i]));
            dropdown.value = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, options.Length - 1));
            dropdown.RefreshShownValue();

            return dropdown;
        }

        Button CreateButton(Transform parent, Font font, string label) {
            GameObject go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 50f);
            Image image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(go.transform, "Text", font, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = label;

            return button;
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
