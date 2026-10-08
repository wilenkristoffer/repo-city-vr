using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RepoCity {

    // Entry point for Assets/Scenes/MainMenu.unity: add to one empty 'Bootstrapper'
    // GameObject. Builds a world-space menu canvas in front of the camera since
    // screen-space-overlay canvases don't render inside a VR headset.
    public class MainMenu : MonoBehaviour {

        void Start() {
            AudioManager.EnsureInitialized();
            GameSettings.EnsureLoaded();
            AudioManager.PlayMusic("menu_jingle", false);

            if (Camera.main == null) {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
            }

            if (FindObjectOfType<EventSystem>() == null) {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }

            BuildCanvas();
        }

        void BuildCanvas() {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
            Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;

            GameObject canvasGO = new GameObject("MenuCanvas");
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 320f);
            canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Vector3 menuTarget = camPos + camForward * 3f;
            canvasGO.transform.position = menuTarget;
            // World-space UI canvases read correctly from their LOCAL -Z side, so after
            // LookAt (which points local +Z at the target) we flip 180 degrees so the
            // readable face actually points back at the camera.
            canvasGO.transform.LookAt(camPos);
            canvasGO.transform.Rotate(0f, 180f, 0f);

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.14f, 0.92f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Text title = CreateText(canvasGO.transform, "Title", font, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -70f), new Vector2(-8f, -10f));
            title.text = "REPO CITY";

            CreateButton(canvasGO.transform, font, "PlayButton", "Play", new Vector2(0f, -90f), () => {
                UnityEngine.SceneManagement.SceneManager.LoadScene("RepoWorld");
            });

            CreateButton(canvasGO.transform, font, "SettingsButton", "Settings", new Vector2(0f, -160f), () => {
                GameObject settingsGO = new GameObject("SettingsPanel");
                settingsGO.transform.SetParent(transform, false);
                settingsGO.transform.position = menuTarget;
                settingsGO.transform.rotation = canvasGO.transform.rotation;
                settingsGO.AddComponent<SettingsPanel>().Initialize(() => { Destroy(settingsGO); });
            });

            CreateButton(canvasGO.transform, font, "QuitButton", "Quit", new Vector2(0f, -230f), () => {
                Application.Quit();
            });
        }

        void CreateButton(Transform parent, Font font, string name, string label, Vector2 centerOffset, System.Action onClick) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.22f, 0.4f, 1f);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(220f, 52f);
            rect.anchoredPosition = centerOffset;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => AudioManager.PlayUi("ui_click"));
            button.onClick.AddListener(() => onClick());

            Text text = CreateText(go.transform, "Label", font, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = label;
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
